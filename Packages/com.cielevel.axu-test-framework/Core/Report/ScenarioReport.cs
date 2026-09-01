using System.Collections.Generic;
using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>一次 suite 运行的结构化报告（schema axu-scenario-report/1，见 DOC/ScenarioSchema.md §3）。</summary>
    public class ScenarioReport
    {
        /// <summary>自描述 schema，默认 axu-scenario-report/1。</summary>
        [JsonProperty("schema")] public string Schema = SchemaNames.ScenarioReport;

        /// <summary>所属 suite id。</summary>
        [JsonProperty("suiteId")] public string SuiteId;

        /// <summary>开始时间（UTC ISO 8601）。</summary>
        [JsonProperty("startedAt")] public string StartedAt;

        /// <summary>结束时间（UTC ISO 8601）。</summary>
        [JsonProperty("endedAt")] public string EndedAt;

        /// <summary>运行环境（Player 侧尤其重要）。</summary>
        [JsonProperty("environment")] public ScenarioEnvironment Environment;

        /// <summary>结果汇总。</summary>
        [JsonProperty("summary")] public ReportSummary Summary;

        /// <summary>进程退出码（0/1/2/3，序列化为数字）。</summary>
        [JsonProperty("exitCode")] public ScenarioExitCode ExitCode;

        /// <summary>逐 scenario 结果。</summary>
        [JsonProperty("results")] public List<ScenarioReportResult> Results = new List<ScenarioReportResult>();

        /// <summary>
        /// 是否为部分报告（soak 等长跑能力定期 flush 的中间快照）：true = 运行尚未结束、数据可能不完整；
        /// null（默认，序列化省略）= 完整报告。
        /// </summary>
        [JsonProperty("truncated", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Truncated;
    }
}
