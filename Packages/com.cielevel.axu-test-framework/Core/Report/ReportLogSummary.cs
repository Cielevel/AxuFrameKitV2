using System.Collections.Generic;
using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>单条 scenario 的日志摘要（schema results[].log 节）。</summary>
    public class ReportLogSummary
    {
        [JsonProperty("errorCount")] public int ErrorCount;

        [JsonProperty("warningCount")] public int WarningCount;

        /// <summary>代表性日志样本（截断防膨胀）。</summary>
        [JsonProperty("samples")] public List<string> Samples = new List<string>();
    }
}
