using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// stress 能力参数（scenario.params）：phases 列表声明要顺序执行的压力工具组合，
    /// 每个 phase 是一个 <see cref="StressPhaseParams"/>（工具名 + 该工具的参数，全部平铺 POCO 化，
    /// 未用到的工具参数被忽略）。见同目录 DOC.md。
    /// </summary>
    public class StressParams
    {
        /// <summary>顺序执行的压力 phase 列表（tool ∈ spawner / capacity / churn / flood）。</summary>
        [JsonProperty("phases")] public List<StressPhaseParams> Phases = new List<StressPhaseParams>();
    }

    /// <summary>
    /// 单个压力 phase 参数：tool 选定工具，其余为各工具参数（平铺；各工具只读自己那组）。
    /// clamp 保证非法值（0/负数/超界）落到安全区间，runner 不因脏参数崩。
    /// </summary>
    public class StressPhaseParams
    {
        // ------------------------------------------------------------ 工具选择

        /// <summary>工具名：spawner / capacity / churn / flood（大小写不敏感）。</summary>
        [JsonProperty("tool")] public string Tool = "spawner";

        /// <summary>规范化工具名（小写；未知值原样返回小写，runner 判未知工具）。</summary>
        public string NormalizedTool => string.IsNullOrEmpty(Tool) ? "" : Tool.Trim().ToLowerInvariant();

        // ------------------------------------------------------------ spawner / capacity 共用（对象源）

        /// <summary>
        /// prefab 的 Resources 相对路径（不含 "Resources/" 前缀与扩展名，如 "StressTarget"）。
        /// 空 = 内置 Primitive Cube 替身（去 Collider），无任何游戏资产也能跑。
        /// Player 侧只能加载随构建打包的 Resources 资产——路径语义见 DOC.md。
        /// </summary>
        [JsonProperty("prefabPath")] public string PrefabPath = "";

        /// <summary>起始实体数（spawner/capacity；默认 100，clamp 1..50000）。</summary>
        [JsonProperty("startCount")] public int StartCount = 100;

        /// <summary>每档增量（spawner/capacity；默认 100，clamp 1..50000）。</summary>
        [JsonProperty("rampStep")] public int RampStep = 100;

        /// <summary>每档加压后的间隔秒（spawner/capacity ramp 档；默认 1，clamp 0..600）。</summary>
        [JsonProperty("rampIntervalSec")] public float RampIntervalSec = 1f;

        /// <summary>实体数上限（spawner 终点 / capacity ramp 上界；默认 10000，clamp 1..100000）。</summary>
        [JsonProperty("maxCount")] public int MaxCount = 10000;

        /// <summary>加压后稳定等待秒（capacity 判定前 / spawner 初始；默认 2，clamp 0..600）。</summary>
        [JsonProperty("settleSec")] public float SettleSec = 2f;

        // ------------------------------------------------------------ capacity（容量探测）

        /// <summary>目标帧率阈值（fps.avg 滑动窗口低于此值持续 belowThresholdSec 判超载；默认 60）。</summary>
        [JsonProperty("targetFps")] public float TargetFps = 60f;

        /// <summary>低于阈值的持续秒数（默认 2，clamp 0.5..60）。</summary>
        [JsonProperty("belowThresholdSec")] public float BelowThresholdSec = 2f;

        /// <summary>二分收敛精度（百分比；默认 5，clamp 1..50）。</summary>
        [JsonProperty("convergeTolerancePercent")] public float ConvergeTolerancePercent = 5f;

        // ------------------------------------------------------------ churn（对象翻搅）

        /// <summary>每秒 Create/Destroy 的对象数（默认 50，clamp 1..10000）。</summary>
        [JsonProperty("createPerSec")] public int CreatePerSec = 50;

        /// <summary>持续秒数（churn/flood 共用；默认 10，clamp 0.1..3600）。</summary>
        [JsonProperty("durationSec")] public float DurationSec = 10f;

        // ------------------------------------------------------------ flood（托管事件洪峰）

        /// <summary>每帧发布事件数（默认 1000，clamp 1..1000000）。</summary>
        [JsonProperty("eventsPerFrame")] public int EventsPerFrame = 1000;

        /// <summary>自建事件中心的订阅者数（默认 8，clamp 1..256）。</summary>
        [JsonProperty("subscribers")] public int Subscribers = 8;

        // ------------------------------------------------------------ clamp 访问器

        /// <summary>clamp 后起始实体数。</summary>
        public int ClampedStartCount => Mathf.Clamp(StartCount, 1, 50000);

        /// <summary>clamp 后每档增量。</summary>
        public int ClampedRampStep => Mathf.Clamp(RampStep, 1, 50000);

        /// <summary>clamp 后档间隔秒。</summary>
        public float ClampedRampIntervalSec => Mathf.Clamp(RampIntervalSec, 0f, 600f);

        /// <summary>clamp 后实体数上限。</summary>
        public int ClampedMaxCount => Mathf.Clamp(MaxCount, 1, 100000);

        /// <summary>clamp 后稳定等待秒。</summary>
        public float ClampedSettleSec => Mathf.Clamp(SettleSec, 0f, 600f);

        /// <summary>clamp 后目标帧率。</summary>
        public float ClampedTargetFps => Mathf.Clamp(TargetFps, 1f, 1000f);

        /// <summary>clamp 后低于阈值持续秒。</summary>
        public float ClampedBelowThresholdSec => Mathf.Clamp(BelowThresholdSec, 0.5f, 60f);

        /// <summary>clamp 后收敛精度百分比。</summary>
        public float ClampedConvergeTolerancePercent => Mathf.Clamp(ConvergeTolerancePercent, 1f, 50f);

        /// <summary>clamp 后每秒创建数。</summary>
        public int ClampedCreatePerSec => Mathf.Clamp(CreatePerSec, 1, 10000);

        /// <summary>clamp 后持续秒。</summary>
        public float ClampedDurationSec => Mathf.Clamp(DurationSec, 0.1f, 3600f);

        /// <summary>clamp 后每帧事件数。</summary>
        public int ClampedEventsPerFrame => Mathf.Clamp(EventsPerFrame, 1, 1000000);

        /// <summary>clamp 后订阅者数。</summary>
        public int ClampedSubscribers => Mathf.Clamp(Subscribers, 1, 256);
    }
}
