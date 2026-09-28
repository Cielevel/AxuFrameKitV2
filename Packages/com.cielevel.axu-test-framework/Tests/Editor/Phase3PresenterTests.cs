using System.Collections.Generic;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Editor;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    [Category("Axu/TestFramework")]
    public class Phase3PresenterTests
    {
        [Test]
        public void NextStep_无状态但有历史报告_明确历史归属()
        {
            var text = ScenarioReportPresenter.NextStep(null, new ScenarioReport());
            Assert.That(text, Does.Contain("历史"));
            Assert.That(text, Does.Not.Contain("全部通过"));
        }

        [Test]
        public void NextStep_本轮失败_优先错误而非先出现的警告()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = (int)ScenarioExitCode.Failures
            };
            var report = new ScenarioReport
            {
                ExitCode = ScenarioExitCode.Failures,
                Results = new List<ScenarioReportResult>
                {
                    new ScenarioReportResult
                    {
                        ScenarioId = "warning.first",
                        Status = "warn",
                        Findings = new List<Finding> { new Finding(FindingSeverity.Warning, "BASELINE_DRIFT", "轻微劣化") }
                    },
                    new ScenarioReportResult
                    {
                        ScenarioId = "perf.main",
                        Status = "fail",
                        Findings = new List<Finding>
                        {
                            new Finding(FindingSeverity.Warning, "BUDGET_WARN", "接近门槛"),
                            new Finding(FindingSeverity.Error, "BUDGET_FAIL", "实际未达标")
                        }
                    }
                }
            };
            var text = ScenarioReportPresenter.NextStep(status, report);
            Assert.That(text, Does.Contain("perf.main"));
            Assert.That(text, Does.Contain("预算未达标"));
            Assert.That(text, Does.Contain("检查指标"));
        }

        [Test]
        public void NextStep_本轮无预算通过_提示结论边界()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = (int)ScenarioExitCode.Success
            };
            var report = new ScenarioReport
            {
                ExitCode = ScenarioExitCode.Success,
                Results = new List<ScenarioReportResult> { new ScenarioReportResult { Unbudgeted = true } }
            };
            Assert.That(ScenarioReportPresenter.NextStep(status, report), Does.Contain("未设预算"));
            Assert.That(ScenarioReportPresenter.BuildConclusion(report.Results[0]), Does.Contain("未设预算"));
            Assert.That(ScenarioReportPresenter.BuildConclusion(report.Results[0]), Does.Not.Contain("无异常"));
        }

        [Test]
        public void NextStep_示例自检通过_不建议配置预算也不称项目通过()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                SuiteId = FirstRunSuiteCatalog.SelfCheckSuiteId,
                ExitCode = (int)ScenarioExitCode.Success
            };
            var report = new ScenarioReport
            {
                ExitCode = ScenarioExitCode.Success,
                Results = new List<ScenarioReportResult> { new ScenarioReportResult { Unbudgeted = true } }
            };
            var text = ScenarioReportPresenter.NextStep(status, report);
            Assert.That(text, Does.Contain("测试工具链可用"));
            Assert.That(text, Does.Not.Contain("补充预算"));
            Assert.That(text, Does.Contain("不是项目通过结论"));
        }

        [Test]
        public void NextStep_warnAsFail_解释警告为何判失败()
        {
            var status = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = (int)ScenarioExitCode.Failures,
                Summary = new AxuTestFramework.Core.Report.ReportSummary { Total = 1, Warned = 1 }
            };
            var report = new ScenarioReport
            {
                ExitCode = ScenarioExitCode.Failures,
                Results = new List<ScenarioReportResult>
                {
                    new ScenarioReportResult
                    {
                        ScenarioId = "perf.warning",
                        Status = "warn",
                        Findings = new List<Finding> { new Finding(FindingSeverity.Warning, "BUDGET_WARN", "接近门槛") }
                    }
                }
            };
            var text = ScenarioReportPresenter.NextStep(status, report);
            Assert.That(text, Does.Contain("警告按失败处理"));
            Assert.That(text, Does.Contain("perf.warning"));
        }

        [Test]
        public void Summary_历史单元报告_限定类型和范围()
        {
            var report = new AxuTestReport
            {
                Mode = "editor",
                Filter = new FilterInfo { Type = "category", Value = "Axu/TestFramework" },
                Console = new ReportConsole { ErrorCount = 2 },
                Summary = new AxuTestFramework.ReportSummary { Total = 2, Passed = 2 }
            };
            var text = UtfResultPresenter.Summary(report, true);
            Assert.That(text, Does.Contain("单元测试 · EditMode"));
            Assert.That(text, Does.Contain("历史报告"));
            Assert.That(text, Does.Contain("类别 Axu/TestFramework"));
            Assert.That(text, Does.Contain("本次范围全部通过"), "预期 Console 日志不应直接覆盖用例结论");
            Assert.That(text, Does.Contain("Console 错误日志 2 条"));
            Assert.That(text, Does.Not.Contain("项目全部通过"));
        }

        [Test]
        public void IssueText_断言失败_保留原始首行并给定位建议()
        {
            var result = new ReportResult { Status = "Failed:Assertion", Message = "Expected: 3\nBut was: 2" };
            Assert.That(UtfResultPresenter.IssueText(result), Does.Contain("Expected: 3"));
            Assert.That(UtfResultPresenter.Advice(result), Does.Contain("预期值"));
        }
    }
}
