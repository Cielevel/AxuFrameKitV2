using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 点按注入后端接口（主 Agent 程序集定义，实现位于可选守护程序集）：
    /// 装有 Input System 时为 <c>AxuTestFramework.Agent.Input.InputSystemTapInjector</c>。
    /// 由 <see cref="AgentInputBackends"/> 反射发现——主程序集不硬引用任何输入包（backlog #5 正规化）。
    /// UGUI 兜底不经此接口（见 <see cref="IAgentUguiProbe"/>）。
    /// </summary>
    public interface IAgentTapInjector
    {
        /// <summary>后端标识（当前仅 <see cref="AgentInputBackends.InputSystemInjectorName"/>）。</summary>
        string Name { get; }

        /// <summary>当前是否有可用注入目标（Input System：鼠标或触屏设备存在）。</summary>
        bool Available { get; }

        /// <summary>注入一次点按；返回是否已产生真实输入事件（false = 调用方降级）。</summary>
        bool TryInject(Vector2 screenPosition);
    }
}
