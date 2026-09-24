using System.Collections.Generic;
using System.IO;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Budget;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Editor;
using NUnit.Framework;
using ScenarioReportSummary = AxuTestFramework.Core.Report.ReportSummary;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] ScenarioReportPresenter：看板 Scenario 详情展开的纯函数
    /// （工件路径解析 / 图片判定 / 结论生成 / finding 翻译 / 色调映射，backlog #4 P0）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class ScenarioReportPresenterTests
    {
        const string ReportPath = "Library/AxuTestReports/scenario/20260920-120000-echo.json";

        // ------------------------------------------------------------------ 工件路径

        [Test]
        public void ResolveArtifactPath_相对报告目录条目_拼成完整路径()
        {
            var resolved = ScenarioReportPresenter.ResolveArtifactPath(ReportPath, "artifacts/echo/shot.png");
            var expected = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(ReportPath), "artifacts", "echo", "shot.png"));

            Assert.That(resolved, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveArtifactPath_绝对路径条目_原样返回()
        {
            var absolute = Path.GetFullPath(Path.Combine("Library", "other", "shot.png"));
            var resolved = ScenarioReportPresenter.ResolveArtifactPath(ReportPath, absolute);

            Assert.That(resolved, Is.EqualTo(absolute));
        }

        [Test]
        public void ResolveArtifactPath_空条目_返回null()
        {
            Assert.That(ScenarioReportPresenter.ResolveArtifactPath(ReportPath, null), Is.Null);
            Assert.That(ScenarioReportPresenter.ResolveArtifactPath(ReportPath, ""), Is.Null);
        }

        [Test]
        public void IsImageArtifact_png与大写JPG_为真_csv_为假()
        {
            Assert.That(ScenarioReportPresenter.IsImageArtifact("artifacts/a.png"), Is.True);
            Assert.That(ScenarioReportPresenter.IsImageArtifact("artifacts/a.JPG"), Is.True);
            Assert.That(ScenarioReportPresenter.IsImageArtifact("artifacts/a.jpeg"), Is.True);
            Assert.That(ScenarioReportPresenter.IsImageArtifact("artifacts/a.csv"), Is.False);
            Assert.That(ScenarioReportPresenter.IsImageArtifact(null), Is.False);
        }

        // ------------------------------------------------------------------ finding 翻译

        [Test]
        public void TranslateFinding_已知code_返回人话短句()
        {
            var finding = new Finding(FindingSeverity.Error, "BUDGET_FAIL", "fps.avg 低于门槛");
            Assert.That(ScenarioReportPresenter.TranslateFinding(finding), Is.EqualTo("预算未达标"));
        }

        [Test]
        public void TranslateFinding_未知code_回退原始message()
        {
            var finding = new Finding(FindingSeverity.Warning, "SOMETHING_NEW", "原始信息");
            Assert.That(ScenarioReportPresenter.TranslateFinding(finding), Is.EqualTo("原始信息"));
        }

        // ------------------------------------------------------------------ 一句话结论

        [Test]
        public void BuildConclusion_全通过且无异常_输出全部通过()
        {
            var result = new ScenarioReportResult { Status = "pass", ScenarioId = "echo.demo", Capability = "echo" };
            Assert.That(ScenarioReportPresenter.BuildConclusion(result), Is.EqualTo("结论：全部通过，无异常。"));
        }

        [Test]
        public void BuildConclusion_预算fail与错误finding_带计数与首要问题()
        {
            var result = new ScenarioReportResult
            {
                Status = "fail",
                ScenarioId = "perf.bench",
                Capability = "perf",
                Budgets = new List<BudgetOutcome>
                {
                    new BudgetOutcome { Metric = "fps.avg", Op = ">=", Threshold = 60, Actual = 50, Verdict = "fail" }
                },
                Findings = new List<Finding>
                {
                    new Finding(FindingSeverity.Error, "BUDGET_FAIL", "fps.avg 未达标")
                }
            };

            var text = ScenarioReportPresenter.BuildConclusion(result);
            Assert.That(text, Does.Contain("未通过"));
            Assert.That(text, Does.Contain("预算未达标 1 项"));
            Assert.That(text, Does.Contain("错误 1 项"));
            Assert.That(text, Does.Contain("首要问题：预算未达标"));
        }

        [Test]
        public void BuildConclusion_仅警告与基线劣化_不判未通过()
        {
            var result = new ScenarioReportResult
            {
                Status = "warn",
                ScenarioId = "soak.10m",
                Capability = "soak",
                BaselineDiff = new List<BaselineDiffEntry>
                {
                    new BaselineDiffEntry { Metric = "fps.avg", Base = 100, Current = 88, DeltaRatio = -0.12, Verdict = "warn" }
                },
                Findings = new List<Finding>
                {
                    new Finding(FindingSeverity.Warning, "BASELINE_DRIFT", "fps.avg 较基线下降 12%")
                }
            };

            var text = ScenarioReportPresenter.BuildConclusion(result);
            Assert.That(text, Does.Contain("通过但有警告"));
            Assert.That(text, Does.Contain("较基线轻微劣化 1 项"));
            Assert.That(text, Does.Contain("警告 1 项"));
        }

        // ------------------------------------------------------------------ 色调映射

        [Test]
        public void ToneClass_各判定_映射到对应色调类()
        {
            Assert.That(ScenarioReportPresenter.ToneClass("pass"), Is.EqualTo("ax-tone-ok"));
            Assert.That(ScenarioReportPresenter.ToneClass("warn"), Is.EqualTo("ax-tone-warn"));
            Assert.That(ScenarioReportPresenter.ToneClass("fail"), Is.EqualTo("ax-tone-bad"));
            Assert.That(ScenarioReportPresenter.ToneClass("error"), Is.EqualTo("ax-tone-bad"));
            Assert.That(ScenarioReportPresenter.ToneClass(null), Is.EqualTo("ax-tone-neutral"));
        }

        [Test]
        public void RunStatus_warnAsFail关闭_仅警告仍显示警告色和真实计数()
        {
            var status = CompletedStatus(exitCode: (int)ScenarioExitCode.WarningsOnly, warned: 2, failed: 0);

            Assert.That(ScenarioRunStatusPresenter.Label(status), Does.Contain("通过（有警告）"));
            Assert.That(ScenarioRunStatusPresenter.ToneClass(status), Is.EqualTo("ax-tone-warn"));
            Assert.That(ScenarioRunStatusPresenter.BadgeClass(status), Is.EqualTo("ax-badge-warn"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(status, "demo"), Does.Contain("2 警告 · 0 失败"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(status, "demo"), Does.Not.Contain("warnAsFail"));
        }

        [Test]
        public void RunStatus_warnAsFail开启且仅警告_所有结论均为失败语义并保留原因()
        {
            var status = CompletedStatus(exitCode: (int)ScenarioExitCode.Failures, warned: 2, failed: 0);

            Assert.That(ScenarioRunStatusPresenter.Label(status), Does.Contain("失败"));
            Assert.That(ScenarioRunStatusPresenter.Label(status), Does.Contain("warnAsFail"));
            Assert.That(ScenarioRunStatusPresenter.ToneClass(status), Is.EqualTo("ax-tone-bad"));
            Assert.That(ScenarioRunStatusPresenter.BadgeClass(status), Is.EqualTo("ax-badge-bad"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(status, "demo"), Does.Contain("0 fail、2 warn"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(status, "demo"), Does.Contain("因 warnAsFail 判失败"));
        }

        [Test]
        public void RunStatus_真实失败和成功_按退出码及计数映射()
        {
            var failed = CompletedStatus(exitCode: (int)ScenarioExitCode.Failures, warned: 0, failed: 1);
            var passed = CompletedStatus(exitCode: (int)ScenarioExitCode.Success, warned: 0, failed: 0);
            var failedWithoutSummary = new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = (int)ScenarioExitCode.Failures
            };

            Assert.That(ScenarioRunStatusPresenter.Label(failed), Does.Contain("有失败"));
            Assert.That(ScenarioRunStatusPresenter.ToneClass(failed), Is.EqualTo("ax-tone-bad"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(failed, "demo"), Does.Not.Contain("warnAsFail"));
            Assert.That(ScenarioRunStatusPresenter.Label(passed), Does.Contain("全部通过"));
            Assert.That(ScenarioRunStatusPresenter.ToneClass(passed), Is.EqualTo("ax-tone-ok"));
            Assert.That(ScenarioRunStatusPresenter.BadgeClass(passed), Is.EqualTo("ax-badge-ok"));
            Assert.That(ScenarioRunStatusPresenter.Label(failedWithoutSummary), Does.Contain("有失败"));
            Assert.That(ScenarioRunStatusPresenter.ToneClass(failedWithoutSummary), Is.EqualTo("ax-tone-bad"));
            Assert.That(ScenarioRunStatusPresenter.DetailText(failedWithoutSummary, "demo"), Does.Contain("运行失败"));
        }

        static AxuScenarioRunStatus CompletedStatus(int exitCode, int warned, int failed)
        {
            return new AxuScenarioRunStatus
            {
                Status = AxuScenarioRunStatus.Completed,
                ExitCode = exitCode,
                Summary = new ScenarioReportSummary { Total = 3, Passed = 3 - warned - failed, Warned = warned, Failed = failed }
            };
        }
    }
}
