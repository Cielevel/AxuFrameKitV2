using System.IO;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// "Run Suite JSON..." 菜单（方案 §3.4：Editor 侧 scenario 运行入口的最简形态）：
    /// 选一个 suite/scenario JSON → 把选中绝对路径写入桥文件 <see cref="PendingSuiteBridgePath"/>
    /// → 进入 Play Mode，由 Agent（<c>#if AXU_TEST_AGENT</c> 守护的 AgentBootstrap）在运行时读取并执行。
    /// <para>桥文件格式为与运行时侧的契约，勿改：
    /// <code>{"suitePath": "&lt;所选 JSON 绝对路径&gt;", "quitOnDone": false}</code>
    /// 前置检查：Play 中拦截；define 未开启时经 <see cref="ScenarioPrecheck.WarnIfAgentDisabled"/>
    /// 弹可跳过警告（未开 define 时 Agent 入口不参与编译，进 Play 也不会有 Agent 读取桥文件）。</para>
    /// </summary>
    public static class ScenarioRunMenu
    {
        /// <summary>菜单路径。</summary>
        public const string MenuPath = "AxuTestFramework/Run Suite JSON...";

        /// <summary>桥文件路径（相对工程根 Temp/；与运行时侧契约，勿改）。</summary>
        public const string PendingSuiteBridgePath = "Temp/axu_pending_suite.json";

        [MenuItem(MenuPath, true)]
        public static bool ValidateRunSuiteJson()
        {
            // Play 中禁用：进 Play 前 AgentBootstrap 已读取桥文件，运行中改写不生效，
            // 需退出重进（MenuItem 无公开 tooltip API，禁用原因在本类 XML 注释与执行分支弹窗中说明）。
            return !EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath, false)]
        public static void RunSuiteJson()
        {
            var selected = EditorUtility.OpenFilePanel(
                "选择 Suite / Scenario JSON", ResolveStartDirectory(), "json");
            if (string.IsNullOrEmpty(selected))
                return;

            TryRunSuite(selected);
        }

        /// <summary>
        /// 运行指定 suite/scenario JSON（菜单与看板 Scenario 页共用的运行路径）：
        /// Play 中拦截（提示退出重进）→ <see cref="ScenarioPrecheck.WarnIfAgentDisabled"/> 可跳过预检
        /// → 写桥文件 → 进入 Play Mode。返回是否已成功启动（false = 被拦截或用户取消）。
        /// </summary>
        public static bool TryRunSuite(string suiteAbsolutePath)
        {
            if (EditorApplication.isPlaying)
            {
                // 防御分支（菜单 validate 已禁用；看板按钮/脚本直呼时兜底）：不强行重启，提示退出重进。
                EditorUtility.DisplayDialog(
                    "Run Suite JSON",
                    "当前已处于 Play Mode。桥文件需在进入 Play 前写入才会被 Agent 读取——" +
                    "请退出 Play 后重新运行，下一次进 Play 时生效。",
                    "好");
                return false;
            }

            if (!ScenarioPrecheck.WarnIfAgentDisabled())
                return false;

            WriteBridge(suiteAbsolutePath);
            EditorApplication.isPlaying = true;

            Debug.Log(
                $"[AxuTestFramework] 桥文件已写入: {PendingSuiteBridgePath}；suite: {suiteAbsolutePath}" +
                "——已进入 Play Mode，Agent 将读取并执行。");
            return true;
        }

        /// <summary>文件选择起始目录：工程约定场景目录存在则用之，否则退回 Assets 根。</summary>
        private static string ResolveStartDirectory()
        {
            return AssetDatabase.IsValidFolder(ScenarioJsonMenu.ScenariosFolder)
                ? Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    ScenarioJsonMenu.ScenariosFolder.Replace('/', Path.DirectorySeparatorChar))
                : Application.dataPath;
        }

        /// <summary>写桥文件 <see cref="PendingSuiteBridgePath"/>（Temp 目录不存在则创建；
        /// 工程根由 Application.dataPath 推导，不依赖进程 cwd）。</summary>
        private static void WriteBridge(string suiteAbsolutePath)
        {

            // Phase 4：桥文件写入统一走 ScenarioRunService（与 pipeline 命令共用同一契约实现）
            ScenarioRunService.WriteBridgeFile(suiteAbsolutePath, null);
        }
    }
}
