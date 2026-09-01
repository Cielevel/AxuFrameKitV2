using System;
using System.IO;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Trend;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 报告 → 趋势 CSV 入库工具（Phase 4：axu_scenario_run 完成回调与 axu_player_suite_run 共用）。
    /// 报告 JSON 反序列化为 <see cref="ScenarioReport"/> 后经 <see cref="TrendStore.FromReport"/> 扁平化，
    /// 追加到 <see cref="AxuTrendPaths.TrendCsvPath"/>（Library/AxuTestReports/trend/trend.csv）。
    /// </summary>
    public static class AxuScenarioTrendUtil
    {
        /// <summary>趋势 CSV 完整物理路径。</summary>
        public static string TrendCsvFullPath => AxuTestReportPaths.ToFullPath(AxuTrendPaths.TrendCsvPath);

        /// <summary>Editor 侧 scenario 报告根目录完整物理路径（Library/AxuTestReports/scenario）。</summary>
        public static string ScenarioReportRootFullPath => AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot);

        /// <summary>
        /// 读报告 JSON 并把指标/结果行追加进趋势 CSV。
        /// 返回 false 时 <paramref name="error"/> 给出原因（报告缺失/解析失败/写盘失败），不抛出。
        /// </summary>
        public static bool TryAppendReport(string reportPath, out string suiteId, out string error)
        {
            suiteId = null;
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(reportPath))
                {
                    error = "报告路径为空（状态文件未携带 reportPath）。";
                    return false;
                }

                var fullPath = AxuTestReportPaths.ToFullPath(reportPath);
                if (!File.Exists(fullPath))
                {
                    error = $"报告文件不存在: {fullPath}";
                    return false;
                }

                var report = JsonConvert.DeserializeObject<ScenarioReport>(File.ReadAllText(fullPath));
                if (report == null)
                {
                    error = "报告反序列化结果为 null。";
                    return false;
                }

                suiteId = report.SuiteId;
                TrendStore.Append(TrendCsvFullPath, TrendStore.FromReport(report, report.SuiteId));
                return true;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }
    }
}
