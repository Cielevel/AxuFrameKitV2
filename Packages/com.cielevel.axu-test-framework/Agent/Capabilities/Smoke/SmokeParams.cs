using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Smoke
{
    /// <summary>Smoke 能力参数（scenario.params）。</summary>
    public class SmokeParams
    {
        /// <summary>场景清单来源："list" / "buildsettings" / "directory"（未知值归 list）。</summary>
        [JsonProperty("source")] public string Source = "list";

        /// <summary>场景白名单（source=list 时生效）。</summary>
        [JsonProperty("scenes")] public List<string> Scenes = new List<string>();

        /// <summary>扫描目录（source=directory 时生效；仅 Editor，转为 Assets/... 相对路径）。</summary>
        [JsonProperty("directory")] public string DirectoryPath = "";

        /// <summary>稳定窗时长秒数（此窗口内的错误计入 errorCount；默认 1，clamp 0..60）。</summary>
        [JsonProperty("stabilitySeconds")] public float StabilitySeconds = 1f;

        /// <summary>稳定窗前置帧数（默认 10，clamp 1..1000）。</summary>
        [JsonProperty("stabilityFrames")] public int StabilityFrames = 10;

        /// <summary>是否逐场景截图（默认 true；需 context.ArtifactsDir 非空）。</summary>
        [JsonProperty("screenshot")] public bool Screenshot = true;

        /// <summary>归一化来源：trim + 小写；未知值归 "list"。</summary>
        public string NormalizedSource
        {
            get
            {
                var normalized = (Source ?? string.Empty).Trim().ToLowerInvariant();
                return normalized == "list" || normalized == "buildsettings" || normalized == "directory"
                    ? normalized
                    : "list";
            }
        }

        /// <summary>clamp 后的稳定窗时长秒数。</summary>
        public float ClampedStabilitySeconds => Mathf.Clamp(StabilitySeconds, 0f, 60f);

        /// <summary>clamp 后的稳定窗前置帧数。</summary>
        public int ClampedStabilityFrames => Mathf.Clamp(StabilityFrames, 1, 1000);
    }
}
