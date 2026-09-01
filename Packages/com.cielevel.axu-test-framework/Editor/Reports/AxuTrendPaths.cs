using System.IO;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Phase 4 趋势与 scenario 报告的 Editor 侧路径约定。
    /// 趋势 CSV 为追加式长表（schema 见 Core/Trend/TrendStore.cs）。
    /// </summary>
    public static class AxuTrendPaths
    {
        public const string TrendSchema = "axu-trend/1";

        /// <summary>趋势库根目录（Library 下，不入库）。</summary>
        public static string TrendRoot => Path.Combine("Library", "AxuTestReports", "trend");

        /// <summary>趋势 CSV 单文件长表路径。</summary>
        public static string TrendCsvPath => Path.Combine(TrendRoot, "trend.csv");

        /// <summary>Editor 侧 scenario 报告目录（Phase 1 既有约定）。</summary>
        public static string ScenarioReportRoot => Path.Combine("Library", "AxuTestReports", "scenario");
    }
}
