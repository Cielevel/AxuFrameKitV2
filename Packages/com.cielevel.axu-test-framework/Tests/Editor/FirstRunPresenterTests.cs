using AxuTestFramework.Editor;
using NUnit.Framework;
using ScenarioReportSummary = AxuTestFramework.Core.Report.ReportSummary;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] FirstRunPresenter + ScenarioRunStatusPresenter.ScopePrefix：
    /// 首次运行引导的纯文案映射契约（阶段 2）。
    /// 关键契约点：
    ///  - 自检全过的结论必须带限定（"测试工具链可用" + "不代表项目全部测试通过"）——防用户把示例结果当项目质量结论；
    ///  - 自检有失败/被中断的文案不得出现"可用/通过"字样；
    ///  - 未就绪按钮文案不出现运行类动词；就绪按钮含"运行示例测试"；
    ///  - 就绪横幅返回 null（UI 据此隐藏）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class FirstRunPresenterTests
    {
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

        static AxuScenarioRunStatus CompletedStatus(int exitCode, int total, int passed, int warned, int failed)
        {
            return new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = exitCode,
                Summary = new ScenarioReportSummary
                {
                    Total = total,
                    Passed = passed,
                    Warned = warned,
                    Failed = failed,
                },
            };
        }

        // ------------------------------------------------------------------ 自检结果一句话

        [Test]
        public void DescribeSelfCheckOutcome_completed全过_含工具链可用且含不代表项目全部测试通过的限定()
        {
            var status = CompletedStatus(exitCode: 0, total: 1, passed: 1, warned: 0, failed: 0);

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Does.Contain("测试工具链可用"));
            Assert.That(text, Does.Contain("不代表项目全部测试通过"), "示例结论必须自带限定");
        }

        [Test]
        public void DescribeSelfCheckOutcome_completed有fail_不含工具链可用()
        {
            var status = CompletedStatus(exitCode: 1, total: 1, passed: 0, warned: 0, failed: 1);

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Is.Not.Null.And.Not.Empty);
            Assert.That(text, Does.Not.Contain("可用"), "有失败时不得声称可用");
            Assert.That(text, Does.Not.Contain("通过"), "有失败时不得出现通过语义");
        }

        [Test]
        public void DescribeSelfCheckOutcome_completed仅警告_仍算工具链可用且带限定()
        {
            var status = CompletedStatus(exitCode: 2, total: 1, passed: 1, warned: 1, failed: 0);

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Does.Contain("测试工具链可用"));
            Assert.That(text, Does.Contain("不代表项目全部测试通过"));
        }

        [Test]
        public void DescribeSelfCheckOutcome_aborted_不出现通过或可用字样()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Aborted,
                Message = "Play Mode 在运行结束前退出",
            };

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Is.Not.Null.And.Not.Empty);
            Assert.That(text, Does.Not.Contain("可用"));
            Assert.That(text, Does.Not.Contain("通过"));
        }

        [Test]
        public void DescribeSelfCheckOutcome_error_给错误说明且不称可用()
        {
            var status = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Error };

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Is.Not.Null.And.Not.Empty);
            Assert.That(text, Does.Not.Contain("可用"));
            Assert.That(text, Does.Not.Contain("通过"));
        }

        [Test]
        public void DescribeSelfCheckOutcome_null_返回未运行提示()
        {
            var text = FirstRunPresenter.DescribeSelfCheckOutcome(null);

            Assert.That(text, Is.Not.Null.And.Not.Empty);
            Assert.That(text, Does.Contain("还没有运行"));
        }

        [Test]
        public void DescribeSelfCheckOutcome_running_提示正在运行()
        {
            var status = new AxuScenarioRunStatus { Status = AxuScenarioRunStatus.Running };

            var text = FirstRunPresenter.DescribeSelfCheckOutcome(status);

            Assert.That(text, Does.Contain("正在运行"));
        }

        // ------------------------------------------------------------------ 按钮文案与悬浮说明

        [Test]
        public void RunButtonLabel_未就绪_不含运行动词且Tooltip含阻断原因()
        {
            var readiness = RunReadinessCheck.Evaluate(new RunReadinessInput { IsPlaying = true },
                requireAgentDefine: false, requireBuildScenes: false);

            var label = FirstRunPresenter.RunButtonLabel(readiness);
            var tooltip = FirstRunPresenter.RunButtonTooltip(readiness);

            Assert.That(label, Is.Not.Empty);
            Assert.That(label, Does.Not.Contain("运行"), "未就绪按钮不得出现运行类动词");
            Assert.That(tooltip, Does.Contain("Play Mode"));
            Assert.That(tooltip, Does.Contain(readiness.Issues[0].Message), "悬浮说明须合并阻断项的影响说明");
        }

        [Test]
        public void RunButtonLabel_就绪_含运行示例测试()
        {
            var readiness = RunReadinessCheck.Evaluate(ReadyInput(), requireAgentDefine: true, requireBuildScenes: true);

            Assert.That(readiness.IsReady, Is.True);
            Assert.That(FirstRunPresenter.RunButtonLabel(readiness), Does.Contain("运行示例测试"));
        }

        // ------------------------------------------------------------------ 就绪横幅

        [Test]
        public void ReadinessBannerText_就绪返回null_未就绪合并阻断说明与提示()
        {
            var ready = RunReadinessCheck.Evaluate(ReadyInput(), requireAgentDefine: true, requireBuildScenes: true);
            Assert.That(FirstRunPresenter.ReadinessBannerText(ready), Is.Null, "就绪时返回 null，UI 据此隐藏横幅");

            var input = ReadyInput();
            input.IsPlaying = true;
            input.IsCompiling = true;
            var blocked = RunReadinessCheck.Evaluate(input, requireAgentDefine: true, requireBuildScenes: true);

            var banner = FirstRunPresenter.ReadinessBannerText(blocked);
            Assert.That(banner, Does.Contain("Play Mode"));
            Assert.That(banner, Does.Contain("编译"), "非阻断提示（编译中）也进横幅");
        }

        // ------------------------------------------------------------------ 免责说明与自检前缀

        [Test]
        public void SelfCheckDisclaimer_声明只验证工具链且不代表项目全部测试通过()
        {
            var disclaimer = FirstRunPresenter.SelfCheckDisclaimer;

            Assert.That(disclaimer, Is.Not.Null.And.Not.Empty);
            Assert.That(disclaimer, Does.Contain("不代表项目"));
            Assert.That(disclaimer, Does.Contain("全部通过"));
        }

        [Test]
        public void ScopePrefix_自检suiteId_返回示例自检前缀()
        {
            Assert.That(ScenarioRunStatusPresenter.ScopePrefix(FirstRunSuiteCatalog.SelfCheckSuiteId),
                Is.EqualTo("（示例自检）"));
        }

        [Test]
        public void ScopePrefix_其他suiteId_返回空()
        {
            Assert.That(ScenarioRunStatusPresenter.ScopePrefix("echo-demo"), Is.Empty);
            Assert.That(ScenarioRunStatusPresenter.ScopePrefix(null), Is.Empty);
            Assert.That(ScenarioRunStatusPresenter.ScopePrefix(""), Is.Empty);
        }
    }
}
