using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuTestFramework.Core;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using AxuTestFramework.Core.Trend;
using AxuTestFramework.Editor.Export;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// <see cref="AxuTestRunnerWindow"/> 的 Scenario / Trend 页签（Phase 4，方案 §6.3）。
    /// <para>Scenario 页：扫描 Assets 下 *.suite.json / *.scenario.json 列表 → 选中后 Run Suite
    /// （复用 <see cref="ScenarioRunMenu.TryRunSuite"/>，含 define 预检）→ 退出 Play 后自动加载
    /// <c>Library/AxuTestReports/scenario/</c> 最新报告（scenario/status/durationMs/退出码）、
    /// 趋势入库（TrendStore.Append）并提供 JUnit / HTML 导出。Trend 页：读
    /// <see cref="AxuTrendPaths.TrendCsvPath"/>，按 scenario 分组选看各 metric 最近 10 次序列与
    /// 最近两次环比（TrendSummary.ChangePercent，正值=变差用警示色）。</para>
    /// </summary>
    public partial class AxuTestRunnerWindow
    {
        private VisualElement _scenarioPage;
        private VisualElement _trendPage;
        private Button _overviewTabButton;
        private Button _testsTabButton;
        private Button _scenarioTabButton;
        private Button _trendTabButton;

        // ---------------------------------------------------------------- Scenario 页状态

        private struct SuiteEntry
        {
            public string Path;
            public string Label;
            public bool IsSuite;
        }

        private readonly List<SuiteEntry> _suiteEntries = new List<SuiteEntry>();
        private readonly List<VisualElement> _suiteRowElements = new List<VisualElement>();
        private string _selectedSuitePath;
        private ScrollView _suiteListScroll;
        private Button _runSuiteButton;
        private Button _exportJunitButton;
        private Button _exportHtmlButton;
        private Label _scenarioSummaryLabel;
        private ScrollView _scenarioResultsScroll;
        private ScenarioReport _scenarioReport;
        private string _scenarioReportPath;

        /// <summary>Scenario 页发起的运行是否在等 Play Mode 结束（结束后回读报告 + 趋势入库）。
        /// 静态：进 Play 的域重载会重建窗口实例，实例字段不跨域存活。</summary>
        private static bool _scenarioRunPending;
        private static DateTime _scenarioRunStartedUtc;

        // ---------------------------------------------------------------- Trend 页状态

        private Label _trendHeaderLabel;
        private DropdownField _trendScenarioField;
        private ScrollView _trendScroll;
        private List<TrendPoint> _trendPoints = new List<TrendPoint>();

        // ------------------------------------------------------------------ 页签

        private void BuildTabBar()
        {
            var tabBar = new VisualElement { name = "ax-tabbar" };
            tabBar.AddToClassList("ax-topbar");

            _overviewTabButton = new Button(() => SetPage("overview")) { text = "总览" };
            _testsTabButton = new Button(() => SetPage("tests")) { text = "单元测试" };
            _scenarioTabButton = new Button(() => SetPage("scenario")) { text = "场景套件" };
            _trendTabButton = new Button(() => SetPage("trend")) { text = "趋势" };
            foreach (var button in new[] { _overviewTabButton, _testsTabButton, _scenarioTabButton, _trendTabButton })
                button.AddToClassList("ax-mode-button");

            tabBar.Add(_overviewTabButton);
            tabBar.Add(_testsTabButton);
            tabBar.Add(_scenarioTabButton);
            tabBar.Add(_trendTabButton);
            rootVisualElement.Add(tabBar);
        }

        private void SetPage(string page)
        {
            if (_overviewPage == null || _testsPage == null || _scenarioPage == null || _trendPage == null)
                return;

            _overviewPage.style.display = page == "overview" ? DisplayStyle.Flex : DisplayStyle.None;
            _testsPage.style.display = page == "tests" ? DisplayStyle.Flex : DisplayStyle.None;
            _scenarioPage.style.display = page == "scenario" ? DisplayStyle.Flex : DisplayStyle.None;
            _trendPage.style.display = page == "trend" ? DisplayStyle.Flex : DisplayStyle.None;

            _overviewTabButton.EnableInClassList("ax-mode-active", page == "overview");
            _testsTabButton.EnableInClassList("ax-mode-active", page == "tests");
            _scenarioTabButton.EnableInClassList("ax-mode-active", page == "scenario");
            _trendTabButton.EnableInClassList("ax-mode-active", page == "trend");

            if (page == "trend")
                RefreshTrend();
            if (page == "overview")
                RefreshOverview();
        }

        // ------------------------------------------------------------------ Scenario 页

        private void BuildScenarioPage()
        {
            _scenarioPage = new VisualElement { name = "ax-page-scenario" };
            _scenarioPage.style.flexGrow = 1f;
            _scenarioPage.style.flexDirection = FlexDirection.Row;

            // 左：suite/scenario JSON 列表
            var listPane = new VisualElement { name = "ax-suite-list-pane" };
            listPane.AddToClassList("ax-pane");
            listPane.style.width = 380f;
            var listHeader = new Label("场景套件列表（点击选中后，右侧运行）");
            listHeader.AddToClassList("ax-pane-header");
            var refreshButton = new Button(RefreshSuiteList) { text = "刷新列表" };
            refreshButton.AddToClassList("ax-secondary-button");
            _suiteListScroll = new ScrollView { viewDataKey = "ax-suite-list-scroll" };
            _suiteListScroll.AddToClassList("ax-scroll");
            var listHint = new Label("① 点击左侧套件选中  →  ② 点右侧「▶ 运行选中的套件」  →  ③ 跑完自动回显结果");
            listHint.AddToClassList("ax-help-note");
            listPane.Add(listHeader);
            listPane.Add(refreshButton);
            listPane.Add(_suiteListScroll);
            listPane.Add(listHint);

            // 右：运行 + 报告摘要 + 导出
            var reportPane = new VisualElement { name = "ax-suite-report-pane" };
            reportPane.AddToClassList("ax-pane");
            reportPane.AddToClassList("ax-pane-last");
            var reportHeader = new Label("运行与报告");
            reportHeader.AddToClassList("ax-pane-header");

            var actionRow = new VisualElement { name = "ax-suite-actions" };
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginBottom = 8f;
            _runSuiteButton = new Button(RunSelectedSuite) { text = "▶  运行选中的套件" };
            _runSuiteButton.AddToClassList("ax-run-button");
            _runSuiteButton.SetEnabled(false);
            _runSuiteButton.tooltip = "先点击左侧列表选中一套件，再点此运行（写桥文件并进入 Play Mode，由 Agent 执行；含 AXU_TEST_AGENT define 预检）";
            _exportJunitButton = new Button(ExportSelectedReportJunit) { text = "导出 JUnit" };
            _exportJunitButton.AddToClassList("ax-secondary-button");
            _exportJunitButton.SetEnabled(false);
            _exportJunitButton.tooltip = "把最近一次报告导出为 JUnit XML（CI 系统对接用）";
            _exportHtmlButton = new Button(ExportSelectedReportHtml) { text = "导出 HTML" };
            _exportHtmlButton.AddToClassList("ax-secondary-button");
            _exportHtmlButton.SetEnabled(false);
            _exportHtmlButton.tooltip = "把最近一次报告导出为单文件 HTML 摘要（可分享给非技术同事）";
            actionRow.Add(_runSuiteButton);
            actionRow.Add(_exportJunitButton);
            actionRow.Add(_exportHtmlButton);

            _scenarioSummaryLabel = new Label("（尚无报告——点击左侧套件选中后运行，结果自动加载）");
            _scenarioSummaryLabel.AddToClassList("ax-summary");
            _scenarioResultsScroll = new ScrollView { viewDataKey = "ax-scenario-results-scroll" };
            _scenarioResultsScroll.AddToClassList("ax-scroll");

            reportPane.Add(reportHeader);
            reportPane.Add(actionRow);
            reportPane.Add(_scenarioSummaryLabel);
            reportPane.Add(_scenarioResultsScroll);

            _scenarioPage.Add(listPane);
            _scenarioPage.Add(reportPane);
            rootVisualElement.Add(_scenarioPage);
        }

        /// <summary>扫描 Assets 下 *.suite.json / *.scenario.json 并渲染为可选列表（复用运行入口的路径约定）。</summary>
        private void RefreshSuiteList()
        {
            if (_suiteListScroll == null)
                return;

            _suiteEntries.Clear();
            try
            {
                var root = Application.dataPath;
                foreach (var file in Directory.EnumerateFiles(root, "*.suite.json", SearchOption.AllDirectories))
                    _suiteEntries.Add(new SuiteEntry { Path = file, IsSuite = true });
                foreach (var file in Directory.EnumerateFiles(root, "*.scenario.json", SearchOption.AllDirectories))
                    _suiteEntries.Add(new SuiteEntry { Path = file, IsSuite = false });
                _suiteEntries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            }
            catch (Exception ex)
            {
                UpdateStatus("Suite 列表扫描失败: " + ex.Message);
                return;
            }

            _suiteListScroll.Clear();
            _suiteRowElements.Clear();
            if (_suiteEntries.Count == 0)
            {
                var empty = new Label("（Assets 下未发现 .suite.json / .scenario.json——菜单 AxuTestFramework/Create Suite JSON 可生成模板）");
                empty.AddToClassList("ax-empty-note");
                _suiteListScroll.Add(empty);
            }

            foreach (var entry in _suiteEntries)
            {
                var captured = entry;
                var (title, id, capabilities) = ReadSuiteMeta(captured.Path);

                // 主行文本：优先中文标题（来自 suite 的 title 字段），无标题回退 id；副行 = id + 能力清单
                var primary = !string.IsNullOrEmpty(title) ? title : id ?? captured.Path;
                var subParts = new List<string>();
                if (!string.IsNullOrEmpty(id) && !string.Equals(id, primary, StringComparison.Ordinal))
                    subParts.Add(id);
                if (!string.IsNullOrEmpty(capabilities))
                    subParts.Add("能力：" + capabilities);

                var row = new VisualElement { name = "ax-suite-row" };
                row.AddToClassList("ax-suite-row");
                var titleLabel = new Label(primary);
                titleLabel.AddToClassList("ax-suite-title");
                row.Add(titleLabel);
                if (subParts.Count > 0)
                {
                    var subLabel = new Label(string.Join(" · ", subParts));
                    subLabel.AddToClassList("ax-suite-sub");
                    row.Add(subLabel);
                }
                row.tooltip = captured.IsSuite
                    ? $"套件文件：{captured.Path}\n点击选中后在右侧点「▶ 运行选中的套件」"
                    : $"场景文件：{captured.Path}";
                row.RegisterCallback<PointerDownEvent>(_ => SelectSuite(captured));
                _suiteListScroll.Add(row);
                _suiteRowElements.Add(row);
            }

            UpdateSuiteSelectionHighlight();
            UpdateSelectedSuiteUi();
        }

        /// <summary>读 suite JSON 的 title/id/能力清单（Inline.Capability / Ref）。任何解析失败静默降级——显示路径即可。</summary>
        private static (string title, string id, string capabilities) ReadSuiteMeta(string path)
        {
            try
            {
                var suite = Newtonsoft.Json.JsonConvert.DeserializeObject<SuiteDef>(File.ReadAllText(path));
                if (suite == null)
                    return (null, null, null);

                var caps = new List<string>();
                if (suite.Scenarios != null)
                {
                    foreach (var entry in suite.Scenarios)
                    {
                        if (entry == null)
                            continue;
                        if (entry.Inline != null && !string.IsNullOrEmpty(entry.Inline.Capability))
                            caps.Add(entry.Inline.Capability);
                        else if (!string.IsNullOrEmpty(entry.Ref))
                            caps.Add(entry.Ref);
                    }
                }

                var capabilities = caps.Count > 0 ? string.Join("、", caps.Distinct().Take(6)) : null;
                return (suite.Title, suite.Id, capabilities);
            }
            catch
            {
                return (null, null, null);
            }
        }

        private void SelectSuite(SuiteEntry entry)
        {
            _selectedSuitePath = entry.Path;
            UpdateSuiteSelectionHighlight();
            UpdateSelectedSuiteUi();
        }

        /// <summary>按选中路径回填列表行高亮（重建/选中共用）。</summary>
        private void UpdateSuiteSelectionHighlight()
        {
            for (var i = 0; i < _suiteEntries.Count && i < _suiteRowElements.Count; i++)
                _suiteRowElements[i].EnableInClassList("ax-suite-selected", _suiteEntries[i].Path == _selectedSuitePath);
        }

        private void UpdateSelectedSuiteUi()
        {
            if (_runSuiteButton == null)
                return;
            _runSuiteButton.SetEnabled(!string.IsNullOrEmpty(_selectedSuitePath) && !EditorApplication.isPlaying);
        }

        /// <summary>运行选中 suite：复用 <see cref="ScenarioRunMenu.TryRunSuite"/>（define 预检 + 桥文件 + 进 Play）。</summary>
        private void RunSelectedSuite()
        {
            if (string.IsNullOrEmpty(_selectedSuitePath))
                return;

            if (!ScenarioRunMenu.TryRunSuite(_selectedSuitePath))
                return;

            _scenarioRunPending = true;
            _scenarioRunStartedUtc = DateTime.UtcNow;
            UpdateStatus($"Suite 运行中（{Relativize(AxuTestReportPaths.ProjectRootFullPath, _selectedSuitePath)}）——退出 Play 后自动加载报告。");
            UpdateSelectedSuiteUi();
        }

        /// <summary>Play Mode 状态钩子：静态订阅（进 Play 的域重载会重建窗口实例，实例订阅会丢）。
        /// 退出 Play 且有挂起的 scenario 运行时，延迟到编辑器空闲回调回读报告（退出瞬间可能尚未落盘）。</summary>
        [InitializeOnLoadMethod]
        private static void HookPlayModeEvents()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
                    return;
                if (!_scenarioRunPending)
                    return;

                _scenarioRunPending = false;
                EditorApplication.delayCall += DeliverScenarioRunResults;
            };
        }

        /// <summary>回读最新报告 → 趋势入库 → 转发给所有打开的看板实例渲染。</summary>
        private static void DeliverScenarioRunResults()
        {
            if (!TryLoadScenarioReport(_scenarioRunStartedUtc, out var report, out var path))
                return;

            AppendScenarioTrend(report);

            foreach (var window in Resources.FindObjectsOfTypeAll<AxuTestRunnerWindow>())
            {
                window._scenarioReport = report;
                window._scenarioReportPath = path;
                window.RenderScenarioReport();
                window.RefreshOverview(); // 若停留在总览页，徽章与趋势速览同步刷新（重新读最近报告与趋势 CSV）
                window.UpdateStatus($"Suite 运行完成：{Path.GetFileName(path)}（趋势已入库）");
                window.UpdateSelectedSuiteUi();
            }
        }

        /// <summary>找 scenario 报告目录里、指定时刻之后落盘的最新报告并反序列化。
        /// soak 中断残留的 *.partial.json（truncated 报告）不参与最新报告选择，仅告警提示残留。</summary>
        private static bool TryLoadScenarioReport(DateTime sinceUtc, out ScenarioReport report, out string path)
        {
            report = null;
            path = null;

            var directory = AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot);
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning("[AxuTestFramework] 未发现 scenario 报告目录（Library/AxuTestReports/scenario）——Agent 可能未运行（检查 AXU_TEST_AGENT define）。");
                return false;
            }

            // 排除 *.partial.json：soak 中断残留的 truncated 报告同样以 .json 结尾，若恰为最新会被误当作本次运行报告
            var newest = new DirectoryInfo(directory)
                .EnumerateFiles("*.json")
                .Where(f => !f.Name.EndsWith(".partial.json", StringComparison.OrdinalIgnoreCase))
                .Where(f => f.LastWriteTimeUtc >= sinceUtc.AddSeconds(-5))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            // 先于 newest 判空告警：soak 中断后目录可能只有 partial 残留，此时残留提示比"未发现报告"更有价值
            WarnIfPartialRemnants(directory);

            if (newest == null)
            {
                Debug.LogWarning("[AxuTestFramework] 未发现本次运行产出的 scenario 报告——Agent 可能未运行或报告写盘失败。");
                return false;
            }

            try
            {
                report = AxuTestFramework.Core.Json.AxuJson.FromFile<ScenarioReport>(
                    newest.FullName, SchemaNames.ScenarioReport);
                path = newest.FullName;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AxuTestFramework] scenario 报告解析失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>扫描 scenario 报告目录的 *.partial.json 残留并告警（soak 中断留下的 truncated 报告，
        /// 只提示完整数据以前段为准，不改变返回值与流程）。</summary>
        private static void WarnIfPartialRemnants(string directory)
        {
            try
            {
                var partials = new DirectoryInfo(directory)
                    .EnumerateFiles("*.partial.json")
                    .Select(f => f.Name)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (partials.Count == 0)
                    return;

                var detail = partials.Count <= 3
                    ? string.Join("、", partials)
                    : $"共 {partials.Count} 个";
                Debug.LogWarning($"[AxuTestFramework] 发现 soak 中断残留的 partial 报告（truncated），完整数据以前段为准: {detail}");
            }
            catch (Exception ex)
            {
                // 残留扫描失败不拦报告加载
                Debug.LogWarning($"[AxuTestFramework] partial 残留扫描失败（忽略）: {ex.Message}");
            }
        }

        /// <summary>Editor 内运行路径的趋势入库（pipeline 命令侧由 axu_player_suite_run 负责，此处不重复）。</summary>
        private static void AppendScenarioTrend(ScenarioReport report)
        {
            if (report == null)
                return;
            try
            {
                TrendStore.Append(
                    AxuTestReportPaths.ToFullPath(AxuTrendPaths.TrendCsvPath),
                    TrendStore.FromReport(report, report.SuiteId));
            }
            catch (Exception ex)
            {
                // 入库失败不拦报告展示
                Debug.LogWarning($"[AxuTestFramework] 趋势入库失败: {ex.Message}");
            }
        }

        private void RenderScenarioReport()
        {
            _scenarioResultsScroll.Clear();
            var report = _scenarioReport;
            if (report == null)
                return;

            _scenarioSummaryLabel.text =
                $"suite: {report.SuiteId} · exitCode {(int)report.ExitCode} · " +
                (report.Summary != null
                    ? $"total {report.Summary.Total} · pass {report.Summary.Passed} · warn {report.Summary.Warned} · fail {report.Summary.Failed}"
                    : "（无汇总）") +
                $"\n报告: {Path.GetFileName(_scenarioReportPath)}";

            if (report.Results == null || report.Results.Count == 0)
            {
                var empty = new Label("（报告无 scenario 明细）");
                empty.AddToClassList("ax-empty-note");
                _scenarioResultsScroll.Add(empty);
            }
            else
            {
                foreach (var result in report.Results)
                {
                    if (result == null) continue;
                    var status = (result.Status ?? "-").Trim().ToLowerInvariant();
                    var row = new Label(
                        $"{StatusBadge(status)} {result.ScenarioId} · {result.Capability} · " +
                        $"{result.DurationMs:0.#} ms · findings {result.Findings?.Count ?? 0}");
                    row.AddToClassList("ax-result-detail");
                    row.EnableInClassList("ax-row-fail", status == "fail" || status == "error");
                    row.EnableInClassList("ax-row-pass", status == "pass");
                    _scenarioResultsScroll.Add(row);
                }
            }

            _exportJunitButton.SetEnabled(true);
            _exportHtmlButton.SetEnabled(true);
        }

        private static string StatusBadge(string status)
        {
            switch (status)
            {
                case "pass": return "[PASS]";
                case "warn": return "[WARN]";
                case "fail": return "[FAIL]";
                case "error": return "[ERROR]";
                default: return $"[{status}]";
            }
        }

        private void ExportSelectedReportJunit()
        {
            if (_scenarioReport == null)
                return;
            var path = EditorUtility.SaveFilePanel("导出 JUnit XML", "",
                $"{_scenarioReport.SuiteId}-junit.xml", "xml");
            if (string.IsNullOrEmpty(path))
                return;
            JUnitExporter.ExportToFile(_scenarioReport, _scenarioReport.SuiteId, path);
            UpdateStatus("JUnit XML 已导出: " + path);
            EditorUtility.RevealInFinder(path);
        }

        private void ExportSelectedReportHtml()
        {
            if (_scenarioReport == null)
                return;
            var path = EditorUtility.SaveFilePanel("导出 HTML 摘要", "",
                $"{_scenarioReport.SuiteId}-summary.html", "html");
            if (string.IsNullOrEmpty(path))
                return;
            HtmlSummaryExporter.ExportToFile(_scenarioReport, _scenarioReport.SuiteId, path);
            UpdateStatus("HTML 摘要已导出: " + path);
            EditorUtility.RevealInFinder(path);
        }

        // ------------------------------------------------------------------ Trend 页

        private void BuildTrendPage()
        {
            _trendPage = new VisualElement { name = "ax-page-trend" };
            _trendPage.style.flexGrow = 1f;
            _trendPage.AddToClassList("ax-pane");

            _trendHeaderLabel = new Label("趋势（每个场景最近 10 次运行，最新在右）");
            _trendHeaderLabel.AddToClassList("ax-pane-header");

            var controlRow = new VisualElement { name = "ax-trend-controls" };
            controlRow.style.flexDirection = FlexDirection.Row;
            controlRow.style.marginBottom = 8f;
            var refreshButton = new Button(RefreshTrend) { text = "刷新" };
            refreshButton.AddToClassList("ax-secondary-button");
            _trendScenarioField = new DropdownField("场景") { choices = new List<string>() };
            _trendScenarioField.style.flexGrow = 1f;
            _trendScenarioField.RegisterValueChangedCallback(_ => RenderTrend());
            controlRow.Add(refreshButton);
            controlRow.Add(_trendScenarioField);

            var trendHint = new Label("每行一个指标：数字序列 = 最近 10 次运行的值（最新在右）；右侧百分比 = 最近两次变化（红色=变差，绿色=改善）。");
            trendHint.AddToClassList("ax-help-note");

            _trendScroll = new ScrollView { viewDataKey = "ax-trend-scroll" };
            _trendScroll.AddToClassList("ax-scroll");

            _trendPage.Add(_trendHeaderLabel);
            _trendPage.Add(controlRow);
            _trendPage.Add(_trendScroll);
            rootVisualElement.Add(_trendPage);

            RefreshTrend();
        }

        /// <summary>读趋势 CSV 并回填 scenario 下拉（CSV 缺失/解析失败给空态提示）。</summary>
        private void RefreshTrend()
        {
            if (_trendScroll == null)
                return;

            var fullPath = AxuTestReportPaths.ToFullPath(AxuTrendPaths.TrendCsvPath);
            if (!File.Exists(fullPath))
            {
                _trendPoints = new List<TrendPoint>();
                _trendScenarioField.choices = new List<string>();
                RenderTrendEmpty("趋势库为空——在「场景套件」页运行一次套件后，趋势会自动记录到这里。");
                return;
            }

            try
            {
                _trendPoints = TrendStore.Read(fullPath);
            }
            catch (Exception ex)
            {
                _trendPoints = new List<TrendPoint>();
                RenderTrendEmpty("趋势 CSV 读取失败: " + ex.Message);
                return;
            }

            var scenarios = _trendPoints
                .Select(p => p.Scenario)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();
            _trendScenarioField.choices = scenarios;
            if (scenarios.Count > 0 && (string.IsNullOrEmpty(_trendScenarioField.value) || !scenarios.Contains(_trendScenarioField.value)))
                _trendScenarioField.SetValueWithoutNotify(scenarios[0]);

            RenderTrend();
        }

        private void RenderTrendEmpty(string message)
        {
            _trendScroll.Clear();
            var empty = new Label(message);
            empty.AddToClassList("ax-empty-note");
            _trendScroll.Add(empty);
        }

        /// <summary>渲染选中 scenario 的各 metric：最近 10 次序列 + 最近两次 ChangePercent（正值=变差，警示色）。</summary>
        private void RenderTrend()
        {
            if (_trendScroll == null)
                return;
            _trendScroll.Clear();

            var scenario = _trendScenarioField.value;
            if (string.IsNullOrEmpty(scenario) || _trendPoints.Count == 0)
            {
                RenderTrendEmpty("（无数据——选择场景后此处展示各指标最近 10 次序列与最近两次变化）");
                return;
            }

            _trendHeaderLabel.text = $"趋势 · {scenario}";

            var metrics = _trendPoints
                .Where(p => p.Scenario == scenario)
                .Select(p => p.Metric)
                .Where(m => !string.IsNullOrEmpty(m))
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            // run 级行：整体结果/退出码序列（无环比——Value 为 null 不参与 ChangePercent）
            var runRows = TrendSummary.Series(_trendPoints, scenario, string.Empty);
            if (runRows.Count > 0)
            {
                var lastRun = runRows[runRows.Count - 1];
                _trendScroll.Add(CreateTrendRow("(run) 整体结果",
                    string.Join(" → ", LastN(runRows, 10).Select(r => $"{r.Result}(exit {r.ExitCode})")), null,
                    lastRun.Result));
            }

            foreach (var metric in metrics)
            {
                var series = TrendSummary.Series(_trendPoints, scenario, metric);
                if (series.Count == 0)
                    continue;

                var last10 = LastN(series, 10);
                var seriesText = string.Join(" → ", last10.Select(p => p.Value.HasValue ? p.Value.Value.ToString("0.###") : "-"));

                double? change = null;
                if (last10.Count >= 2)
                {
                    try { change = TrendSummary.ChangePercent(last10[last10.Count - 2], last10[last10.Count - 1]); }
                    catch (ArgumentException) { change = null; }
                }
                _trendScroll.Add(CreateTrendRow(metric, seriesText, change));
            }
        }

        /// <summary>单个指标行：名称 + 最近 10 次序列 + 环比（正值=变差→红，负值=改善→绿，无→灰）。
        /// <paramref name="resultTone"/> 仅 run 级行使用：pass/warn/fail/error 给序列文本着色。</summary>
        private static VisualElement CreateTrendRow(string metric, string seriesText, double? changePercent, string resultTone = null)
        {
            var row = new VisualElement { name = "ax-trend-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6f;
            row.style.flexWrap = Wrap.Wrap;

            var nameLabel = new Label(metric);
            nameLabel.style.width = 180f;
            nameLabel.style.color = ParseColor("#e8e8e8");
            nameLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;

            var seriesLabel = new Label(seriesText);
            seriesLabel.style.color = ParseColor("#a0a0a0");
            seriesLabel.style.flexGrow = 1f;

            var tone = (resultTone ?? "-").Trim().ToLowerInvariant();
            switch (tone)
            {
                case "pass": seriesLabel.AddToClassList("ax-trend-run-ok"); break;
                case "warn": seriesLabel.AddToClassList("ax-trend-run-warn"); break;
                case "fail":
                case "error": seriesLabel.AddToClassList("ax-trend-run-bad"); break;
            }

            row.Add(nameLabel);
            row.Add(seriesLabel);

            if (changePercent.HasValue)
            {
                var changeLabel = new Label($"{changePercent.Value:+0.#;-0.#;0}%");
                changeLabel.style.width = 80f;
                changeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                changeLabel.style.color = changePercent.Value > 0.0001
                    ? ParseColor("#ec8a8a")
                    : changePercent.Value < -0.0001 ? ParseColor("#8ed19a") : ParseColor("#a0a0a0");
                changeLabel.tooltip = "最近两次的百分比变化——正值 = 相对上次变差（红），负值 = 改善（绿）";
                row.Add(changeLabel);
            }

            return row;
        }

        private static Color ParseColor(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.white;
        }

        /// <summary>取列表末尾最多 n 个（保持原顺序；.NET Framework/Standard 无 TakeLast，自实现）。</summary>
        private static List<T> LastN<T>(List<T> list, int n)
        {
            return list.Skip(Math.Max(0, list.Count - n)).ToList();
        }

        private static string Relativize(string root, string fullPath)
        {
            var normalizedRoot = root.Replace('\\', '/').TrimEnd('/') + "/";
            var normalized = fullPath.Replace('\\', '/');
            return normalized.StartsWith(normalizedRoot, StringComparison.Ordinal)
                ? normalized.Substring(normalizedRoot.Length)
                : fullPath;
        }
    }
}
