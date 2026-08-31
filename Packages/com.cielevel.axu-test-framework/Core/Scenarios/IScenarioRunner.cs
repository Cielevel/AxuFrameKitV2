using System.Collections;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 场景运行器：以协程状态机执行单个已解析 scenario，可被宿主超时/取消打断。
    /// 等待请产出 <c>AxuTestFramework.Agent.Tools.AgentWait.RealtimeSeconds</c>（分段 ≤0.25s，保证看门狗响应）。
    /// </summary>
    public interface IScenarioRunner
    {
        /// <summary>执行协程；结束时不改 Status 则由宿主判 Passed，异常/超时由宿主兜底置 Error。</summary>
        IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle);
    }
}
