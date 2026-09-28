using System.Linq;
using AxuTestFramework.Editor;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] RunReadinessCheck：运行就绪判定的纯函数契约（阶段 2 首次运行引导）。
    /// 关键契约点（每条对应一个真实错误行为）：
    ///  - 阻判定序稳定：in_play &gt; agent_define &gt; 双在途（utf/scenario）&gt; build_scenes，编译中恒在末尾；
    ///  - code 目标不要求 define（单元测试不经 Agent，define 未开不能拦它）；
    ///  - 非 scene 目标不要求构建场景清单；
    ///  - 编译中是非阻断提示：IsReady 仍为真，只进 HasWarnings。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class RunReadinessCheckTests
    {
        /// <summary>全绿基线输入（各可选项按"已满足"构造，用例里只翻转关心的字段）。</summary>
        static RunReadinessInput ReadyInput()
        {
            return new RunReadinessInput
            {
                IsPlaying = false,
                IsCompiling = false,
                HasAgentDefine = true,
                UtfRunInFlight = false,
                ScenarioRunInFlight = false,
                BuildSettingsHasScenes = true,
            };
        }

        static RunReadinessIssue Find(RunReadiness readiness, string id)
        {
            return readiness.Issues.FirstOrDefault(i => i != null && i.Id == id);
        }

        // ------------------------------------------------------------------ 各前提独立判定

        [Test]
        public void Evaluate_各项就绪_无阻断()
        {
            var readiness = RunReadinessCheck.Evaluate(ReadyInput(), requireAgentDefine: true, requireBuildScenes: true);

            Assert.That(readiness.IsReady, Is.True);
            Assert.That(readiness.HasWarnings, Is.False);
            Assert.That(readiness.Issues, Is.Empty);
        }

        [Test]
        public void Evaluate_define未开且需要define_报agent_define阻断FixKind为EnableAgentDefine()
        {
            var input = ReadyInput();
            input.HasAgentDefine = false;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: false);
            var issue = Find(readiness, "agent_define");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.IsBlocking, Is.True);
            Assert.That(issue.FixKind, Is.EqualTo(ReadinessFixKind.EnableAgentDefine));
            Assert.That(issue.Message, Is.Not.Empty);
            Assert.That(issue.FixHint, Is.Not.Empty, "阻断项必须给出修复指引");
        }

        [Test]
        public void Evaluate_code目标不需要define_define未开_不报agent_define()
        {
            var input = ReadyInput();
            input.HasAgentDefine = false;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: false, requireBuildScenes: false);

            Assert.That(Find(readiness, "agent_define"), Is.Null, "单元测试不经 Agent，不能被 define 拦截");
            Assert.That(readiness.IsReady, Is.True);
        }

        [Test]
        public void Evaluate_Play中_报in_play_mode阻断FixKind为ExitPlayMode()
        {
            var input = ReadyInput();
            input.IsPlaying = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);
            var issue = Find(readiness, "in_play_mode");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.FixKind, Is.EqualTo(ReadinessFixKind.ExitPlayMode));
        }

        [Test]
        public void Evaluate_utf在途_报utf_run_in_flight且FixKind为WaitUntilIdle()
        {
            var input = ReadyInput();
            input.UtfRunInFlight = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);
            var issue = Find(readiness, "utf_run_in_flight");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.IsBlocking, Is.True);
            Assert.That(issue.FixKind, Is.EqualTo(ReadinessFixKind.WaitUntilIdle));
        }

        [Test]
        public void Evaluate_scenario在途_报scenario_run_in_flight()
        {
            var input = ReadyInput();
            input.ScenarioRunInFlight = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);
            var issue = Find(readiness, "scenario_run_in_flight");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.IsBlocking, Is.True);
            Assert.That(issue.FixKind, Is.EqualTo(ReadinessFixKind.WaitUntilIdle),
                "双在途都归「等待」类修复，与 utf 在途一致");
        }

        [Test]
        public void Evaluate_场景运行记录未收尾_阻断首用入口()
        {
            var input = ReadyInput();
            input.ScenarioRecordPresent = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: false);
            var issue = Find(readiness, "scenario_run_in_flight");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.Message, Does.Contain("运行记录"));
        }

        [Test]
        public void Evaluate_编译中_仅非阻断提示IsReady仍为真()
        {
            var input = ReadyInput();
            input.IsCompiling = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);
            var issue = Find(readiness, "compiling");

            Assert.That(readiness.IsReady, Is.True, "编译中是提示不是阻断");
            Assert.That(readiness.HasWarnings, Is.True);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.IsBlocking, Is.False);
            Assert.That(issue.Message, Is.Not.Empty);
        }

        [Test]
        public void Evaluate_scene目标场景清单为空_报build_scenes_empty()
        {
            var input = ReadyInput();
            input.BuildSettingsHasScenes = false;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: false, requireBuildScenes: true);
            var issue = Find(readiness, "build_scenes_empty");

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.FixKind, Is.EqualTo(ReadinessFixKind.ConfigureBuildScenes));
        }

        [Test]
        public void Evaluate_非scene目标场景清单为空_不报()
        {
            var input = ReadyInput();
            input.BuildSettingsHasScenes = false;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: false);

            Assert.That(Find(readiness, "build_scenes_empty"), Is.Null, "自检/单元测试/性能采样不依赖构建场景清单");
            Assert.That(readiness.IsReady, Is.True);
        }

        // ------------------------------------------------------------------ 多阻断并存（顺序稳定）

        [Test]
        public void Evaluate_多阻断并存_全部列出且顺序稳定()
        {
            var input = ReadyInput();
            input.IsPlaying = true;
            input.HasAgentDefine = false;
            input.UtfRunInFlight = true;
            input.ScenarioRunInFlight = true;
            input.BuildSettingsHasScenes = false;
            input.IsCompiling = true;

            var readiness = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(readiness.Issues.Select(i => i.Id).ToArray(), Is.EqualTo(new[]
            {
                "in_play_mode",
                "agent_define",
                "utf_run_in_flight",
                "scenario_run_in_flight",
                "build_scenes_empty",
                "compiling", // 非阻断提示恒在末尾
            }));
        }

        // ------------------------------------------------------------------ 输入兜底

        [Test]
        public void Evaluate_输入为null_按全空输入处理不抛()
        {
            var readiness = RunReadinessCheck.Evaluate(null, requireAgentDefine: false, requireBuildScenes: false);

            Assert.That(readiness, Is.Not.Null);
            Assert.That(readiness.IsReady, Is.True);
        }
    }
}
