using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Perf
{
    /// <summary>Perf 能力参数（scenario.params）。</summary>
    public class PerfParams
    {
        /// <summary>目标场景路径（空 = 不加载、对当前场景采样）。</summary>
        [JsonProperty("scene")] public string Scene = "";

        /// <summary>warmup 秒数（丢弃采样；默认 0，clamp 0..3600）。</summary>
        [JsonProperty("warmupSeconds")] public float WarmupSeconds = 0f;

        /// <summary>度量期秒数（默认 30，clamp 0.1..3600）。</summary>
        [JsonProperty("measureSeconds")] public float MeasureSeconds = 30f;

        /// <summary>结束时是否截图（默认 false；需 context.ArtifactsDir 非空）。</summary>
        [JsonProperty("screenshot")] public bool Screenshot = false;

        /// <summary>clamp 后的 warmup 秒数。</summary>
        public float ClampedWarmupSeconds => Mathf.Clamp(WarmupSeconds, 0f, 3600f);

        /// <summary>clamp 后的度量期秒数。</summary>
        public float ClampedMeasureSeconds => Mathf.Clamp(MeasureSeconds, 0.1f, 3600f);
    }
}
