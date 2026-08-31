using System.Collections.Generic;
using AxuTestFramework.Core.Metrics;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] MetricStats/MetricSummary 纯统计语义（Phase 2 契约驱动测试）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - MetricStats.Summarize(IReadOnlyList&lt;double&gt;) → MetricSummary{Count, Avg, P50, P95, Min, Max}；
    ///  - 百分位插值语义 = 线性插值法（rank=(n-1)*p，numpy 默认/PERCENTILE.INC）：偶数个元素 p50 取中间两值均值，
    ///    p95 落在两样本之间时按小数权重线性插值；
    ///  - MetricStats.SlopePerSecond(values, timestampsSeconds) = 值对时间的最小二乘斜率；
    ///    样本数 &lt; 2 返回 0；空/单样本不抛异常。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class MetricStatsTests
    {
        // ------------------------------------------------------------------ Summarize

        [Test]
        public void Summarize_序列1到100_各统计值精确()
        {
            var samples = new List<double>();
            for (var i = 1; i <= 100; i++) samples.Add(i);

            var summary = MetricStats.Summarize(samples);

            Assert.That(summary.Count, Is.EqualTo(100));
            Assert.That(summary.Avg, Is.EqualTo(50.5).Within(1e-9));
            Assert.That(summary.P50, Is.EqualTo(50.5).Within(1e-9));
            Assert.That(summary.P95, Is.EqualTo(95.05).Within(1e-9)); // rank=99*0.95=94.05 → 95 与 96 之间插值
            Assert.That(summary.Min, Is.EqualTo(1d).Within(1e-9));
            Assert.That(summary.Max, Is.EqualTo(100d).Within(1e-9));
        }

        [Test]
        public void Summarize_空序列_Count为0且各统计为0()
        {
            var summary = MetricStats.Summarize(new List<double>());

            Assert.That(summary.Count, Is.EqualTo(0));
            Assert.That(summary.Avg, Is.EqualTo(0d));
            Assert.That(summary.P50, Is.EqualTo(0d));
            Assert.That(summary.P95, Is.EqualTo(0d));
            Assert.That(summary.Min, Is.EqualTo(0d));
            Assert.That(summary.Max, Is.EqualTo(0d));
        }

        [Test]
        public void Summarize_单元素序列_各统计等于该元素()
        {
            var summary = MetricStats.Summarize(new List<double> { 7.5 });

            Assert.That(summary.Count, Is.EqualTo(1));
            Assert.That(summary.Avg, Is.EqualTo(7.5).Within(1e-9));
            Assert.That(summary.P50, Is.EqualTo(7.5).Within(1e-9));
            Assert.That(summary.P95, Is.EqualTo(7.5).Within(1e-9));
            Assert.That(summary.Min, Is.EqualTo(7.5).Within(1e-9));
            Assert.That(summary.Max, Is.EqualTo(7.5).Within(1e-9));
        }

        [Test]
        public void Summarize_偶数个元素_p50取中间两值均值_p95线性插值()
        {
            // n=10：p50 rank=4.5 → (4+5)/2=4.5；p95 rank=8.55 → 8 与 9 之间按 0.55 插值
            var samples = new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var summary = MetricStats.Summarize(samples);

            Assert.That(summary.P50, Is.EqualTo(4.5).Within(1e-9));
            Assert.That(summary.P95, Is.EqualTo(8.55).Within(1e-9));
        }

        [Test]
        public void Summarize_奇数个元素_p50为正中元素()
        {
            var summary = MetricStats.Summarize(new List<double> { 10, 20, 30 });

            Assert.That(summary.P50, Is.EqualTo(20d).Within(1e-9));
        }

        // ------------------------------------------------------------------ SlopePerSecond

        [Test]
        public void SlopePerSecond_完全线性序列_斜率精确()
        {
            var timestamps = new List<double> { 0, 1, 2, 3, 4 };
            var values = new List<double> { 0, 1, 2, 3, 4 }; // y = t

            Assert.That(MetricStats.SlopePerSecond(values, timestamps), Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void SlopePerSecond_常数序列_斜率为0()
        {
            var timestamps = new List<double> { 0, 1, 2, 3 };
            var values = new List<double> { 5, 5, 5, 5 };

            Assert.That(MetricStats.SlopePerSecond(values, timestamps), Is.EqualTo(0d).Within(1e-9));
        }

        [Test]
        public void SlopePerSecond_样本少于2_返回0且不抛异常()
        {
            var timestamps = new List<double> { 0 };
            var values = new List<double> { 42 };

            Assert.That(MetricStats.SlopePerSecond(values, timestamps), Is.EqualTo(0d));
            Assert.That(MetricStats.SlopePerSecond(new List<double>(), new List<double>()), Is.EqualTo(0d));
        }

        [Test]
        public void SlopePerSecond_带噪声线性序列_斜率在容差内()
        {
            // 固定噪声序列（非随机，可复现）：真斜率 2，最小二乘解 1.91
            var timestamps = new List<double> { 0, 1, 2, 3, 4 };
            var values = new List<double> { 0.1, 2.2, 3.8, 6.3, 7.6 };

            Assert.That(MetricStats.SlopePerSecond(values, timestamps), Is.EqualTo(2.0).Within(0.1));
        }
    }
}
