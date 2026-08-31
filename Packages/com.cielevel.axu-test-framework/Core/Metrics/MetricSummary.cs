namespace AxuTestFramework.Core.Metrics
{
    /// <summary>采样序列的统计摘要（空序列 Count=0 且各统计为 0）。</summary>
    public class MetricSummary
    {
        /// <summary>样本数。</summary>
        public int Count;

        /// <summary>算术平均。</summary>
        public double Avg;

        /// <summary>第 50 百分位（线性插值）。</summary>
        public double P50;

        /// <summary>第 95 百分位（线性插值）。</summary>
        public double P95;

        /// <summary>最小值。</summary>
        public double Min;

        /// <summary>最大值。</summary>
        public double Max;
    }
}
