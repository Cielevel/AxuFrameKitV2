using System;
using System.Collections;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.GameHook
{
    /// <summary>
    /// 【游戏侧自定义能力模板】——非内建注册（BuiltInCapabilities 不含本类），复制到游戏工程改造后
    /// 经 <c>AxuTestAgent.RegisterCapability(new HookSampleCapability())</c> 显式注册（方案 §4.8 gamehook）。
    /// 演示最小闭环：自定义能力 + FromCode scenario + 参数读取；详见同目录 DOC.md。
    /// </summary>
    public class HookSampleCapability : IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability / FromCode 的 capability 参数引用它）。</summary>
        public const string CapabilityId = "hook.sample";

        /// <inheritdoc/>
        public string Id => CapabilityId;

        /// <inheritdoc/>
        public string DisplayName => "GameHook 示例（模板）";

        /// <inheritdoc/>
        public Type ParamType => typeof(HookSampleParams);

        /// <inheritdoc/>
        public IScenarioRunner CreateRunner(ScenarioContext context) => new Runner(context);

        /// <summary>能力参数 POCO（scenario.params 反序列化目标；字段名即 params 键）。</summary>
        [Serializable]
        public class HookSampleParams
        {
            /// <summary>演示参数：要打出的消息。</summary>
            public string message = "hook.sample ok";
        }

        /// <summary>最小 Runner：读参数 → 打日志 → 结束（结束不改 Status 即由宿主判 Passed）。</summary>
        class Runner : IScenarioRunner
        {
            readonly ScenarioContext _context;

            public Runner(ScenarioContext context)
            {
                _context = context;
            }

            public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
            {
                var parameters = scenario.Params?.ToObject<HookSampleParams>() ?? new HookSampleParams();
                _context.Log?.Info($"[{CapabilityId}] {parameters.message}（seed={_context.Seed?.ToString() ?? "-"}）");

                // 需要等待时用 AgentWait（分段 ≤0.25s，保证看门狗响应）：
                // yield return AxuTestFramework.Agent.Tools.AgentWait.RealtimeSeconds(0.5);
                yield break;
            }
        }
    }
}
