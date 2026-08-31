using System.Collections.Generic;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Budget;
using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>单条 scenario 的结果摘要（schema results[] 条目，见 DOC/ScenarioSchema.md §3）。</summary>
    public class ScenarioReportResult
    {
        /// <summary>扇出后的 scenario id（含 @profile 后缀）。</summary>
        [JsonProperty("scenarioId")] public string ScenarioId;

        /// <summary>能力 id。</summary>
        [JsonProperty("capability")] public string Capability;

        /// <summary>结果串：pass / warn / fail / error。</summary>
        [JsonProperty("status")] public string Status;

        /// <summary>耗时毫秒（真实时间）。</summary>
        [JsonProperty("durationMs")] public double DurationMs;

        /// <summary>无预算标记（Budgets 为空时 true，防"没人写阈值所以永远绿"）。</summary>
        [JsonProperty("unbudgeted")] public bool Unbudgeted;

        /// <summary>指标聚合（collector → {stat → 值}）；无度量产出时为 null。</summary>
        [JsonProperty("metrics")] public Dictionary<string, Dictionary<string, double>> Metrics;

        /// <summary>预算判定明细（BudgetEvaluator 产出；无 budgets 或未产出时为 null）。</summary>
        [JsonProperty("budgets")] public List<BudgetOutcome> Budgets;

        /// <summary>基线对比条目（有 baseline 且对比成功时非空；字段见 DOC §5）。</summary>
        [JsonProperty("baselineDiff")] public List<BaselineDiffEntry> BaselineDiff;

        /// <summary>序列数据引用（后续阶段产出），Phase 1 置 null。</summary>
        [JsonProperty("series")] public object Series;

        /// <summary>工件相对路径（截图等）。</summary>
        [JsonProperty("artifacts")] public List<string> Artifacts = new List<string>();

        /// <summary>日志摘要（error/warning 计数与样本）。</summary>
        [JsonProperty("log")] public ReportLogSummary Log = new ReportLogSummary();

        /// <summary>结构化结论。</summary>
        [JsonProperty("findings")] public List<Finding> Findings = new List<Finding>();
    }
}
