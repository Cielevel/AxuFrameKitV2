using System.IO;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// "Run Suite JSON..." 菜单（Editor 侧 scenario 运行入口的最简形态）：选一个 suite/scenario JSON →
    /// 交给 <see cref="ScenarioRunService.StartEditorPlayRun"/>（与看板按钮、pipeline 命令同一条启动-收尾契约）
    /// → 进入 Play Mode，由 Agent（<c>#if AXU_TEST_AGENT</c> 守护的 AgentBootstrap）在运行时读取并执行。
    /// <para>桥文件格式为与运行时侧的契约，勿改：
    /// <code>{"suitePath": "&lt;所选 JSON 绝对路径&gt;", "runId": "&lt;本轮标识&gt;", "quitOnDone": false}</code>
    /// 运行前提（Play 中 / define 未开启）经 <see cref="ScenarioPrecheck"/> 拦截并给出修复入口；
    /// 文件结构问题（schema/空 scenarios/ref 缺字段）也在进 Play 前拦下，不把用户丢进必然失败的 Play。
    /// 终态收尾与看板一致：Agent 写出终态或用户手动停止后自动退出 Play。</para>
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

            TryRunSuite(selected, out _);
        }

        /// <summary>
        /// 运行指定 suite/scenario JSON（菜单与看板 Scenario 页共用的运行路径）。
        /// 返回是否已成功启动（false = 缺运行前提/文件有结构问题/启动失败，原因见 <paramref name="error"/>）。
        /// </summary>
        public static bool TryRunSuite(string suiteAbsolutePath, out string error)
        {
            return TryRunSuite(suiteAbsolutePath, ScenarioRunRecord.SourceMenu, out error);
        }

        /// <summary>
        /// 运行指定 suite/scenario JSON 并标注发起方（<paramref name="source"/>，见
        /// <see cref="ScenarioRunRecord"/> 的 Source* 常量）。失败时弹对话框说明原因（交互入口），
        /// 并把同一段文案经 <paramref name="error"/> 回传给调用方（看板据此写状态栏）。
        /// </summary>
        public static bool TryRunSuite(string suiteAbsolutePath, string source, out string error)
        {
            if (!ScenarioPrecheck.EnsureRunnable())
            {
                error = ScenarioPrecheck.DescribeBlocker();
                return false;
            }

            if (!ScenarioRunService.StartEditorPlayRun(suiteAbsolutePath, null, out error, source))
            {
                EditorUtility.DisplayDialog("AxuTestFramework — 无法启动场景套件运行",
                    error ?? "未知原因（详见 Editor Console）。",
                    "好");
                return false;
            }

            Debug.Log($"[AxuTestFramework] 已进入 Play Mode，Agent 将读取并执行 suite: {suiteAbsolutePath}" +
                      "（写出终态后自动退出 Play 并回显本次报告）");
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
    }
}
