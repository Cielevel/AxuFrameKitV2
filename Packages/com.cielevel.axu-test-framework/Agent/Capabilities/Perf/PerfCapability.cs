using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Perf
{
    /// <summary>
    /// Perf 能力（性能基准，方案 §4.2）：加载目标场景 → warmup（丢弃采样）→ 度量期采样 →
    /// （可选）结束截图；指标由 params.collectors 选定的采集器产出，预算判定由宿主完成。
    /// </summary>
    public class PerfCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "perf"）。</summary>
        public const string CapabilityId = "perf";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "性能基准";

        /// <inheritdoc/>
        public Type ParamType => typeof(PerfParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new PerfRunner(context);
    }
}
