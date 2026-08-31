using System;
using System.Collections.Generic;

namespace AxuTestFramework.Core.Metrics
{
    /// <summary>
    /// 纯统计工具（零引擎依赖）：汇总统计（百分位 = 线性插值法，rank=(n-1)*p，含端点插值）
    /// 与值对时间的最小二乘斜率。
    /// </summary>
    public static class MetricStats
    {
        /// <summary>汇总统计；空序列返回 Count=0 且各统计为 0（不抛异常）。</summary>
        public static MetricSummary Summarize(IReadOnlyList<double> values)
        {
            var summary = new MetricSummary();
            if (values == null || values.Count == 0) return summary;

            double sum = 0;
            var sorted = new List<double>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                sum += values[i];
                sorted.Add(values[i]);
            }
            sorted.Sort();

            summary.Count = sorted.Count;
            summary.Avg = sum / sorted.Count;
            summary.Min = sorted[0];
            summary.Max = sorted[sorted.Count - 1];
            summary.P50 = Percentile(sorted, 0.50);
            summary.P95 = Percentile(sorted, 0.95);
            return summary;
        }

        /// <summary>
        /// 值对时间的最小二乘斜率（每秒）；样本少于 2、两表长度不一致取交集后仍不足、或时间无方差时返回 0，不抛异常。
        /// </summary>
        public static double SlopePerSecond(IReadOnlyList<double> values, IReadOnlyList<double> timestampsSeconds)
        {
            if (values == null || timestampsSeconds == null) return 0;

            var n = Math.Min(values.Count, timestampsSeconds.Count);
            if (n < 2) return 0;

            double sumT = 0, sumV = 0;
            for (var i = 0; i < n; i++)
            {
                sumT += timestampsSeconds[i];
                sumV += values[i];
            }
            var meanT = sumT / n;
            var meanV = sumV / n;

            double covariance = 0, variance = 0;
            for (var i = 0; i < n; i++)
            {
                var dt = timestampsSeconds[i] - meanT;
                covariance += dt * (values[i] - meanV);
                variance += dt * dt;
            }
            return variance <= 0 ? 0 : covariance / variance;
        }

        /// <summary>线性插值百分位（rank=(n-1)*p，小数权重落在相邻两样本之间）；sorted 须升序且非空。</summary>
        static double Percentile(List<double> sorted, double p)
        {
            var count = sorted.Count;
            if (count == 1) return sorted[0];

            var rank = (count - 1) * p;
            var lower = (int)Math.Floor(rank);
            var upper = (int)Math.Ceiling(rank);
            if (lower == upper) return sorted[lower];

            var fraction = rank - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }
    }
}
