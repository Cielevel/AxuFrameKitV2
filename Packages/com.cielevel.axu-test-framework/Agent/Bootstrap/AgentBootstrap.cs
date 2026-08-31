using AxuTestFramework.Agent.Runner;
using UnityEngine;

namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>
    /// Agent 场景入口组件：拖入任意场景即可自动初始化（与 RuntimeInitializeOnLoad 自动挂载等价、幂等）。
    /// define 未开启（AXU_TEST_AGENT）时一切为空操作，不创建任何宿主。
    /// </summary>
    public class AgentBootstrap : MonoBehaviour
    {
        /// <summary>场景加载前自动挂起 Agent 宿主（define 未开启时为空方法体）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoInit()
        {
#if AXU_TEST_AGENT
            AgentRunnerHost.EnsureHost().StartFromSources();
#endif
        }

        void Awake()
        {
#if AXU_TEST_AGENT
            // 幂等：EnsureHost 复用既有宿主，StartFromSources 自身防重入
            AgentRunnerHost.EnsureHost().StartFromSources();
#endif
        }
    }
}
