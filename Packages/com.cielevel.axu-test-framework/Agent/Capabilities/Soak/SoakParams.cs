using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Soak
{
    /// <summary>Soak 能力参数（scenario.params，方案 §4.5）。</summary>
    public class SoakParams
    {
        /// <summary>浸泡时长（分钟；默认 60，clamp 0.1..1440，支持整夜长跑）。</summary>
        [JsonProperty("durationMinutes")] public float DurationMinutes = 60f;

        /// <summary>采样间隔（秒；默认 30，clamp 1..600）。</summary>
        [JsonProperty("sampleIntervalSec")] public float SampleIntervalSec = 30f;

        /// <summary>每次采样前是否强制 GC.Collect（默认 true：把波动从趋势里剥离，斜率更接近真实泄漏）。</summary>
        [JsonProperty("gcCollectBeforeSample")] public bool GcCollectBeforeSample = true;

        /// <summary>每 N 分钟自动截图留证（默认 0 = 不截；需 context.ArtifactsDir 非空）。</summary>
        [JsonProperty("screenshotEveryMinutes")] public float ScreenshotEveryMinutes = 0f;

        /// <summary>泄漏判定阈值（MB/min）：memory.allocated 滑窗回归斜率超过即出 warn finding（默认 5）。</summary>
        [JsonProperty("leakSlopeMBPerMin")] public double LeakSlopeMbPerMin = 5.0;

        /// <summary>FPS 衰减判定阈值（%）：后半程 p50 比前半程低超过该百分比即出 warn finding（默认 15）。</summary>
        [JsonProperty("fpsDecayPercent")] public float FpsDecayPercent = 15f;

        /// <summary>partial report 刷新间隔（分钟；默认 5，clamp 0.5..duration）。0 以下按默认处理。</summary>
        [JsonProperty("flushEveryMinutes")] public float FlushEveryMinutes = 5f;

        /// <summary>clamp 后的浸泡时长（分钟）。</summary>
        public float ClampedDurationMinutes => Mathf.Clamp(DurationMinutes, 0.1f, 1440f);

        /// <summary>clamp 后的采样间隔（秒）。</summary>
        public float ClampedSampleIntervalSec => Mathf.Clamp(SampleIntervalSec, 1f, 600f);

        /// <summary>clamp 后的截图间隔（分钟；≤0 = 不截）。</summary>
        public float ClampedScreenshotEveryMinutes => ScreenshotEveryMinutes <= 0f
            ? 0f
            : Mathf.Clamp(ScreenshotEveryMinutes, 0.1f, ClampedDurationMinutes);

        /// <summary>clamp 后的 partial flush 间隔（分钟；≤0 回落默认 5）。</summary>
        public float ClampedFlushEveryMinutes => FlushEveryMinutes <= 0f
            ? 5f
            : Mathf.Clamp(FlushEveryMinutes, 0.5f, Mathf.Max(0.5f, ClampedDurationMinutes));
    }
}
