namespace AxuTestFramework.Core.Metrics
{
    /// <summary>指标对"劣化"的敏感方向。</summary>
    public enum MetricSensitivity
    {
        /// <summary>越高越好（如 fps）：仅下降计入劣化。</summary>
        HigherIsBetter,
        /// <summary>越低越好（如帧时间/内存）：仅上升计入劣化。</summary>
        LowerIsBetter,
        /// <summary>方向不定（如 drawcall 数、计数类）：升降都计入劣化（对称判定）。</summary>
        Symmetric,
    }

    /// <summary>
    /// 指标键（&lt;collector&gt;.&lt;stat&gt;）→ 劣化方向的解析表。collector 段已知时给出方向语义，
    /// 未知 collector 一律 Symmetric（保守：双向都按潜在劣化处理）。新增采集器时在此登记。
    /// </summary>
    public static class MetricDirection
    {
        /// <summary>解析指标键的劣化敏感方向（键形如 fps.avg / frameTime.cpuP95 / custom.scope.x.p95）。</summary>
        public static MetricSensitivity Of(string metricKey)
        {
            if (string.IsNullOrEmpty(metricKey)) return MetricSensitivity.Symmetric;

            var dot = metricKey.IndexOf('.');
            var collector = dot > 0 ? metricKey.Substring(0, dot) : metricKey;
            var stat = dot > 0 ? metricKey.Substring(dot + 1) : string.Empty;

            switch (collector)
            {
                case "fps":
                    return MetricSensitivity.HigherIsBetter;
                case "frameTime":
                case "memory":
                    return MetricSensitivity.LowerIsBetter;
                case "gc":
                    return stat.StartsWith("allocPerFrame", System.StringComparison.Ordinal)
                        ? MetricSensitivity.LowerIsBetter   // 每帧分配量，升高=劣化
                        : MetricSensitivity.Symmetric;      // collections 计数升降语义不定
                case "custom":
                    return stat.StartsWith("scope.", System.StringComparison.Ordinal)
                        ? MetricSensitivity.LowerIsBetter   // 耗时 ms
                        : MetricSensitivity.Symmetric;      // counter 累计/gauge 瞬时语义不定
                default:
                    return MetricSensitivity.Symmetric;
            }
        }
    }
}
