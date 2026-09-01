using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.LoadTime
{
    /// <summary>LoadTime 能力参数（scenario.params）。</summary>
    public class LoadTimeParams
    {
        /// <summary>目标场景清单（逐项加载计时；空清单 = LOADTIME_NO_SCENES 错误）。</summary>
        [JsonProperty("scenes")] public List<string> Scenes = new List<string>();

        /// <summary>稳定判定目标帧率（每帧 unscaledDeltaTime ≤ 1/该值；默认 30，clamp 1..1000）。</summary>
        [JsonProperty("targetFps")] public float TargetFps = 30f;

        /// <summary>稳定判定连续帧数（默认 30，clamp 1..10000）。</summary>
        [JsonProperty("stableFrames")] public int StableFrames = 30;

        /// <summary>稳定判定超时秒数（超时未稳定 → SCENE_NOT_STABLE 警告；默认 30，clamp 1..600）。</summary>
        [JsonProperty("stabilityTimeoutSeconds")] public float StabilityTimeoutSeconds = 30f;

        /// <summary>clamp 后的目标帧率。</summary>
        public float ClampedTargetFps => Mathf.Clamp(TargetFps, 1f, 1000f);

        /// <summary>clamp 后的稳定判定连续帧数。</summary>
        public int ClampedStableFrames => Mathf.Clamp(StableFrames, 1, 10000);

        /// <summary>clamp 后的稳定判定超时秒数。</summary>
        public float ClampedStabilityTimeoutSeconds => Mathf.Clamp(StabilityTimeoutSeconds, 1f, 600f);
    }
}
