using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Smoke
{
    /// <summary>
    /// Smoke 能力（冒烟矩阵，方案 §4.6）：场景清单逐个加载 → 稳定窗（帧 + 秒）内错误计数 → 截图留证；
    /// 产出"场景 × {进得去?、耗时、错误数、截图}"矩阵，出包回归首选。
    /// </summary>
    public class SmokeCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "smoke"）。</summary>
        public const string CapabilityId = "smoke";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "冒烟矩阵";

        /// <inheritdoc/>
        public Type ParamType => typeof(SmokeParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new SmokeRunner(context);
    }
}
