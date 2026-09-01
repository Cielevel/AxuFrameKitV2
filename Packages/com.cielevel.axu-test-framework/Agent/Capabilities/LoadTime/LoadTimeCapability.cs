using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.LoadTime
{
    /// <summary>
    /// LoadTime 能力（加载时长，方案 §4.3）：场景矩阵逐个 LoadSceneAsync，记录加载时长/首帧时长/稳定帧时长。
    /// </summary>
    public class LoadTimeCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "loadtime"）。</summary>
        public const string CapabilityId = "loadtime";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "加载时长";

        /// <inheritdoc/>
        public Type ParamType => typeof(LoadTimeParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new LoadTimeRunner(context);
    }
}
