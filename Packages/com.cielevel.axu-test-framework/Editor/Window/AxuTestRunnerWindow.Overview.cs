using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuTestFramework.Core;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Trend;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// <see cref="AxuTestRunnerWindow"/> 的总览页（默认页，2026-09-02 面板可读化改造新增）。
    /// <para>面向"不看技术细节的使用者"：一屏给出 ①最近一次场景套件运行的大结果徽章
    /// （通过/警告/失败/尚无记录 + 中文摘要）②最近 8 次运行的趋势速览（彩色圆点序列）
    /// ③三个大按钮（运行场景套件 / 查看趋势 / 刷新）。数据同源：最新 scenario 报告
    /// （排除 *.partial.json）+ 趋势 CSV（<see cref="TrendStore.Read"/>）。</para>
    /// </summary>
    public partial class AxuTestRunnerWindow
    {
        private VisualElement _overviewPage;
        private Label _overviewBadgeLabel;
        private Label _overviewDetailLabel;
        private Label _overviewLastRunLabel;
        private Button _overviewOpenReportButton;
        private ScrollView _overviewTrendScroll;

        /// <summary>最近一次 scenario 报告（总览页数据快照；"查看详情"转发给 Scenario 页渲染）。</summary>
        private ScenarioReport _overviewReport;
        private string _overviewReportPath;

        // ------------------------------------------------------------------ 构建

        private void BuildOverviewPage()
        {
            _overviewPage = new VisualElement { name = "ax-page-overview" };
            _overviewPage.style.flexGrow = 1f;

            var layout = new VisualElement { name = "ax-overview-layout" };
            layout.style.flexGrow = 1f;
            layout.style.overflow = Overflow.Hidden;
            layout.style.flexDirection = FlexDirection.Column;
            _overviewPage.Add(layout);

            // 卡 1：最近一次运行的大结果
            var runCard = new VisualElement { name = "ax-overview-run-card" };
            runCard.AddToClassList("ax-overview-card");

            var runRow = new VisualElement { name = "ax-overview-run-row" };
            runRow.style.flexDirection = FlexDirection.Row;
            runRow.style.alignItems = Align.Center;

            _overviewBadgeLabel = new Label("—");
            _overviewBadgeLabel.AddToClassList("ax-badge");

            var infoCol = new VisualElement { name = "ax-overview-run-info" };
            infoCol.style.flexGrow = 1f;
            infoCol.style.flexDirection = FlexDirection.Column;
            _overviewLastRunLabel = new Label("尚无运行记录");
            _overviewLastRunLabel.AddToClassList("ax-overview-subtitle");
            _overviewDetailLabel = new Label("在「场景套件」页选择一套件并运行，结果会汇总到这里。");
            _overviewDetailLabel.AddToClassList("ax-overview-note");
            infoCol.Add(_overviewLastRunLabel);
            infoCol.Add(_overviewDetailLabel);

            _overviewOpenReportButton = new Button(OpenLatestReportInScenarioPage) { text = "查看详情", name = "ax-overview-open-report" };
            _overviewOpenReportButton.AddToClassList("ax-secondary-button");
            _overviewOpenReportButton.SetEnabled(false);

            runRow.Add(_overviewBadgeLabel);
            runRow.Add(infoCol);
            runRow.Add(_overviewOpenReportButton);
            runCard.Add(runRow);
            layout.Add(runCard);

            // 卡 2：趋势速览
            var trendCard = new VisualElement { name = "ax-overview-trend-card" };
            trendCard.AddToClassList("ax-overview-card");
            var trendHeader = new VisualElement { name = "ax-overview-trend-header" };
            trendHeader.style.flexDirection = FlexDirection.Row;
            trendHeader.style.alignItems = Align.Center;
            var trendTitle = new Label("趋势速览（最近 8 次运行，最新在右）");
            trendTitle.AddToClassList("ax-pane-header");
            trendTitle.style.flexGrow = 1f;
            var goTrendButton = new Button(GoToTrendPage) { text = "查看趋势" };
            goTrendButton.AddToClassList("ax-secondary-button");
            trendHeader.Add(trendTitle);
            trendHeader.Add(goTrendButton);
            trendCard.Add(trendHeader);

            _overviewTrendScroll = new ScrollView { name = "ax-overview-trend-scroll" };
            _overviewTrendScroll.AddToClassList("ax-scroll");
            trendCard.Add(_overviewTrendScroll);
            layout.Add(trendCard);

            // 卡 3：快捷操作
            var quickCard = new VisualElement { name = "ax-overview-quick-card" };
            quickCard.AddToClassList("ax-overview-card");
            var quickRow = new VisualElement { name = "ax-overview-quick-row" };
            quickRow.style.flexDirection = FlexDirection.Row;

            var runSuiteButton = new Button(GoToScenarioPage) { text = "▶  运行场景套件" };
            runSuiteButton.AddToClassList("ax-big-button");
            runSuiteButton.tooltip = "跳转到「场景套件」页，选择套件并运行";
            var viewTrendButton = new Button(GoToTrendPage) { text = "查看趋势" };
            viewTrendButton.AddToClassList("ax-big-button");
            viewTrendButton.tooltip = "跳转到「趋势」页，查看各指标历次序列与环比";
            var refreshButton = new Button(RefreshOverview) { text = "刷新" };
            refreshButton.AddToClassList("ax-big-button");
            refreshButton.tooltip = "重新读取最近报告与趋势";
            quickRow.Add(runSuiteButton);
            quickRow.Add(viewTrendButton);
            quickRow.Add(refreshButton);
            quickCard.Add(quickRow);

            var quickHint = new Label("一句话流程：第一步选套件 → 第二步运行 → 跑完回来这里看结果与趋势。");
            quickHint.AddToClassList("ax-overview-note");
            quickCard.Add(quickHint);

            layout.Add(quickCard);
            rootVisualElement.Add(_overviewPage);
        }

        // ------------------------------------------------------------------ 数据与渲染

        /// <summary>刷新总览（读最新 scenario 报告 + 趋势 CSV 并重渲染；两处读盘失败均静默降级为空态）。</summary>
        private void RefreshOverview()
        {
            if (_overviewPage == null)
                return;

            TryDiscoverLatestScenarioReport(out _overviewReport, out _overviewReportPath);
            RenderOverviewBadge();
            RenderOverviewTrend();
        }

        /// <summary>找 scenario 报告目录里最新一份完整报告（排除 *.partial.json——soak 中断残留不是"最近一次"）。</summary>
        private static bool TryDiscoverLatestScenarioReport(out ScenarioReport report, out string path)
        {
            report = null;
            path = null;
            try
            {
                var directory = AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot);
                if (!Directory.Exists(directory))
                    return false;

                var newest = new DirectoryInfo(directory)
                    .EnumerateFiles("*.json")
                    .Where(f => !f.Name.EndsWith(".partial.json", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (newest == null)
                    return false;

                report = AxuJson.FromFile<ScenarioReport>(newest.FullName, SchemaNames.ScenarioReport);
                if (report == null)
                    return false;

                path = newest.FullName;
                return true;
            }
            catch (Exception ex)
            {
                report = null;
                path = null;
                Debug.LogWarning($"[AxuTestFramework] 总览页读取最近报告失败（忽略）: {ex.Message}");
                return false;
            }
        }

        /// <summary>大徽章：无记录=灰、全部通过=绿、仅警告=黄、有失败=红。详情行给出一句话摘要。</summary>
        private void RenderOverviewBadge()
        {
            _overviewOpenReportButton.SetEnabled(_overviewReport != null);

            var report = _overviewReport;
            if (report == null)
            {
                SetBadge("ax-badge-neutral", "—  尚无运行记录");
                _overviewLastRunLabel.text = "还没有场景套件的运行结果";
                _overviewDetailLabel.text = "在「场景套件」页选择一套件并运行，结果会汇总到这里（进度与报告也会在状态栏提示）。";
                return;
            }

            var summary = report.Summary;
            var total = summary?.Total ?? 0;
            var passed = summary?.Passed ?? 0;
            var warned = summary?.Warned ?? 0;
            var failed = summary?.Failed ?? 0;
            var elapsed = FormatElapsed(report.StartedAt, report.EndedAt);

            if (report.Truncated == true)
            {
                SetBadge("ax-badge-warn", "⚠  运行中 / 中断");
                _overviewLastRunLabel.text = $"套件 {report.SuiteId} · {FormatLocalTime(report.StartedAt)}";
                _overviewDetailLabel.text = "这是一份部分报告（运行未正常结束，数据以前段为准）。";
                return;
            }

            switch (report.ExitCode)
            {
                case ScenarioExitCode.Success when failed == 0:
                    SetBadge("ax-badge-ok", "✓  全部通过");
                    break;
                case ScenarioExitCode.WarningsOnly when failed == 0:
                    SetBadge("ax-badge-warn", "⚠  通过（有警告）");
                    break;
                default:
                    SetBadge("ax-badge-bad", "✗  有失败");
                    break;
            }

            _overviewLastRunLabel.text = $"套件 {report.SuiteId} · 运行于 {FormatLocalTime(report.StartedAt)}";
            _overviewDetailLabel.text = $"{total} 项 · {passed} 通过 · {warned} 警告 · {failed} 失败" +
                                        (elapsed != null ? $" · 用时 {elapsed}" : "") +
                                        (report.ExitCode != ScenarioExitCode.Success ? $" · 退出码 {(int)report.ExitCode}" : "");
        }

        private void SetBadge(string colorClass, string text)
        {
            _overviewBadgeLabel.text = text;
            _overviewBadgeLabel.EnableInClassList("ax-badge-ok", colorClass == "ax-badge-ok");
            _overviewBadgeLabel.EnableInClassList("ax-badge-warn", colorClass == "ax-badge-warn");
            _overviewBadgeLabel.EnableInClassList("ax-badge-bad", colorClass == "ax-badge-bad");
            _overviewBadgeLabel.EnableInClassList("ax-badge-neutral", colorClass == "ax-badge-neutral");
        }

        /// <summary>趋势速览：按 scenario 分组（最近活动在前），每行 = 场景名 + 最近 8 次运行彩色圆点 + 最近时间。</summary>
        private void RenderOverviewTrend()
        {
            if (_overviewTrendScroll == null)
                return;
            _overviewTrendScroll.Clear();

            List<TrendPoint> points = null;
            try
            {
                var csvPath = AxuTestReportPaths.ToFullPath(AxuTrendPaths.TrendCsvPath);
                if (File.Exists(csvPath))
                    points = TrendStore.Read(csvPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AxuTestFramework] 总览页读取趋势失败（忽略）: {ex.Message}");
                points = null;
            }

            if (points == null || points.Count == 0)
            {
                var empty = new Label("暂无趋势记录——在「场景套件」页运行一次套件后自动入库。");
                empty.AddToClassList("ax-empty-note");
                _overviewTrendScroll.Add(empty);
                return;
            }

            var runRows = points.Where(p => string.IsNullOrEmpty(p.Metric)).ToList();
            var byScenario = runRows
                .GroupBy(p => p.Scenario)
                .OrderByDescending(g => g.Max(p => p.TimestampUtc), StringComparer.Ordinal)
                .ToList();

            foreach (var group in byScenario)
            {
                var row = new VisualElement { name = "ax-overview-trend-row" };
                row.AddToClassList("ax-overview-trend-row");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var scenarioName = new Label(group.Key);
                scenarioName.style.width = 180f;
                scenarioName.AddToClassList("ax-overview-trend-name");

                var dots = new VisualElement { name = "ax-overview-trend-dots" };
                dots.style.flexGrow = 1f;
                dots.style.flexDirection = FlexDirection.Row;
                var last8 = LastN(group.OrderBy(p => p.TimestampUtc, StringComparer.Ordinal).ToList(), 8);
                foreach (var point in last8)
                    dots.Add(CreateTrendDot(point));

                var lastTime = new Label(FormatLocalTime(last8[last8.Count - 1].TimestampUtc));
                lastTime.AddToClassList("ax-overview-trend-time");

                row.Add(scenarioName);
                row.Add(dots);
                row.Add(lastTime);

                var captured = group.Key;
                row.RegisterCallback<PointerDownEvent>(_ => ShowTrendForScenario(captured));
                row.tooltip = $"点击查看「{captured}」的完整趋势（各指标序列与环比）";

                _overviewTrendScroll.Add(row);
            }
        }

        /// <summary>单个运行结果圆点：pass=绿 / warn=黄 / fail·error=红 / 其他=灰。</summary>
        private static Label CreateTrendDot(TrendPoint point)
        {
            var dot = new Label("●");
            dot.AddToClassList("ax-trend-dot");
            var result = (point.Result ?? "-").Trim().ToLowerInvariant();
            switch (result)
            {
                case "pass": dot.AddToClassList("ax-trend-dot-ok"); break;
                case "warn": dot.AddToClassList("ax-trend-dot-warn"); break;
                case "fail":
                case "error": dot.AddToClassList("ax-trend-dot-bad"); break;
                default: dot.AddToClassList("ax-trend-dot-neutral"); break;
            }
            dot.tooltip = $"{result} · {FormatLocalTime(point.TimestampUtc)} · 耗时 {point.DurationMs / 1000d:0.#} s";
            return dot;
        }

        // ------------------------------------------------------------------ 跳转

        private void GoToScenarioPage()
        {
            SetPage("scenario");
            UpdateStatus("请在左侧选择一套件，然后点右侧「▶ 运行选中的套件」。");
        }

        private void GoToTrendPage()
        {
            SetPage("trend");
        }

        /// <summary>把总览页最近报告转发给 Scenario 页渲染（复用同一份反序列化结果，避免双读）。</summary>
        private void OpenLatestReportInScenarioPage()
        {
            if (_overviewReport == null)
                return;
            _scenarioReport = _overviewReport;
            _scenarioReportPath = _overviewReportPath;
            RenderScenarioReport();
            SetPage("scenario");
        }

        private void ShowTrendForScenario(string scenario)
        {
            SetPage("trend");
            if (_trendScenarioField == null)
                return;
            // SetPage("trend") 已执行 RefreshTrend（重建 choices 并渲染默认场景），这里切到目标场景后强制重渲染
            _trendScenarioField.SetValueWithoutNotify(scenario);
            RenderTrend();
        }

        // ------------------------------------------------------------------ 工具（纯格式化）

        private static string FormatLocalTime(string utcIso)
        {
            if (string.IsNullOrEmpty(utcIso))
                return "-";
            if (DateTime.TryParse(utcIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return utcIso;
        }

        private static string FormatElapsed(string startedIso, string endedIso)
        {
            if (string.IsNullOrEmpty(startedIso) || string.IsNullOrEmpty(endedIso))
                return null;
            if (!DateTime.TryParse(startedIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var start))
                return null;
            if (!DateTime.TryParse(endedIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var end))
                return null;
            var span = end - start;
            if (span.TotalSeconds < 0)
                return null;
            return span.TotalMinutes >= 1 ? $"{span.TotalMinutes:0.#} 分钟" : $"{span.TotalSeconds:0.#} 秒";
        }
    }
}
