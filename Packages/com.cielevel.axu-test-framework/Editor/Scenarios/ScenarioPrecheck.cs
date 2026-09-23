using UnityEditor;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 运行入口的运行前提预检（看板 / 菜单 / pipeline 共用同一份判定与文案）：
    /// ① 已在 Play Mode（桥文件必须在进 Play 前写入）② 当前 BuildTargetGroup 未开启
    /// <see cref="AgentDefineMenu.DefineName"/>（Agent 入口不参与编译，进 Play 也不会执行 suite）。
    /// <para>缺前提一律拦在 Play 之外——进入一个必然不会执行测试的 Play 状态，用户只会看到
    /// "似乎一直在运行"，这正是运行闭环要消除的体验。<see cref="DescribeBlocker"/> 给出的文案
    /// 同时包含影响与修复入口，供看板状态栏 / 菜单对话框 / pipeline 错误信封原样使用。</para>
    /// </summary>
    public static class ScenarioPrecheck
    {
        /// <summary>
        /// 返回阻断本次运行的原因（面向用户的中文，含修复入口）；具备全部前提时返回 null。
        /// </summary>
        public static string DescribeBlocker()
        {
            if (EditorApplication.isPlaying)
            {
                return "当前已处于 Play Mode：桥文件需在进入 Play 前写入才会被 Agent 读取，" +
                       "请先退出 Play 再发起运行（运行中想改套件也需退出后重跑）。";
            }

            if (!AgentDefineMenu.HasAgentDefine())
            {
                var named = AgentDefineMenu.GetActiveNamedBuildTarget();
                return $"当前 BuildTargetGroup（{named.TargetName}）的脚本符号未开启 {AgentDefineMenu.DefineName}，" +
                       "Agent 入口不会参与编译，进 Play 也不会执行 suite。" +
                       "\n修复：执行菜单 AxuTestFramework/Enable Test Agent，等待重编译完成后重试。";
            }

            return null;
        }

        /// <summary>
        /// 缺前提时弹出说明对话框（含修复入口）并返回 false；具备前提时直接返回 true。
        /// 对话框是阻塞式的，只在交互入口（菜单/看板按钮）调用，pipeline 路径用 <see cref="DescribeBlocker"/> 回错误信封。
        /// </summary>
        public static bool EnsureRunnable()
        {
            var blocker = DescribeBlocker();
            if (blocker == null)
                return true;

            EditorUtility.DisplayDialog("AxuTestFramework — 无法运行场景套件", blocker, "好");
            return false;
        }
    }
}
