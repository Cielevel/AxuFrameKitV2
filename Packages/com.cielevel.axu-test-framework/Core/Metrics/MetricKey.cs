namespace AxuTestFramework.Core.Metrics
{
    /// <summary>指标键（&lt;collector&gt;.&lt;stat&gt; 两段式）解析工具。</summary>
    public static class MetricKey
    {
        /// <summary>
        /// 把指标键按最后一个 '.' 分为 collector 与 stat 两段（如 "fps.avg"、"memory.allocatedMax"；
        /// stat 段可继续含点，如 custom 通道的 "scope.AI.Tick.p95"）；
        /// 无 '.'、首尾为 '.' 或任一段为空返回 false。
        /// </summary>
        public static bool TrySplit(string metricKey, out string collector, out string stat)
        {
            collector = null;
            stat = null;
            if (string.IsNullOrEmpty(metricKey)) return false;

            var dot = metricKey.LastIndexOf('.');
            if (dot <= 0 || dot == metricKey.Length - 1) return false;

            collector = metricKey.Substring(0, dot);
            stat = metricKey.Substring(dot + 1);
            return true;
        }
    }
}
