using AxuTestFramework.Editor;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] <c>mode=all</c> 的双阶段语义（阶段 1：返回值必须能说明两段各自的结果）。
    /// 关键契约点：
    ///  - 续跑决策：EditMode 段**正常完成**（completed，含用例失败）才继续 PlayMode 段；
    ///    超时/错误时跳过第二段——编辑器已卡或报告流程已坏，再进 Play 只会叠加故障并掩盖第一段问题；
    ///  - 整体结论：两段各自的结果一句话说清，跳过时明确写"已跳过"且整体视为未完成；
    ///  - 单段结果描述：通过/有失败/超时/出错/进行中五种形态互不混淆。
    /// <para>注意：本文件的 <c>ReportSummary</c> 是 **UTF 报告**用的那个类型（命名空间 <c>AxuTestFramework</c>，
    /// 字段 total/passed/failed/skipped/inconclusive）——阶段模型承载的是 UTF 两段的结果；
    /// scenario 报告另有 <c>AxuTestFramework.Core.Report.ReportSummary</c>（含 warned），两者同名，勿混。</para>
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AxuTestRunPhaseTests
    {
        static AxuTestRunPhase Phase(string status, ReportSummary summary = null, string error = null)
        {
            return new AxuTestRunPhase
            {
                Mode = "editor",
                Status = status,
                Summary = summary,
                Error = error,
            };
        }

        static ReportSummary Summary(int total, int passed, int failed = 0)
        {
            return new ReportSummary { Total = total, Passed = passed, Failed = failed };
        }

        // ------------------------------------------------------------------ 续跑决策

        [Test]
        public void ShouldStartPlayModePhase_完成即续跑_含用例失败()
        {
            Assert.That(AxuTestRunPhase.ShouldStartPlayModePhase(
                Phase(AxuTestRunPhase.StatusCompleted, Summary(5, 3, 2))), Is.True,
                "用例失败是结果不是故障，PlayMode 段仍应执行");

            Assert.That(AxuTestRunPhase.ShouldStartPlayModePhase(
                Phase(AxuTestRunPhase.StatusCompleted, Summary(5, 5))), Is.True);
        }

        [Test]
        public void ShouldStartPlayModePhase_超时或出错_不续跑()
        {
            Assert.That(AxuTestRunPhase.ShouldStartPlayModePhase(
                Phase(AxuTestRunPhase.StatusTimeout, error: "等待 120000ms 超时")), Is.False);
            Assert.That(AxuTestRunPhase.ShouldStartPlayModePhase(
                Phase(AxuTestRunPhase.StatusError, error: "报告流程出错")), Is.False);
            Assert.That(AxuTestRunPhase.ShouldStartPlayModePhase(null), Is.False);
        }

        // ------------------------------------------------------------------ 整体结论

        [Test]
        public void BuildAllModeConclusion_第一段超时_明确写出跳过与未完成()
        {
            var editor = Phase(AxuTestRunPhase.StatusTimeout, error: "等待 120000ms 超时");

            var conclusion = AxuTestRunPhase.BuildAllModeConclusion(editor, playModeStarted: false);

            Assert.That(conclusion, Does.Contain("EditMode 段"));
            Assert.That(conclusion, Does.Contain("超时"));
            Assert.That(conclusion, Does.Contain("跳过"));
            Assert.That(conclusion, Does.Contain("未完成"));
        }

        [Test]
        public void BuildAllModeConclusion_第一段有失败且第二段已启动_两段结果都在()
        {
            var editor = Phase(AxuTestRunPhase.StatusCompleted, Summary(8, 7, 1));

            var conclusion = AxuTestRunPhase.BuildAllModeConclusion(editor, playModeStarted: true);

            Assert.That(conclusion, Does.Contain("有失败（7/8"));
            Assert.That(conclusion, Does.Contain("PlayMode 段已启动"));
            Assert.That(conclusion, Does.Contain("整体结论待两段都结束"),
                "PlayMode 段未结束时不得宣称整体通过");
        }

        // ------------------------------------------------------------------ 单段描述

        [Test]
        public void Describe_各终态互不混淆()
        {
            Assert.That(AxuTestRunPhase.Describe(Phase(AxuTestRunPhase.StatusCompleted, Summary(4, 4))),
                Does.Contain("全部通过（4/4）"));
            Assert.That(AxuTestRunPhase.Describe(Phase(AxuTestRunPhase.StatusCompleted, Summary(4, 2, 2))),
                Does.Contain("有失败"));
            Assert.That(AxuTestRunPhase.Describe(Phase(AxuTestRunPhase.StatusTimeout, error: "超时了")),
                Does.Contain("超时"));
            Assert.That(AxuTestRunPhase.Describe(Phase(AxuTestRunPhase.StatusError, error: "写盘失败")),
                Does.Contain("出错"));
            Assert.That(AxuTestRunPhase.Describe(Phase(AxuTestRunPhase.StatusRunning)), Is.EqualTo("进行中"));
            Assert.That(AxuTestRunPhase.Describe(null), Does.Contain("未知"));
        }

        [Test]
        public void IsTerminal_仅完成超时错误为终态()
        {
            Assert.That(Phase(AxuTestRunPhase.StatusCompleted).IsTerminal, Is.True);
            Assert.That(Phase(AxuTestRunPhase.StatusTimeout).IsTerminal, Is.True);
            Assert.That(Phase(AxuTestRunPhase.StatusError).IsTerminal, Is.True);
            Assert.That(Phase(AxuTestRunPhase.StatusRunning).IsTerminal, Is.False);
        }

        // ------------------------------------------------------------------ 状态文件 → 阶段投影

        [Test]
        public void FromStatus_完成态带报告_映射为completed且全通过判定正确()
        {
            var status = new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Completed,
                ReportPath = "C:/tmp/playmode.json",
                Summary = Summary(6, 6),
            };

            var phase = AxuTestRunPhase.FromStatus("playmode", status);

            Assert.That(phase.Mode, Is.EqualTo("playmode"));
            Assert.That(phase.Status, Is.EqualTo(AxuTestRunPhase.StatusCompleted));
            Assert.That(phase.ReportPath, Is.EqualTo("C:/tmp/playmode.json"));
            Assert.That(phase.AllPassed, Is.True);
        }

        [Test]
        public void FromStatus_错误态_带原因且非全通过()
        {
            var status = new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Error,
                Message = "报告落盘失败",
            };

            var phase = AxuTestRunPhase.FromStatus("playmode", status);

            Assert.That(phase.Status, Is.EqualTo(AxuTestRunPhase.StatusError));
            Assert.That(phase.Error, Is.EqualTo("报告落盘失败"));
            Assert.That(phase.AllPassed, Is.False);
        }

        [Test]
        public void FromOutcome_超时_保留状态与原因()
        {
            var outcome = new AxuTestRunOutcome
            {
                Success = false,
                Status = "timeout",
                Error = "等待 EditMode 运行完成超时（120000ms）。",
                Mode = "all",
            };

            var phase = AxuTestRunPhase.FromOutcome("editor", outcome);

            Assert.That(phase.Status, Is.EqualTo(AxuTestRunPhase.StatusTimeout));
            Assert.That(phase.Error, Does.Contain("超时"));
            Assert.That(phase.IsTerminal, Is.True);
        }
    }
}
