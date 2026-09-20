using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// UGUI 命中检测 / 降级点按接口（实现位于可选守护程序集 <c>AxuTestFramework.Agent.UGUI</c>，
    /// com.unity.ugui 存在时编译）。命中信息用于 monkey 流水留痕；降级点按用于无 Input System 工程。
    /// 主程序集不硬引用 uGUI（backlog #5 正规化）。
    /// </summary>
    public interface IAgentUguiProbe
    {
        /// <summary>raycast 命中对象名；EventSystem 缺失或未命中返回 null。</summary>
        string HitTest(Vector2 screenPosition);

        /// <summary>对命中对象执行 pointerClick（沿父链）；未命中或无 EventSystem 时为 no-op。</summary>
        void Click(Vector2 screenPosition);
    }
}
