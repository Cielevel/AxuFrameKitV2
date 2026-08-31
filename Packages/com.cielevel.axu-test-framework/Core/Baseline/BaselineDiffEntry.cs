using Newtonsoft.Json;

namespace AxuTestFramework.Core.Baseline
{
    /// <summary>单个指标的基线对比条目（schema results[].baselineDiff[] 条目，见 DOC/ScenarioSchema.md §3/§5）。</summary>
    public class BaselineDiffEntry
    {
        /// <summary>指标键（&lt;collector&gt;.&lt;stat&gt;）。</summary>
        [JsonProperty("metric")] public string Metric;

        /// <summary>基线值。</summary>
        [JsonProperty("base")] public double Base;

        /// <summary>本次运行值。</summary>
        [JsonProperty("current")] public double Current;

        /// <summary>
        /// 相对变化率 = (Current - Base) / Base（正=升、负=降）；
        /// Base=0 的退化分支下无比例语义，恒为 0（该分支按绝对差判定）。
        /// </summary>
        [JsonProperty("deltaRatio")] public double DeltaRatio;

        /// <summary>判定串："pass" / "warn" / "fail"（双侧对称口径，见 BaselineDiff）。</summary>
        [JsonProperty("verdict")] public string Verdict;
    }
}
