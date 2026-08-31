using Newtonsoft.Json;

namespace AxuTestFramework.Core.Budget
{
    /// <summary>
    /// 单条预算规则的判定结果（schema results[].budgets[] 条目，见 DOC/ScenarioSchema.md §3）。
    /// Missing=指标缺失（collector 或 stat 未产出）→ Verdict 恒 "fail"，绝不静默 0 值。
    /// </summary>
    public class BudgetOutcome
    {
        /// <summary>指标键（&lt;collector&gt;.&lt;stat&gt;）。</summary>
        [JsonProperty("metric")] public string Metric;

        /// <summary>操作符原串（"&gt;=" / "&lt;=" / "==" / "range"）。</summary>
        [JsonProperty("op")] public string Op;

        /// <summary>阈值（range 规则为 null，区间上下限见规则 ValueMin/ValueMax）。</summary>
        [JsonProperty("threshold")] public double? Threshold;

        /// <summary>实际值（指标缺失时为 null）。</summary>
        [JsonProperty("actual")] public double? Actual;

        /// <summary>展示单位（透传规则配置，不参与换算）。</summary>
        [JsonProperty("unit")] public string Unit;

        /// <summary>判定串："pass" / "warn" / "fail"。</summary>
        [JsonProperty("verdict")] public string Verdict;

        /// <summary>指标缺失标记（true 时 Verdict 恒 "fail"）。</summary>
        [JsonProperty("missing")] public bool Missing;
    }
}
