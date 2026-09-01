using System.Collections;
using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>
    /// 代码态 scenario 的协程形态工厂（方案 §4.8 gamehook）：
    /// Core 层 <see cref="ScenarioDef.FromCode"/> 只承载同步委托（纯 C#），协程准备（如等场景/资源就绪）
    /// 在本 Agent 侧扩展完成——宿主把 prepare 拼在能力 Runner 协程之前、同一超时看门狗内执行。
    /// </summary>
    public static class ScenarioDefExtensions
    {
        /// <summary>
        /// 代码态 scenario（协程准备形态）：prepare 在能力 Runner 之前执行（同一看门狗内，超时/取消同样生效）。
        /// 参数与 JSON params 等价；budgets/baseline 需要时对返回值继续设置。
        /// </summary>
        /// <param name="id">scenario id（suite 内唯一）。</param>
        /// <param name="capability">能力 id（须已注册，内建或自定义）。</param>
        /// <param name="prepare">准备协程工厂（每执行一次调用一次）；产出等待建议用 AgentWait.RealtimeSeconds。</param>
        /// <param name="parameters">能力参数（可空），字典深转 JObject。</param>
        public static ScenarioDef FromCodeRoutine(string id, string capability,
            System.Func<IEnumerator> prepare, IDictionary<string, object> parameters = null)
        {
            return new ScenarioDef
            {
                Id = id,
                Capability = capability,
                CodePrepare = prepare,
                Params = parameters != null ? Newtonsoft.Json.Linq.JObject.FromObject(parameters) : new Newtonsoft.Json.Linq.JObject(),
            };
        }
    }
}
