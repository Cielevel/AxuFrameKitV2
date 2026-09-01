using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 运行入口的 define 预检（Phase 1 遗留项收口）：
    /// <see cref="WarnIfAgentDisabled"/> 检查当前 BuildTargetGroup 的 scripting define symbols
    /// 是否含 <see cref="AgentDefineMenu.DefineName"/>（AXU_TEST_AGENT）——Agent 桥文件的运行路径
    /// （AgentBootstrap 等 <c>#if AXU_TEST_AGENT</c> 守护代码）需要该 define 才会参与编译。
    /// <para>警告可跳过：define 缺失时弹对话框说明影响，用户可选"仍要继续"（Editor 内经桥文件驱动的
    /// 既有行为不受本检查影响，仅提示）或"取消"中止。供 <see cref="ScenarioRunMenu"/> 与看板
    /// Scenario 页共用。</para>
    /// </summary>
    public static class ScenarioPrecheck
    {
        /// <summary>
        /// define 未开启时弹可跳过警告。返回是否继续运行：
        /// define 已开启 → 恒 true；未开启 → 用户选"仍要继续" true、选"取消" false。
        /// </summary>
        public static bool WarnIfAgentDisabled()
        {
            if (AgentDefineMenu.HasAgentDefine())
                return true;

            var named = AgentDefineMenu.GetActiveNamedBuildTarget();
            return EditorUtility.DisplayDialog(
                "AxuTestFramework — Agent define 未开启",
                $"当前 BuildTargetGroup（{named.TargetName}）的脚本符号未开启 {AgentDefineMenu.DefineName}。\n" +
                "Agent 桥文件运行路径（AgentBootstrap 等 #if AXU_TEST_AGENT 守护代码）需要该 define 才会参与编译，" +
                "未开启时进入 Play Mode 不会有 Agent 读取桥文件，场景不会执行。\n\n" +
                "可先执行菜单 AxuTestFramework/Enable Test Agent 并等待重编译完成。",
                "仍要继续",
                "取消");
        }
    }
}
