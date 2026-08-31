using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>运行结果汇总（schema summary 节）；scenario 执行 error 计入 failed。</summary>
    public class ReportSummary
    {
        [JsonProperty("total")] public int Total;

        [JsonProperty("passed")] public int Passed;

        [JsonProperty("warned")] public int Warned;

        [JsonProperty("failed")] public int Failed;
    }
}
