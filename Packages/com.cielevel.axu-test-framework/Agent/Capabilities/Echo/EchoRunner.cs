using System.Collections;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Echo
{
    /// <summary>
    /// Echo 运行器：开始打印消息 → 分段真实时间等待 DelaySeconds → 结束打印 "{message} done" → Passed。
    /// </summary>
    public class EchoRunner : IScenarioRunner
    {
        readonly ScenarioContext _context;

        public EchoRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = scenario.Params != null ? AxuJson.ToObject<EchoParams>(scenario.Params) : null;
            if (parameters == null) parameters = new EchoParams();

            var message = string.IsNullOrEmpty(parameters.Message) ? "echo" : parameters.Message;
            var log = _context != null ? _context.Log : null;
            var steps = parameters.ClampedSteps;
            var perStep = parameters.ClampedDelaySeconds / steps;

            if (log != null) log.Info(message);

            for (var i = 0; i < steps; i++)
            {
                if (handle != null && handle.CancelRequested) yield break;
                yield return AgentWait.RealtimeSeconds(perStep);
            }

            if (log != null) log.Info($"{message} done");
            if (handle != null) handle.Status = RunStatus.Passed;
        }
    }
}
