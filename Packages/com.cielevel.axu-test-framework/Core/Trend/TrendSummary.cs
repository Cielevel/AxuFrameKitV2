using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Metrics;

namespace AxuTestFramework.Core.Trend
{
    /// <summary>趋势查询/比对（纯 C#）：序列过滤与"正值恒表示变差"的有向百分比变化。</summary>
    public static class TrendSummary
    {
        /// <summary>
        /// 取指定 scenario + metric 的序列（TimestampUtc 升序，字符串序即 ISO 8601 UTC 时序）。
        /// metric 传空串取 run 级行（Value 为 null）。
        /// </summary>
        public static List<TrendPoint> Series(IEnumerable<TrendPoint> points, string scenario, string metric)
        {
            var series = new List<TrendPoint>();
            if (points == null) return series;
            metric = metric ?? string.Empty;

            foreach (var point in points)
            {
                if (point == null || point.Scenario != scenario || point.Metric != metric) continue;
                series.Add(point);
            }
            series.Sort((a, b) => string.CompareOrdinal(a.TimestampUtc, b.TimestampUtc));
            return series;
        }

        /// <summary>
        /// 有向百分比变化：**正值恒表示"变差"**（方向语义取 <see cref="MetricDirection.Of"/>）。
        /// HigherIsBetter（如 fps）：(old-new)/old*100，下降为正；
        /// LowerIsBetter（如帧时间/内存）：(new-old)/old*100，上升为正；
        /// Symmetric：带符号 (new-old)/old*100。
        /// old 为 0 时比例无定义，返回 0。
        /// </summary>
        public static double ChangePercent(TrendPoint older, TrendPoint newer)
        {
            if (older == null || newer == null) throw new ArgumentNullException(older == null ? nameof(older) : nameof(newer));
            if (!older.Value.HasValue || !newer.Value.HasValue)
            {
                throw new ArgumentException("ChangePercent 仅用于 metric 级行（Value 非 null）");
            }
            var old = older.Value.Value;
            var newValue = newer.Value.Value;
            if (old == 0d) return 0d;

            switch (MetricDirection.Of(older.Metric))
            {
                case MetricSensitivity.HigherIsBetter:
                    return (old - newValue) / old * 100d;
                case MetricSensitivity.LowerIsBetter:
                    return (newValue - old) / old * 100d;
                default:
                    return (newValue - old) / old * 100d;
            }
        }
    }
}
