namespace AxuTestFramework.Core.Metrics
{
    /// <summary>单个度量采样点（值 + 采样时刻，秒）。</summary>
    public sealed class MetricSample
    {
        public MetricSample(double value, double timeSeconds)
        {
            Value = value;
            TimeSeconds = timeSeconds;
        }

        /// <summary>采样值。</summary>
        public double Value { get; }

        /// <summary>采样时刻（秒；单调真实时钟口径由写入方保证）。</summary>
        public double TimeSeconds { get; }
    }
}
