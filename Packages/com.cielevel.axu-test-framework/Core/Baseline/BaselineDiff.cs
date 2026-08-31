using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Metrics;

namespace AxuTestFramework.Core.Baseline
{
    /// <summary>
    /// 基线对比（纯 C#）：扁平指标键（&lt;collector&gt;.&lt;stat&gt;）→ 值 的两字典对比。
    /// 分级按 <see cref="MetricDirection.Of"/> 的方向语义：仅"劣化方向"的变化计入分级——
    /// 劣化幅度 ≤ warnRatio → pass；≤ failRatio → warn；否则 fail（端点含于低档）；
    /// 改善方向的变化一律 pass（fps 上升、帧时间下降是优化不是回归）。
    /// Base=0 时比例无定义，退化为绝对差分支（同一阈值作用于劣化方向的 |Current - Base|）。
    /// 仅比较两侧都存在的键：基线独有键忽略；current 独有键不产生条目。
    /// </summary>
    public static class BaselineDiff
    {
        /// <summary>默认 warn 阈值（10%）。</summary>
        public const double DefaultWarnRatio = 0.10;

        /// <summary>默认 fail 阈值（25%）。</summary>
        public const double DefaultFailRatio = 0.25;

        /// <summary>逐指标对比（顺序随基线字典枚举序；任一侧为空返回空列表）。</summary>
        public static List<BaselineDiffEntry> Compare(
            IReadOnlyDictionary<string, double> baseline,
            IReadOnlyDictionary<string, double> current,
            double warnRatio = DefaultWarnRatio,
            double failRatio = DefaultFailRatio)
        {
            var entries = new List<BaselineDiffEntry>();
            if (baseline == null || baseline.Count == 0 || current == null) return entries;

            foreach (var pair in baseline)
            {
                if (string.IsNullOrEmpty(pair.Key) || !current.TryGetValue(pair.Key, out var value)) continue;

                var diff = value - pair.Value;
                var direction = MetricDirection.Of(pair.Key);

                double ratio;
                double degradation; // 劣化方向的变化幅度（改善为 0 或负 → 判 pass）
                if (pair.Value == 0d)
                {
                    // Base=0：比例无定义，退化为绝对差分支（阈值作用于劣化方向的绝对差）
                    ratio = 0d;
                    degradation = direction == MetricSensitivity.LowerIsBetter ? diff
                        : direction == MetricSensitivity.HigherIsBetter ? -diff
                        : Math.Abs(diff);
                }
                else
                {
                    ratio = diff / pair.Value;
                    degradation = direction == MetricSensitivity.LowerIsBetter ? ratio
                        : direction == MetricSensitivity.HigherIsBetter ? -ratio
                        : Math.Abs(ratio);
                }

                entries.Add(new BaselineDiffEntry
                {
                    Metric = pair.Key,
                    Base = pair.Value,
                    Current = value,
                    DeltaRatio = ratio,
                    Verdict = degradation <= warnRatio ? "pass"
                        : degradation <= failRatio ? "warn"
                        : "fail",
                });
            }
            return entries;
        }

        /// <summary>
        /// 把报告 metrics（collector → stat → 值 的嵌套结构）扁平化为两段键字典
        /// （BaselineFile/BaselineDiff 使用扁平键；custom 通道 stat 名本身含点，扁平化后原样保留）。
        /// </summary>
        public static Dictionary<string, double> Flatten(IReadOnlyDictionary<string, Dictionary<string, double>> metrics)
        {
            var flat = new Dictionary<string, double>();
            if (metrics == null) return flat;

            foreach (var collectorPair in metrics)
            {
                if (collectorPair.Value == null) continue;
                foreach (var statPair in collectorPair.Value)
                {
                    flat[$"{collectorPair.Key}.{statPair.Key}"] = statPair.Value;
                }
            }
            return flat;
        }
    }
}
