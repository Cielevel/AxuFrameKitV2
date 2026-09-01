using System;
using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Core.Trend;
using Unity.Pipeline.Commands;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 趋势查询 pipeline 命令（方案 §3.5，Phase 4）：<c>axu_trend_summary</c> 读
    /// Library/AxuTestReports/trend/trend.csv（axu-scenario run / player run 完成时入库），输出各
    /// scenario 各 metric 最近 N 次序列与最近两次的有向 ChangePercent（正值恒表示变差，
    /// 见 <see cref="TrendSummary.ChangePercent"/>）。metric 为空串的 run 级行以 "(run)" 标注。
    /// </summary>
    public static class AxuTrendCommand
    {
        /// <summary>run 级行（Metric 为空串、Value 为 null）的展示名。</summary>
        public const string RunRowMetricLabel = "(run)";

        /// <summary>
        /// 趋势汇总查询。
        /// </summary>
        [CliCommand("axu_trend_summary", "Summarize the trend CSV (Library/AxuTestReports/trend/trend.csv): per scenario+metric last-N series and last-two ChangePercent (positive = worse)", MainThreadRequired = false, Tags = new[] { "tests", "axu", "trend" })]
        public static object GetTrendSummary(
            [CliArg("scenario", "Optional scenario id filter (exact match; empty = all scenarios)")] string scenario = "",
            [CliArg("suite", "Optional suite id filter (exact match; empty = all suites)")] string suite = "",
            [CliArg("last_n", "Number of most recent points per metric series (default 10)")] int last_n = 10)
        {
            try
            {
                var trendPath = AxuScenarioTrendUtil.TrendCsvFullPath;
                if (!System.IO.File.Exists(trendPath))
                {
                    return new
                    {
                        status = "not_found",
                        trendPath,
                        message = "趋势 CSV 尚不存在：先经 axu_scenario_run 或 axu_player_suite_run 完成一次运行（完成时自动入库）。"
                    };
                }

                if (last_n <= 0)
                    last_n = 10;

                var points = TrendStore.Read(trendPath)
                    .Where(p => p != null)
                    .Where(p => string.IsNullOrWhiteSpace(scenario) || p.Scenario == scenario)
                    .Where(p => string.IsNullOrWhiteSpace(suite) || p.Suite == suite)
                    .ToList();

                var scenarios = new List<object>();
                foreach (var scenarioGroup in points.GroupBy(p => p.Scenario).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    var metrics = new List<object>();
                    foreach (var metricGroup in scenarioGroup.GroupBy(p => p.Metric ?? string.Empty).OrderBy(g => g.Key, StringComparer.Ordinal))
                    {
                        // Series：TimestampUtc 升序（ISO 8601 字符串序即时序）后取尾 N 个
                        var series = TrendSummary.Series(scenarioGroup.ToList(), scenarioGroup.Key, metricGroup.Key);
                        var lastN = series.Skip(Math.Max(0, series.Count - last_n)).ToList();

                        // ChangePercent 仅对带值的 metric 行有意义（run 级行 Value 为 null）
                        double? changePercent = null;
                        var valuePoints = lastN.Where(p => p.Value.HasValue).ToList();
                        if (valuePoints.Count >= 2)
                        {
                            var older = valuePoints[valuePoints.Count - 2];
                            var newer = valuePoints[valuePoints.Count - 1];
                            try
                            {
                                changePercent = Math.Round(TrendSummary.ChangePercent(older, newer), 3);
                            }
                            catch (Exception)
                            {
                                // 比例无定义等边界：保持 null
                            }
                        }

                        metrics.Add(new
                        {
                            metric = metricGroup.Key.Length == 0 ? RunRowMetricLabel : metricGroup.Key,
                            isRunRow = metricGroup.Key.Length == 0,
                            pointCount = series.Count,
                            series = lastN.Select(p => new
                            {
                                timestampUtc = p.TimestampUtc,
                                value = p.Value,
                                result = p.Result,
                                durationMs = p.DurationMs
                            }).ToList(),
                            changePercent,
                            changePercentSemantics = "positive = worse (direction-aware, see TrendSummary.ChangePercent)"
                        });
                    }

                    var latest = scenarioGroup.OrderByDescending(p => p.TimestampUtc, StringComparer.Ordinal).First();
                    scenarios.Add(new
                    {
                        scenario = scenarioGroup.Key,
                        suite = latest.Suite,
                        capability = latest.Capability,
                        latestResult = latest.Result,
                        latestExitCode = latest.ExitCode,
                        metrics
                    });
                }

                return new
                {
                    status = "ok",
                    trendPath,
                    lastN = last_n,
                    filters = new { scenario = scenario ?? "", suite = suite ?? "" },
                    totalPoints = points.Count,
                    scenarioCount = scenarios.Count,
                    scenarios
                };
            }
            catch (Exception ex)
            {
                return new { status = "error", message = $"{ex.GetType().Name}: {ex.Message}" };
            }
        }
    }
}
