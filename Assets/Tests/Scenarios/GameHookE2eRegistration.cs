using AxuTestFramework.Agent.Bootstrap;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace TestAndSample.Scenarios
{
    /// <summary>
    /// gamehook e2e 验证脚本（模拟游戏侧注册，方案 §4.8）：
    /// 经 RuntimeInitializeOnLoad 注册包内模板能力 hook.sample + 一个 FromCode 代码态 scenario 定义，
    /// 供 gamehook-demo.suite.json 引用执行。仅测试资产，勿在正式工程模仿（正式做法见
    /// Packages/com.cielevel.axu-test-framework/Agent/Capabilities/GameHook/DOC.md）。
    /// </summary>
    public static class GameHookE2eRegistration
    {
#if UNITY_EDITOR || AXU_TEST_AGENT
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            AxuTestAgent.RegisterCapability(new AxuTestFramework.Agent.Capabilities.GameHook.HookSampleCapability());
            AxuTestAgent.RegisterScenario(ScenarioDef.FromCode(
                id: "hook.fromcode",
                capability: "echo",
                parameters: null));
            Debug.Log("[GameHookE2e] hook.sample 能力与 hook.fromcode scenario 已注册");
        }
#endif
    }
}
