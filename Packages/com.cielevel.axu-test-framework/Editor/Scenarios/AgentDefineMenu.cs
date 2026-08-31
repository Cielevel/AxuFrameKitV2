using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// <see cref="DefineName"/>（AXU_TEST_AGENT）脚本符号开关菜单（方案 §3.1"装包 + 开 define"的接入开关）。
    /// <para>读写当前 <c>EditorUserBuildSettings.activeBuildTarget</c> 对应 BuildTargetGroup 的
    /// scripting define symbols：经 <see cref="NamedBuildTarget.FromBuildTargetGroup"/>（Unity 6 API）
    /// 调 <c>PlayerSettings.Get/SetScriptingDefineSymbols</c>，不直接改 ProjectSettings.asset。
    /// 开启后 Agent 程序集内 <c>#if AXU_TEST_AGENT</c> 守护的入口（AgentBootstrap 等）参与编译；
    /// 关闭即从构建中整体剔除。写符号串会触发脚本重编译，属预期行为。</para>
    /// <para>符号名与运行时侧为契约（方案 D2），勿改。</para>
    /// </summary>
    public static class AgentDefineMenu
    {
        /// <summary>菜单路径。</summary>
        public const string MenuPath = "AxuTestFramework/Enable Test Agent";

        /// <summary>Agent 编译守护符号名（与运行时侧契约，勿改）。</summary>
        public const string DefineName = "AXU_TEST_AGENT";

        [MenuItem(MenuPath, false)]
        public static void Toggle()
        {
            var named = GetActiveNamedBuildTarget();
            var defines = ReadDefines(named);
            var enabled = defines.Contains(DefineName);

            var next = enabled
                ? defines.Where(d => d != DefineName).ToList()
                : AppendDefine(defines, DefineName);

            var joined = string.Join(";", next);
            PlayerSettings.SetScriptingDefineSymbols(named, joined);

            Debug.Log(
                $"[AxuTestFramework] {DefineName} 已{(enabled ? "移除" : "添加")}（{named.TargetName}）。" +
                $"当前符号串: \"{joined}\"——脚本重编译即将发生。");
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, HasAgentDefine());
            return true;
        }

        /// <summary>当前 activeBuildTarget 对应的 NamedBuildTarget（Unity 6 API）。</summary>
        public static NamedBuildTarget GetActiveNamedBuildTarget()
        {
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }

        /// <summary>当前 activeBuildTarget 对应 BuildTargetGroup 是否已开启 <see cref="DefineName"/>。
        /// 供 <see cref="ScenarioRunMenu"/> 等入口做前置检查。</summary>
        public static bool HasAgentDefine()
        {
            return ReadDefines(GetActiveNamedBuildTarget()).Contains(DefineName);
        }

        /// <summary>读当前符号串并解析为去空白去重清单（兼容分号/逗号两种历史分隔符）。</summary>
        private static List<string> ReadDefines(NamedBuildTarget named)
        {
            return PlayerSettings.GetScriptingDefineSymbols(named)
                .Split(';', ',')
                .Select(d => d.Trim())
                .Where(d => d.Length > 0)
                .Distinct()
                .ToList();
        }

        /// <summary>追加符号（ReadDefines 已去重，幂等）。</summary>
        private static List<string> AppendDefine(List<string> defines, string define)
        {
            return defines.Concat(new[] { define }).ToList();
        }
    }
}
