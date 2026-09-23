using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AxuTestFramework.Core;
using AxuTestFramework.Core.Baseline;
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
    /// （<see cref="ScenarioRunService.StartEditorPlayRun"/>，与菜单/pipeline 共享启动-收尾契约）→
    /// Agent 写出终态后看门狗自动退出 Play → 本页按状态文件的 <c>runId</c>/<c>reportPath</c> 回显
    /// **本次**结果（completed 加载状态文件指名的报告；aborted/error 清空面板说明原因，绝不回显陈旧报告）。
    /// 状态来源只有两个文件：运行记录（Temp/axu_scenario_run.json，在途）与状态文件
    /// （Temp/axu_scenario_status.json，进度与终态）——两者都是磁盘事实，跨 Play 的域重载依然成立。
    /// Trend 页：读 <see cref="AxuTrendPaths.TrendCsvPath"/>，按 scenario 分组选看各 metric 最近 10 次序列与
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
        private VisualElement _scenarioProgressRow;
        private ProgressBar _scenarioProgressBar;
        private Label _scenarioProgressLabel;
        private Button _openJsonButton;
        private Button _openArtifactsButton;
        private Button _openHtmlButton;

        /// <summary>截图缩略图纹理缓存（键=完整路径；重渲染/关窗时 DestroyImmediate 释放）。</summary>
        private readonly Dictionary<string, Texture2D> _artifactTextures = new Dictionary<string, Texture2D>();

        /// <summary>已回显的运行标识（runId）：终态只回显一次，避免 800ms 轮询反复重载报告与纹理。
        /// 实例字段——进/退 Play 的域重载会重建窗口，新实例的首轮询按当前状态文件重新回显最近一次终态。</summary>
        private string _deliveredRunId;

        /// <summary>上次同步给总览页的在途进度（完成数）：仅在推进时刷新总览，避免高频重读趋势 CSV。</summary>
        private int _lastRunningCompletedCount = -1;

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
            var listHint = new Label("① 点击左侧套件选中  →  ② 点右侧「▶ 运行选中的套件」  →  ③ 跑完自动退出 Play 并回显本次结果");
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

            // 报告/工件打开入口（backlog #4 P0：打开 JSON / 目录 / HTML 三按钮，报告加载后启用）
            var openRow = new VisualElement { name = "ax-suite-open-actions" };
            openRow.style.flexDirection = FlexDirection.Row;
            openRow.style.marginBottom = 8f;
            _openJsonButton = new Button(OpenReportJson) { text = "打开报告 JSON" };
            _openJsonButton.AddToClassList("ax-secondary-button");
            _openJsonButton.tooltip = "用系统默认程序打开最近一次报告 JSON（原始数据）";
            _openJsonButton.SetEnabled(false);
            _openArtifactsButton = new Button(OpenArtifactsFolder) { text = "打开工件目录" };
            _openArtifactsButton.AddToClassList("ax-secondary-button");
            _openArtifactsButton.tooltip = "在文件管理器中显示截图等工件目录（报告同目录的 artifacts/）";
            _openArtifactsButton.SetEnabled(false);
            _openHtmlButton = new Button(OpenReportHtml) { text = "打开 HTML 摘要" };
            _openHtmlButton.AddToClassList("ax-secondary-button");
            _openHtmlButton.tooltip = "生成（若尚无）并用浏览器打开自包含 HTML 摘要，可直接分享";
            _openHtmlButton.SetEnabled(false);
            openRow.Add(_openJsonButton);
            openRow.Add(_openArtifactsButton);
            openRow.Add(_openHtmlButton);

            // 运行进度行：轮询 Temp/axu_scenario_status.json 渲染"第 x/N 个场景"（无状态文件时隐藏）
            _scenarioProgressRow = new VisualElement { name = "ax-scenario-progress" };
            _scenarioProgressRow.AddToClassList("ax-scenario-progress");
            _scenarioProgressLabel = new Label();
            _scenarioProgressLabel.AddToClassList("ax-progress-label");
            _scenarioProgressBar = new ProgressBar { lowValue = 0f, highValue = 1f, value = 0f };
            _scenarioProgressBar.AddToClassList("ax-progress-bar");
            _scenarioProgressRow.Add(_scenarioProgressLabel);
            _scenarioProgressRow.Add(_scenarioProgressBar);
            _scenarioProgressRow.style.display = DisplayStyle.None;

            _scenarioSummaryLabel = new Label("（尚无运行记录——点击左侧套件选中后运行，结果自动回显）");
            _scenarioSummaryLabel.AddToClassList("ax-summary");
            _scenarioResultsScroll = new ScrollView { viewDataKey = "ax-scenario-results-scroll" };
            _scenarioResultsScroll.AddToClassList("ax-scroll");

            reportPane.Add(reportHeader);
            reportPane.Add(actionRow);
            reportPane.Add(openRow);
            reportPane.Add(_scenarioProgressRow);
            reportPane.Add(_scenarioSummaryLabel);
            reportPane.Add(_scenarioResultsScroll);

            _scenarioPage.Add(listPane);
            _scenarioPage.Add(reportPane);
            rootVisualElement.Add(_scenarioPage);

            // 运行状态统一由 Temp 下的运行记录 + 状态文件推导（域重载安全）：800ms 一次；
            // 不在运行时只看终态、不回显旧报告。
            _scenarioPage.schedule.Execute(RefreshScenarioRunState).Every(800);
            RefreshScenarioRunState();
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
            // 运行中（Play 或运行记录未收尾）禁止再发起：桥文件必须在进 Play 前写入，重复点击无意义
            var busy = EditorApplication.isPlaying || ScenarioRunRecord.TryRead() != null;
            _runSuiteButton.SetEnabled(!string.IsNullOrEmpty(_selectedSuitePath) && !busy);
        }

        /// <summary>
        /// 运行选中 suite：与菜单/pipeline 共用 <see cref="ScenarioRunService.StartEditorPlayRun"/> 的启动-收尾契约
        /// （预检 → 桥文件 + 运行记录 → 进 Play；Agent 写出终态后自动退 Play 并回显本次报告）。
        /// </summary>
        private void RunSelectedSuite()
        {
            if (string.IsNullOrEmpty(_selectedSuitePath))
                return;

            if (!ScenarioRunMenu.TryRunSuite(_selectedSuitePath, ScenarioRunRecord.SourceDashboard, out var error))
            {
                UpdateStatus("未能启动运行：" + error);
                return;
            }

            UpdateStatus($"Suite 运行中（{Relativize(AxuTestReportPaths.ProjectRootFullPath, _selectedSuitePath)}）" +
                         "——跑完自动退出 Play 并回显本次结果。");
            UpdateSelectedSuiteUi();
            RefreshScenarioRunState();
        }

        // ---------------------------------------------------------------- 运行状态 → 界面（文件驱动，域重载安全）

        /// <summary>
        /// 运行状态刷新（800ms 轮询）：状态只在两个来源里——运行记录（在途）与状态文件（终态）。
        /// 在途：渲染进度（第 x/N 个场景，陈旧判定按心跳）；终态：按 runId 回显一次本次结果。
        /// 这里刻意不读"报告目录里最新的文件"——本次运行没有结果时界面就不该有任何结果。
        /// </summary>
        private void RefreshScenarioRunState()
        {
            if (_scenarioProgressRow == null || _scenarioProgressBar == null)
                return;

            var record = ScenarioRunRecord.TryRead();
            var status = AxuScenarioRunStatus.TryRead();

            if (record != null)
            {
                RenderRunningState(record, status);
                return;
            }

            UpdateSelectedSuiteUi();
            if (status == null)
            {
                _scenarioProgressRow.style.display = DisplayStyle.None;
                return;
            }

            RenderTerminalState(status);
        }

        /// <summary>在途渲染：状态文件尚未属于本轮时给"等待 Agent"提示（进 Play 到 Agent 写状态之间的空窗）。</summary>
        private void RenderRunningState(ScenarioRunRecord record, AxuScenarioRunStatus status)
        {
            _runSuiteButton?.SetEnabled(false);

            // 总览页的进度文案同步（仅在进度真的推进时刷新，避免 800ms 一次重读趋势 CSV）
            if (status != null && record.MatchesStatus(status) && status.CompletedCount != _lastRunningCompletedCount)
            {
                _lastRunningCompletedCount = status.CompletedCount;
                RefreshOverview();
            }

            var suiteLabel = Path.GetFileName(record.SuitePath ?? string.Empty);
            if (status == null || !record.MatchesStatus(status))
            {
                ShowScenarioProgress($"已进入 Play Mode，等待 Agent 写入运行状态…（suite {suiteLabel}）",
                    0f, 1f, "ax-tone-neutral");
                return;
            }

            if (status.IsRunning)
            {
                var stale = status.IsStale(DateTime.UtcNow);
                var text = $"运行中：第 {status.CompletedCount}/{status.TotalCount} 个场景 · suite {status.SuiteId}";
                if (stale)
                    text += $"（心跳已停止超过 {AxuScenarioRunStatus.StaleMinutes} 分钟，运行可能已中断）";
                ShowScenarioProgress(text, status.CompletedCount, Mathf.Max(1, status.TotalCount),
                    stale ? "ax-tone-warn" : "ax-tone-neutral");
                return;
            }

            // 终态已写出、记录尚未被看门狗清理（同一轮询窗口内）：按终态渲染
            RenderTerminalState(status);
        }

        /// <summary>
        /// 终态渲染：completed → 加载状态文件指名的报告（路径来自状态文件，不是目录里最新的文件）；
        /// error（suite 加载失败 / 报告写盘失败）与 aborted（用户手动停止）→ 清空结果面板并说明原因，
        /// 绝不留着上一次的报告当本次结果。每个 runId 只回显一次（轮询频率 800ms）。
        /// </summary>
        private void RenderTerminalState(AxuScenarioRunStatus status)
        {
            var runId = status.RunId ?? string.Empty;
            var alreadyDelivered = runId.Length > 0 && _deliveredRunId == runId;
            _lastRunningCompletedCount = -1; // 下一轮运行重新按进度刷新总览

            ShowScenarioProgress(
                $"运行结束（{ScenarioRunStatusPresenter.Label(status)}）· suite {status.SuiteId}" +
                (string.IsNullOrEmpty(status.ReportPath) ? "" : $"（报告 {Path.GetFileName(status.ReportPath)}）"),
                status.CompletedCount, Mathf.Max(1, status.TotalCount),
                ScenarioRunStatusPresenter.ToneClass(status));

            if (alreadyDelivered)
                return;

            _deliveredRunId = runId;

            if (status.IsCompleted)
            {
                if (TryLoadScenarioReport(status.ReportPath, out var report, out var path))
                {
                    _scenarioReport = report;
                    _scenarioReportPath = path;
                    RenderScenarioReport();
                    UpdateStatus($"Suite 运行完成：{Path.GetFileName(path)}（趋势已入库）");
                }
                else
                {
                    _scenarioReport = null;
                    _scenarioReportPath = null;
                    RenderScenarioReport();
                    WarnIfPartialRemnants(AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot));
                    UpdateStatus("运行已结束，但报告文件读不到（可能已删除或写盘失败）——详见 Console 与状态文件。");
                }
            }
            else
            {
                // 未完成（中断/基础设施错误）：清空面板，明确"本次没有结果"
                _scenarioReport = null;
                _scenarioReportPath = null;
                RenderScenarioReport();
                _scenarioResultsScroll.Clear();
                var note = new Label(BuildUnfinishedNote(status));
                note.AddToClassList("ax-empty-note");
                _scenarioResultsScroll.Add(note);
                _scenarioSummaryLabel.text = $"suite: {status.SuiteId} · {ScenarioRunStatusPresenter.Label(status)}";
                UpdateStatus($"本次运行未完成：{ScenarioRunStatusPresenter.Label(status)}");
            }

            RefreshOverview();
            UpdateSelectedSuiteUi();
        }

        /// <summary>未完成（error/aborted）的面板说明：原因 + 报告目录位置（历史报告仍在，但不是本次结果）。</summary>
        private static string BuildUnfinishedNote(AxuScenarioRunStatus status)
        {
            var directory = AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot);
            var reason = ScenarioRunStatusPresenter.ReasonText(status);
            return $"本次运行没有产出结果：{reason}\n" +
                   $"（历史报告仍在 {directory} 下，但那是更早的运行——如需查看请用「打开报告 JSON」。）";
        }

        /// <summary>按显式路径加载 scenario 报告（终态回显用：路径来自状态文件，杜绝"目录里最新"的猜测）。</summary>
        private static bool TryLoadScenarioReport(string reportPath, out ScenarioReport report, out string path)
        {
            report = null;
            path = null;
            if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath))
                return false;

            try
            {
                report = AxuTestFramework.Core.Json.AxuJson.FromFile<ScenarioReport>(
                    reportPath, SchemaNames.ScenarioReport);
                path = reportPath;
                return report != null;
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

        /// <summary>
        /// Editor 内运行路径的趋势入库统一由 <see cref="ScenarioRunService"/> 的看门狗在做完收尾时执行
        /// （看板/菜单/pipeline 三条入口同一处），窗口侧不再入库——否则同一次运行会被记两遍。
        /// </summary>
        private void RenderScenarioReport()
        {
            _scenarioResultsScroll.Clear();
            ClearArtifactTextures();
            var report = _scenarioReport;
            if (report == null)
            {
                // 无本次报告：导出与打开入口一并禁用（中断/失败后不得留下可点的"导出上一次结果"）
                UpdateOpenButtons(false);
                _exportJunitButton?.SetEnabled(false);
                _exportHtmlButton?.SetEnabled(false);
                return;
            }

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
                    _scenarioResultsScroll.Add(CreateScenarioResultRow(result));
                }
            }

            _exportJunitButton.SetEnabled(true);
            _exportHtmlButton.SetEnabled(true);
            UpdateOpenButtons(true);
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

        // ---------------------------------------------------------------- 场景报告详情（backlog #4 P0）

        /// <summary>单条 scenario 结果：Foldout 头部 + 一句话结论 + 指标/预算/基线/findings/工件五段详情。</summary>
        private VisualElement CreateScenarioResultRow(ScenarioReportResult result)
        {
            var status = (result.Status ?? "-").Trim().ToLowerInvariant();
            var failed = status == "fail" || status == "error";
            var warned = status == "warn";
            var header = $"{StatusBadge(status)} {result.ScenarioId} · {result.Capability} · " +
                         $"{result.DurationMs:0.#} ms · findings {result.Findings?.Count ?? 0}" +
                         (result.Unbudgeted ? " · 未设预算" : string.Empty);

            var row = new Foldout { text = header, value = failed || warned, name = "ax-scenario-result" };
            row.AddToClassList("ax-scenario-result");
            row.AddToClassList(failed ? "ax-row-fail" : warned ? "ax-row-warn" : "ax-row-pass");

            var conclusion = new Label(ScenarioReportPresenter.BuildConclusion(result));
            conclusion.AddToClassList("ax-scenario-conclusion");
            conclusion.AddToClassList(ScenarioReportPresenter.ToneClass(status));
            row.Add(conclusion);

            AddMetricsSection(row, result);
            AddBudgetSection(row, result);
            AddBaselineSection(row, result);
            AddFindingsSection(row, result);
            AddArtifactsSection(row, result);
            return row;
        }

        /// <summary>指标表：按 collector 分组，列出每个 stat 的聚合值（BaselineDiff.Flatten 同键口径）。</summary>
        private static void AddMetricsSection(VisualElement parent, ScenarioReportResult result)
        {
            var metrics = result.Metrics;
            if (metrics == null || metrics.Count == 0)
            {
                AddDetailNote(parent, "指标", "（无度量产出）");
                return;
            }

            var total = metrics.Where(pair => pair.Value != null).Sum(pair => pair.Value.Count);
            AddDetailHeader(parent, $"指标（{total}）");

            foreach (var collector in metrics.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (collector.Value == null || collector.Value.Count == 0)
                    continue;
                var sub = new Label(collector.Key);
                sub.AddToClassList("ax-detail-subheader");
                parent.Add(sub);
                foreach (var stat in collector.Value.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    parent.Add(CreateKeyValueRow(stat.Key, ScenarioReportPresenter.FormatMetricValue(stat.Value)));
            }
        }

        /// <summary>预算表：实际值 vs 门槛，按 verdict 着色（missing 指标缺失单独标注）。</summary>
        private static void AddBudgetSection(VisualElement parent, ScenarioReportResult result)
        {
            var budgets = result.Budgets;
            if (budgets == null || budgets.Count == 0)
            {
                AddDetailNote(parent, "预算", result.Unbudgeted
                    ? "（未设预算——unbudgeted，不会被预算拦截）"
                    : "（无预算判定）");
                return;
            }

            AddDetailHeader(parent, $"预算（{budgets.Count}）");
            foreach (var budget in budgets)
            {
                if (budget == null)
                    continue;

                string expression;
                if (budget.Missing)
                    expression = "指标缺失";
                else if (string.Equals(budget.Op, "range", StringComparison.OrdinalIgnoreCase))
                    expression = $"需落在规定区间{budget.Unit}";
                else
                    expression = $"门槛 {budget.Op} {ScenarioReportPresenter.FormatMetricValue(budget.Threshold ?? 0)}{budget.Unit}";

                var actual = budget.Actual.HasValue
                    ? ScenarioReportPresenter.FormatMetricValue(budget.Actual.Value) + (budget.Unit ?? string.Empty)
                    : "-";
                parent.Add(CreateKeyValueRow(budget.Metric,
                    $"实际 {actual} · {expression}{(budget.Missing ? "（判定恒 fail）" : string.Empty)}",
                    budget.Verdict));
            }
        }

        /// <summary>基线对比表：base → current + 变化率，按 verdict 着色（verdict 已含方向语义）。</summary>
        private static void AddBaselineSection(VisualElement parent, ScenarioReportResult result)
        {
            var entries = result.BaselineDiff;
            if (entries == null || entries.Count == 0)
            {
                AddDetailNote(parent, "基线对比", "（无基线对比数据——未配置 baseline 或首次运行）");
                return;
            }

            AddDetailHeader(parent, $"基线对比（{entries.Count}）");
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;
                var delta = entry.DeltaRatio.ToString("+0.#%;-0.#%;0%", CultureInfo.InvariantCulture);
                var value =
                    $"{ScenarioReportPresenter.FormatMetricValue(entry.Base)} → " +
                    $"{ScenarioReportPresenter.FormatMetricValue(entry.Current)}（{delta}）";
                parent.Add(CreateKeyValueRow(entry.Metric, value, entry.Verdict));
            }
        }

        /// <summary>findings 列表：严重级 + 人话翻译（原始 message 与 code 进 tooltip）。</summary>
        private static void AddFindingsSection(VisualElement parent, ScenarioReportResult result)
        {
            var findings = result.Findings;
            if (findings == null || findings.Count == 0)
            {
                AddDetailNote(parent, "结论 / findings", "（无 findings）");
                return;
            }

            AddDetailHeader(parent, $"结论 / findings（{findings.Count}）");
            foreach (var finding in findings)
            {
                if (finding == null)
                    continue;

                var severity = string.IsNullOrEmpty(finding.SeverityText) ? "info" : finding.SeverityText;
                var row = new VisualElement();
                row.AddToClassList("ax-kv-row");
                var badge = new Label(severity);
                badge.AddToClassList("ax-finding-badge");
                badge.AddToClassList(ScenarioReportPresenter.ToneClass(severity));
                var text = new Label(ScenarioReportPresenter.TranslateFinding(finding));
                text.AddToClassList("ax-finding-text");
                text.AddToClassList(ScenarioReportPresenter.ToneClass(severity));
                text.tooltip = $"[{finding.Code}] {finding.Message}";
                row.Add(badge);
                row.Add(text);
                parent.Add(row);
            }
        }

        /// <summary>工件：图片（png/jpg/jpeg）渲染缩略图画廊，其余以可点击链接列出（点击用默认程序打开）。</summary>
        private void AddArtifactsSection(VisualElement parent, ScenarioReportResult result)
        {
            var artifacts = result.Artifacts;
            if (artifacts == null || artifacts.Count == 0)
            {
                AddDetailNote(parent, "工件（截图等）", "（本次运行未产出工件）");
                return;
            }

            AddDetailHeader(parent, $"工件（{artifacts.Count}）");

            var gallery = new VisualElement();
            gallery.AddToClassList("ax-artifact-gallery");
            var galleryUsed = false;
            foreach (var artifact in artifacts)
            {
                if (!ScenarioReportPresenter.IsImageArtifact(artifact))
                    continue;
                gallery.Add(CreateArtifactThumbnail(
                    ScenarioReportPresenter.ResolveArtifactPath(_scenarioReportPath, artifact), artifact));
                galleryUsed = true;
            }
            if (galleryUsed)
                parent.Add(gallery);

            foreach (var artifact in artifacts)
            {
                if (ScenarioReportPresenter.IsImageArtifact(artifact))
                    continue;
                var fullPath = ScenarioReportPresenter.ResolveArtifactPath(_scenarioReportPath, artifact);
                var row = new VisualElement();
                row.AddToClassList("ax-kv-row");
                var link = new Label(artifact);
                link.AddToClassList("ax-artifact-link");
                link.tooltip = fullPath + "\n（点击用默认程序打开）";
                link.RegisterCallback<ClickEvent>(_ => OpenPath(fullPath));
                row.Add(link);
                parent.Add(row);
            }
        }

        /// <summary>单张截图缩略图（160×90 预览 + 文件名；点击用默认程序打开；加载失败显示占位）。</summary>
        private VisualElement CreateArtifactThumbnail(string fullPath, string relativePath)
        {
            var box = new VisualElement();
            box.AddToClassList("ax-artifact-box");

            var texture = LoadArtifactTexture(fullPath);
            if (texture != null)
            {
                var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
                image.AddToClassList("ax-artifact-thumb");
                image.tooltip = relativePath + "\n（点击用默认程序打开）";
                image.RegisterCallback<ClickEvent>(_ => OpenPath(fullPath));
                box.Add(image);
            }
            else
            {
                var missing = new Label(File.Exists(fullPath) ? "（无法解码）" : "（图片缺失）");
                missing.AddToClassList("ax-artifact-thumb-missing");
                missing.tooltip = fullPath;
                box.Add(missing);
            }

            var label = new Label(Path.GetFileName(relativePath));
            label.AddToClassList("ax-artifact-label");
            label.tooltip = relativePath;
            box.Add(label);
            return box;
        }

        /// <summary>读取图片纹理进缓存（同路径只解码一次；非图片/损坏返回 null）。</summary>
        private Texture2D LoadArtifactTexture(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return null;
            if (_artifactTextures.TryGetValue(fullPath, out var cached) && cached != null)
                return cached;

            try
            {
                var bytes = File.ReadAllBytes(fullPath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    DestroyImmediate(texture);
                    return null;
                }
                _artifactTextures[fullPath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AxuTestFramework] 缩略图加载失败（{Path.GetFileName(fullPath)}）: {ex.Message}");
                return null;
            }
        }

        /// <summary>释放缩略图纹理缓存（重渲染报告前与关窗时调用，避免纹理泄漏）。</summary>
        private void ClearArtifactTextures()
        {
            foreach (var texture in _artifactTextures.Values)
            {
                if (texture != null)
                    DestroyImmediate(texture);
            }
            _artifactTextures.Clear();
        }

        private void OnDestroy()
        {
            ClearArtifactTextures();
        }

        /// <summary>报告加载态同步三个打开按钮可用性。</summary>
        private void UpdateOpenButtons(bool hasReport)
        {
            var hasPath = hasReport && !string.IsNullOrEmpty(_scenarioReportPath);
            _openJsonButton?.SetEnabled(hasPath);
            _openArtifactsButton?.SetEnabled(hasPath);
            _openHtmlButton?.SetEnabled(hasPath);
        }

        private void OpenReportJson()
        {
            OpenPath(_scenarioReportPath);
        }

        private void OpenArtifactsFolder()
        {
            var directory = AxuTestReportPaths.GetArtifactsDirectoryFor(_scenarioReportPath);
            if (Directory.Exists(directory))
            {
                EditorUtility.RevealInFinder(directory);
                UpdateStatus("已打开工件目录: " + Relativize(AxuTestReportPaths.ProjectRootFullPath, directory));
            }
            else
            {
                UpdateStatus("本次运行未产出工件目录（无截图或自定义工件）: " + directory);
            }
        }

        /// <summary>打开 HTML 摘要：报告旁生成（若尚无）`{报告名}.summary.html` 后用默认浏览器打开。</summary>
        private void OpenReportHtml()
        {
            if (_scenarioReport == null || string.IsNullOrEmpty(_scenarioReportPath) || !File.Exists(_scenarioReportPath))
                return;

            try
            {
                var htmlPath = Path.Combine(
                    Path.GetDirectoryName(_scenarioReportPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(_scenarioReportPath) + ".summary.html");
                if (!File.Exists(htmlPath))
                    HtmlSummaryExporter.ExportToFile(_scenarioReport, _scenarioReport.SuiteId, htmlPath);
                OpenPath(htmlPath);
                UpdateStatus("HTML 摘要: " + htmlPath);
            }
            catch (Exception ex)
            {
                UpdateStatus("HTML 摘要生成失败: " + ex.Message);
                Debug.LogWarning("[AxuTestFramework] HTML 摘要生成失败: " + ex.Message);
            }
        }

        /// <summary>用系统默认程序打开文件；抛异常时退回 Finder/资源管理器定位（均失败仅记状态栏）。</summary>
        private void OpenPath(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                {
                    UpdateStatus("文件不存在: " + fullPath);
                    return;
                }
                EditorUtility.OpenWithDefaultApp(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AxuTestFramework] 默认程序打开失败，改为定位文件: " + ex.Message);
                try
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
                catch (Exception revealEx)
                {
                    UpdateStatus("打开文件失败: " + revealEx.Message);
                }
            }
        }

        // ---------------------------------------------------------------- 运行进度（状态文件轮询）

        private void ShowScenarioProgress(string text, float value, float highValue, string toneClass)
        {
            _scenarioProgressRow.style.display = DisplayStyle.Flex;
            _scenarioProgressLabel.text = text;
            foreach (var tone in new[] { "ax-tone-ok", "ax-tone-warn", "ax-tone-bad", "ax-tone-neutral" })
                _scenarioProgressLabel.EnableInClassList(tone, tone == toneClass);
            _scenarioProgressBar.highValue = Mathf.Max(1f, highValue);
            _scenarioProgressBar.value = Mathf.Clamp(value, 0f, _scenarioProgressBar.highValue);
        }

        private static void AddDetailHeader(VisualElement parent, string text)
        {
            var header = new Label(text);
            header.AddToClassList("ax-detail-header");
            parent.Add(header);
        }

        private static void AddDetailNote(VisualElement parent, string section, string note)
        {
            AddDetailHeader(parent, section);
            var label = new Label(note);
            label.AddToClassList("ax-detail-note");
            parent.Add(label);
        }

        /// <summary>名称 + 值（+ 可选判定）行；判定按 verdict 着色调（pass 绿 / warn 黄 / fail 红）。</summary>
        private static VisualElement CreateKeyValueRow(string name, string value, string verdict = null)
        {
            var row = new VisualElement();
            row.AddToClassList("ax-kv-row");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("ax-kv-name");
            nameLabel.tooltip = name;

            var valueLabel = new Label(value);
            valueLabel.AddToClassList("ax-kv-value");
            valueLabel.tooltip = value;

            row.Add(nameLabel);
            row.Add(valueLabel);

            if (!string.IsNullOrEmpty(verdict))
            {
                var verdictLabel = new Label(verdict);
                verdictLabel.AddToClassList("ax-kv-verdict");
                verdictLabel.AddToClassList(ScenarioReportPresenter.ToneClass(verdict));
                row.Add(verdictLabel);
            }
            return row;
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
