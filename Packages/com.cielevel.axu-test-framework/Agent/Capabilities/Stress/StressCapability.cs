using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// Stress 能力（压力/承载，方案 §4.4）：组合能力——phases 列表顺序执行
    /// spawner（阶梯加压）/ capacity（容量探测：自反馈加压 + 二分收敛）/ churn（对象翻搅）/
    /// flood（自建托管事件洪峰）。详见同目录 DOC.md。
    /// </summary>
    public class StressCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "stress"）。</summary>
        public const string CapabilityId = "stress";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "压力/承载";

        /// <inheritdoc/>
        public Type ParamType => typeof(StressParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new StressRunner(context);
    }
}
