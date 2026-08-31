using Newtonsoft.Json;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>suite 级运行设置（schema settings 节，见 DOC/ScenarioSchema.md §2）。</summary>
    public class SuiteSettings
    {
        /// <summary>某 scenario fail 后是否中断剩余，默认 false。</summary>
        [JsonProperty("stopOnFailure")] public bool StopOnFailure = false;

        /// <summary>warn 计入 fail（影响退出码），默认 false。</summary>
        [JsonProperty("warnAsFail")] public bool WarnAsFail = false;

        /// <summary>单 scenario 看门狗超时秒数；null 时宿主用 600。</summary>
        [JsonProperty("scenarioTimeoutSec")] public double? ScenarioTimeoutSec = null;
    }
}
