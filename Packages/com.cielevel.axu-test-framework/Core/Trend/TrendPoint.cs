namespace AxuTestFramework.Core.Trend
{
    /// <summary>
    /// 趋势 CSV 的一行（schema axu-trend/1）：metric 级行（Value 非 null）逐指标记录，
    /// run 级行（Metric = ""、Value = null）记录 scenario 整体结果/退出码/耗时。
    /// </summary>
    public class TrendPoint
    {
        /// <summary>时间戳（UTC ISO 8601，取自报告 startedAt）。</summary>
        public string TimestampUtc;

        /// <summary>所属 suite id。</summary>
        public string Suite;

        /// <summary>扇出后的 scenario id（含 @profile 后缀）。</summary>
        public string Scenario;

        /// <summary>能力 id。</summary>
        public string Capability;

        /// <summary>结果串：pass / warn / fail / error。</summary>
        public string Result;

        /// <summary>进程退出码（0/1/2/3）。</summary>
        public int ExitCode;

        /// <summary>耗时毫秒（真实时间）。</summary>
        public double DurationMs;

        /// <summary>扁平指标键（&lt;collector&gt;.&lt;stat&gt;）；空串 = run 级行。</summary>
        public string Metric;

        /// <summary>指标值；run 级行为 null。</summary>
        public double? Value;
    }
}
