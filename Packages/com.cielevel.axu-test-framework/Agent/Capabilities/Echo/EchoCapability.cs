using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Echo
{
    /// <summary>Echo 能力（场景测试链路的"ping"：仅延时+日志，验证 suite→分发→Runner→报告 全链路）。</summary>
    public class EchoCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "echo"）。</summary>
        public const string CapabilityId = "echo";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "Echo（链路自检）";

        /// <inheritdoc/>
        public Type ParamType => typeof(EchoParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new EchoRunner(context);
    }
}
