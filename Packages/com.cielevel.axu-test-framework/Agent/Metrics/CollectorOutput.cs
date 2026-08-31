using System.Collections.Generic;

namespace AxuTestFramework.Agent.Metrics
{
    /// <summary>聚合器种类（BuildSummary 按声明聚合序列，避免反射）。</summary>
    public enum MetricAggregator
    {
        /// <summary>算术平均。</summary>
        Avg,

        /// <summary>第 50 百分位。</summary>
        P50,

        /// <summary>第 95 百分位。</summary>
        P95,

        /// <summary>最小值。</summary>
        Min,

        /// <summary>最大值。</summary>
        Max,

        /// <summary>求和（Counter 类序列）。</summary>
        Sum,

        /// <summary>最后一个采样（全程口径单点值）。</summary>
        Last,
    }

    /// <summary>单个输出 stat 的声明：输出名 + 聚合器 + 源序列名（Source 为 null 时与输出同名）。</summary>
    public struct CollectorStatOutput
    {
        /// <summary>输出 stat 名（报告 metrics 内层键；预算键 &lt;collector&gt;.&lt;stat&gt; 的 stat 段）。</summary>
        public string Name;

        /// <summary>源序列名（采集器 Record 时使用的 stat 名；null=与 Name 同名）。</summary>
        public string Source;

        /// <summary>聚合器。</summary>
        public MetricAggregator Aggregator;

        public CollectorStatOutput(string name, MetricAggregator aggregator, string source = null)
        {
            Name = name;
            Aggregator = aggregator;
            Source = source;
        }
    }

    /// <summary>
    /// 采集器输出声明（小型声明式结构）：该采集器输出的 stat 固定清单 + 每 stat 的聚合方式，
    /// MetricSession.BuildSummary 据此从采样序列聚合报告 metrics（不反射、无每帧分配）。
    /// </summary>
    public class CollectorOutput
    {
        /// <summary>该采集器输出的 stat 固定清单（顺序即报告内层键顺序）。</summary>
        public IReadOnlyList<CollectorStatOutput> Stats { get; }

        public CollectorOutput(params CollectorStatOutput[] stats)
        {
            Stats = stats;
        }
    }
}
