using System;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Monkey
{
    /// <summary>
    /// Monkey 能力（猴子测试，方案 §4.7 v1）：显式 seed 随机点按序列施虐目标场景，
    /// JSONL 流水留痕 + 错误监视（可即停）+ 卡死检测，产出 monkey.* metrics。
    /// 输入注入由 <see cref="AxuTestFramework.Agent.Tools.AgentInput"/> 统一封装
    /// （Input System 主路径 / UGUI ExecuteEvents 降级）。
    /// </summary>
    public class MonkeyCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability = "monkey"）。</summary>
        public const string CapabilityId = "monkey";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "猴子测试";

        /// <inheritdoc/>
        public Type ParamType => typeof(MonkeyParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new MonkeyRunner(context);
    }
}
