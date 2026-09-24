using UnityEditor;
using UnityEngine.SceneManagement;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行就绪判定（首次运行引导与各类运行入口共用同一份判定与文案）：
    /// <see cref="Evaluate"/> 是注入全量输入的纯函数（单测只测它，不触 Unity 状态），
    /// <see cref="GatherInput"/> 才读 Unity 真实状态（主线程调用）。
    /// <para>阻断判定顺序（多问题并存时按此稳定列出）：在 Play 中 &gt; define 未开（仅要求时）&gt;
    /// UTF/scenario 运行在途 &gt; 构建场景清单为空（仅 scene 类目标）；编译中为非阻断提示，不改变可运行结论。</para>
    /// </summary>
    public static class RunReadinessCheck
    {
        /// <summary>
        /// 纯函数判定。<paramref name="requireAgentDefine"/>=false 用于 code 类目标
        /// （单元测试不经 Agent，不需要 AXU_TEST_AGENT）；
        /// <paramref name="requireBuildScenes"/>=true 仅 scene 类目标需要
        /// （smoke 的 buildSettings 来源枚举的是构建清单内已启用场景，清单为空则没有可检查的内容）。
        /// </summary>
        public static RunReadiness Evaluate(RunReadinessInput input, bool requireAgentDefine, bool requireBuildScenes)
        {
            var readiness = new RunReadiness();
            var source = input ?? new RunReadinessInput();

            if (source.IsPlaying)
            {
                readiness.Issues.Add(RunReadinessIssue.Blocking(
                    "in_play_mode",
                    "Unity 当前处于 Play Mode（正在运行游戏），此时无法发起新的测试运行。",
                    "点击 Unity 上方工具栏的停止按钮退出 Play Mode，再重新发起运行。",
                    ReadinessFixKind.ExitPlayMode));
            }

            if (requireAgentDefine && !source.HasAgentDefine)
            {
                readiness.Issues.Add(RunReadinessIssue.Blocking(
                    "agent_define",
                    "本工程尚未开启测试插件所需的脚本符号（AXU_TEST_AGENT），场景类示例测试进 Play 后不会被执行。",
                    "用界面上的\"开启测试支持\"入口（等价菜单 AxuTestFramework/Enable Test Agent）开启，" +
                    "随后等待 Unity 底部的编译进度结束再运行。",
                    ReadinessFixKind.EnableAgentDefine));
            }

            if (source.UtfRunInFlight)
            {
                readiness.Issues.Add(RunReadinessIssue.Blocking(
                    "utf_run_in_flight",
                    "已有一批单元测试正在运行，同一时间只能进行一个测试运行。",
                    "等待当前单元测试运行结束（或手动停止它）后再发起。",
                    ReadinessFixKind.WaitUntilIdle));
            }

            if (source.ScenarioRunInFlight)
            {
                readiness.Issues.Add(RunReadinessIssue.Blocking(
                    "scenario_run_in_flight",
                    "已有一个场景套件正在运行（Play Mode 进行中），同一时间只能进行一个测试运行。",
                    "等待当前运行结束（结束后会自动退出 Play Mode）再发起；确认卡住时可停止 Play 后重试。",
                    ReadinessFixKind.WaitUntilIdle));
            }

            if (requireBuildScenes && !source.BuildSettingsHasScenes)
            {
                readiness.Issues.Add(RunReadinessIssue.Blocking(
                    "build_scenes_empty",
                    "构建场景清单（Build Settings / Build Profiles 的场景列表）中还没有已启用的场景，" +
                    "场景冒烟测试没有可检查的内容。",
                    "打开菜单 File/Build Profiles（或旧版 Build Settings），把要检查的场景加入场景清单并勾选启用。",
                    ReadinessFixKind.ConfigureBuildScenes));
            }

            if (source.IsCompiling)
            {
                readiness.Issues.Add(RunReadinessIssue.Warning(
                    "compiling",
                    "Unity 正在编译脚本，此时发起的运行结果可能不完整——建议等编译进度结束后再开始。"));
            }

            return readiness;
        }

        /// <summary>
        /// 读 Unity 真实状态组装判定输入（必须在主线程调用）：Play/编译态、define 开关、
        /// 两侧在途判定（<see cref="AxuTestRunService.IsAxuRunInFlight"/> / <see cref="ScenarioRunService.IsScenarioRunInFlight"/>）、
        /// 构建场景清单是否含已启用场景——与 smoke 能力 buildSettings 来源同口径
        /// （<c>SceneManager.sceneCountInBuildSettings</c> 只统计已启用场景）。
        /// </summary>
        public static RunReadinessInput GatherInput()
        {
            return new RunReadinessInput
            {
                IsPlaying = EditorApplication.isPlaying,
                IsCompiling = EditorApplication.isCompiling,
                HasAgentDefine = AgentDefineMenu.HasAgentDefine(),
                UtfRunInFlight = AxuTestRunService.IsAxuRunInFlight(),
                ScenarioRunInFlight = ScenarioRunService.IsScenarioRunInFlight(),
                BuildSettingsHasScenes = SceneManager.sceneCountInBuildSettings > 0,
            };
        }
    }
}
