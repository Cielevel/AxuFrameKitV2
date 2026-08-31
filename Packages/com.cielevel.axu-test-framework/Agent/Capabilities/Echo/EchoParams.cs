using Newtonsoft.Json;

namespace AxuTestFramework.Agent.Capabilities.Echo
{
    /// <summary>Echo 能力参数（scenario.params）。</summary>
    public class EchoParams
    {
        /// <summary>要打印的消息（开始打一条、结束追加 " done"）。</summary>
        [JsonProperty("message")] public string Message = "echo";

        /// <summary>总等待秒数（默认 1.0，clamp 0..600，分段真实时间等待）。</summary>
        [JsonProperty("delaySeconds")] public float DelaySeconds = 1.0f;

        /// <summary>分段数（默认 3，clamp 1..1000；总时长仍为 DelaySeconds）。</summary>
        [JsonProperty("steps")] public int Steps = 3;

        /// <summary>clamp 后的单次运行等待总时长。</summary>
        public float ClampedDelaySeconds => UnityEngine.Mathf.Clamp(DelaySeconds, 0f, 600f);

        /// <summary>clamp 后的分段数。</summary>
        public int ClampedSteps => UnityEngine.Mathf.Clamp(Steps, 1, 1000);
    }
}
