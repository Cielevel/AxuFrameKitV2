using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Metrics;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Core.Budget
{
    /// <summary>
    /// 预算判定（纯 C#，Core 单测覆盖）：规则键按 &lt;collector&gt;.&lt;stat&gt; 两段式解析后与运行产出 metrics 对比。
    /// 键解析先按最后一个 '.' 分两段；collector 未命中时回退按第一个 '.' 分段
    /// （兼容 custom 通道的含点 stat 名，如 custom.scope.AI.Tick.p95 → custom / scope.AI.Tick.p95）。
    /// collector 或 stat 缺失 → Missing=true 且 Verdict="fail"，绝不静默 0 值。
    /// </summary>
    public static class BudgetEvaluator
    {
        /// <summary>== 判定的容差（避免浮点直等误判）。</summary>
        public const double EqualTolerance = 1e-9;

        /// <summary>逐条判定（输出顺序与规则一致；null 规则跳过；rules 为 null 返回空列表）。</summary>
        public static List<BudgetOutcome> Evaluate(
            IReadOnlyDictionary<string, Dictionary<string, double>> metrics,
            IEnumerable<BudgetRule> rules)
        {
            var outcomes = new List<BudgetOutcome>();
            if (rules == null) return outcomes;

            foreach (var rule in rules)
            {
                if (rule == null) continue;
                outcomes.Add(EvaluateRule(metrics, rule));
            }
            return outcomes;
        }

        static BudgetOutcome EvaluateRule(
            IReadOnlyDictionary<string, Dictionary<string, double>> metrics, BudgetRule rule)
        {
            var outcome = new BudgetOutcome
            {
                Metric = rule.Metric,
                Op = rule.Op,
                Threshold = rule.Value,
                Unit = rule.Unit,
                Verdict = "fail", // 缺失/非法一律 fail（绝不静默 0 值）
                Missing = true,
            };

            if (!TryResolveActual(metrics, rule.Metric, out var actual)) return outcome;
            if (!BudgetOpParser.TryParse(rule.Op, out var op)) return outcome;

            outcome.Missing = false;
            outcome.Actual = actual;
            outcome.Verdict = IsViolated(rule, op, actual)
                ? (rule.Severity == BudgetSeverity.Warn ? "warn" : "fail")
                : "pass";
            return outcome;
        }

        /// <summary>先按最后一个 '.' 分段查 metrics；未命中回退第一个 '.'（custom 等含点 stat 名）。</summary>
        static bool TryResolveActual(
            IReadOnlyDictionary<string, Dictionary<string, double>> metrics, string metricKey, out double actual)
        {
            actual = 0;
            if (metrics == null || metrics.Count == 0 || string.IsNullOrEmpty(metricKey)) return false;

            if (MetricKey.TrySplit(metricKey, out var collector, out var stat)
                && TryLookup(metrics, collector, stat, out actual))
            {
                return true;
            }

            var firstDot = metricKey.IndexOf('.');
            if (firstDot > 0 && firstDot < metricKey.Length - 1
                && TryLookup(metrics, metricKey.Substring(0, firstDot), metricKey.Substring(firstDot + 1), out actual))
            {
                return true;
            }

            return false;
        }

        static bool TryLookup(
            IReadOnlyDictionary<string, Dictionary<string, double>> metrics,
            string collector, string stat, out double actual)
        {
            actual = 0;
            return metrics.TryGetValue(collector, out var stats)
                && stats != null
                && stats.TryGetValue(stat, out actual);
        }

        static bool IsViolated(BudgetRule rule, BudgetOp op, double actual)
        {
            switch (op)
            {
                case BudgetOp.GreaterOrEqual:
                    return rule.Value == null || actual < rule.Value.Value;
                case BudgetOp.LessOrEqual:
                    return rule.Value == null || actual > rule.Value.Value;
                case BudgetOp.Equal:
                    return rule.Value == null || Math.Abs(actual - rule.Value.Value) > EqualTolerance;
                case BudgetOp.Range:
                    // 区间残缺（Validate 应已拦截）视为违约，宁可误报不静默放行
                    if (rule.ValueMin == null || rule.ValueMax == null) return true;
                    return actual < rule.ValueMin.Value || actual > rule.ValueMax.Value;
                default:
                    return true;
            }
        }
    }
}
