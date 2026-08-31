using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>结构化结论/问题记录（schema results[].findings[] 条目，见 DOC/ScenarioSchema.md §3）。</summary>
    public class Finding
    {
        /// <summary>严重级别枚举本体（序列化经 SeverityText 映射为 "info"/"warn"/"error"）。</summary>
        [JsonIgnore] public FindingSeverity Severity;

        /// <summary>schema 的 severity 字符串（与 DOC 一致：info / warn / error）。</summary>
        [JsonProperty("severity")]
        public string SeverityText
        {
            get
            {
                switch (Severity)
                {
                    case FindingSeverity.Warning: return "warn";
                    case FindingSeverity.Error: return "error";
                    default: return "info";
                }
            }
            set
            {
                switch ((value ?? string.Empty).Trim().ToLowerInvariant())
                {
                    case "warn": Severity = FindingSeverity.Warning; break;
                    case "error": Severity = FindingSeverity.Error; break;
                    default: Severity = FindingSeverity.Info; break;
                }
            }
        }

        /// <summary>机器可读代码（如 CAPABILITY_MISSING / RUNTIME_TIMEOUT / BUDGET_WARN）。</summary>
        [JsonProperty("code")] public string Code;

        /// <summary>人类可读描述。</summary>
        [JsonProperty("message")] public string Message;

        public Finding() { }

        public Finding(FindingSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }
    }
}
