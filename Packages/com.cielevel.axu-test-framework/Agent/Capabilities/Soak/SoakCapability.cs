using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Soak
{
    /// <summary>
    /// Soak 能力（浸泡/泄漏，方案 §4.5）：长时运行目标场景，周期采样内存/帧率/错误并定期截图；
    /// 泄漏判定（allocated 滑窗线性回归斜率）、FPS 衰减判定（后半程 p50 对比）；partial report 定期 flush
    /// 保证崩溃不丢前段数据。指标建议搭配 memory/fps/gc 采集器（宿主按 params.collectors 解析）。
    /// </summary>
    public class SoakCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "soak"）。</summary>
        public const string CapabilityId = "soak";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "浸泡/泄漏";

        /// <inheritdoc/>
        public Type ParamType => typeof(SoakParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new SoakRunner(context);
    }
}
