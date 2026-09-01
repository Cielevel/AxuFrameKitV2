using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Core.Trend;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] TrendSummary 趋势序列与劣化百分比（Phase 4 契约驱动测试）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - Series(points, scenario, metric)：按 scenario+metric 过滤，结果按 TimestampUtc 升序；
    ///  - ChangePercent(older, newer)：正值恒表示"变差"——
    ///      HigherIsBetter（fps.*）用 (old-new)/old*100：fps 上升得负值；
    ///      LowerIsBetter（frameTime.*/memory.*）用 (new-old)/old*100：上升得正值；
    ///      Symmetric（未知键，见 MetricDirection.Of）带符号 (new-old)/old*100。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class TrendSummaryTests
    {
        static TrendPoint Point(string timestamp, string scenario, string metric, double value) =>
            new TrendPoint
            {
                TimestampUtc = timestamp,
                Suite = "suite.perf",
                Scenario = scenario,
                Capability = "perf",
                Result = "pass",
                ExitCode = 0,
                DurationMs = 1000,
                Metric = metric,
                Value = value,
            };

        // ------------------------------------------------------------------ Series

        [Test]
        public void Series_按scenario与metric过滤_仅返回匹配行()
        {
            var points = new List<TrendPoint>
            {
                Point("2026-09-01T00:00:00Z", "perf.combat.fhd", "fps.avg", 48),
                Point("2026-09-01T00:00:00Z", "perf.combat.fhd", "frameTime.cpuP95", 22),
                Point("2026-09-01T00:00:00Z", "smoke.main", "fps.avg", 60),
                Point("2026-09-01T00:00:00Z", "perf.combat.fhd", "", 1),
            };

            var series = TrendSummary.Series(points, "perf.combat.fhd", "fps.avg").ToList();

            Assert.That(series, Has.Count.EqualTo(1));
            Assert.That(series[0].Scenario, Is.EqualTo("perf.combat.fhd"));
            Assert.That(series[0].Metric, Is.EqualTo("fps.avg"));
            Assert.That(series[0].Value, Is.EqualTo(48));
        }

        [Test]
        public void Series_输入乱序_按TimestampUtc升序返回()
        {
            var points = new List<TrendPoint>
            {
                Point("2026-09-03T00:00:00Z", "perf.combat.fhd", "fps.avg", 50),
                Point("2026-09-01T00:00:00Z", "perf.combat.fhd", "fps.avg", 48),
                Point("2026-09-02T00:00:00Z", "perf.combat.fhd", "fps.avg", 49),
            };

            var series = TrendSummary.Series(points, "perf.combat.fhd", "fps.avg").ToList();

            Assert.That(series.Select(p => p.TimestampUtc).ToList(),
                Is.EqualTo(new[]
                {
                    "2026-09-01T00:00:00Z",
                    "2026-09-02T00:00:00Z",
                    "2026-09-03T00:00:00Z",
                }));
        }

        [Test]
        public void Series_无匹配_返回空序列()
        {
            var points = new List<TrendPoint> { Point("2026-09-01T00:00:00Z", "smoke.main", "fps.avg", 60) };

            var series = TrendSummary.Series(points, "perf.combat.fhd", "fps.avg").ToList();

            Assert.That(series, Is.Empty);
        }

        // ------------------------------------------------------------------ ChangePercent 方向语义

        [Test]
        public void ChangePercent_fps上升_HigherIsBetter得负值()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "fps.avg", 40);
            var newer = Point("2026-09-02T00:00:00Z", "s", "fps.avg", 50);

            var change = TrendSummary.ChangePercent(older, newer);

            Assert.That(change, Is.Negative, "fps 上升是改善，ChangePercent 应为负。");
            Assert.That(change, Is.EqualTo(-25.0).Within(1e-9), "(40-50)/40*100 = -25%。");
        }

        [Test]
        public void ChangePercent_fps下降_HigherIsBetter得正值()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "fps.avg", 50);
            var newer = Point("2026-09-02T00:00:00Z", "s", "fps.avg", 40);

            var change = TrendSummary.ChangePercent(older, newer);

            Assert.That(change, Is.EqualTo(20.0).Within(1e-9), "(50-40)/50*100 = 20%，下降=劣化=正。");
        }

        [Test]
        public void ChangePercent_frameTime上升_LowerIsBetter得正值()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "frameTime.cpuP95", 20);
            var newer = Point("2026-09-02T00:00:00Z", "s", "frameTime.cpuP95", 25);

            var change = TrendSummary.ChangePercent(older, newer);

            Assert.That(change, Is.EqualTo(25.0).Within(1e-9), "(25-20)/20*100 = 25%，上升=劣化=正。");
        }

        [Test]
        public void ChangePercent_memory上升_LowerIsBetter得正值()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "memory.allocated.max", 1800);
            var newer = Point("2026-09-02T00:00:00Z", "s", "memory.allocated.max", 2000);

            var change = TrendSummary.ChangePercent(older, newer);

            Assert.That(change, Is.EqualTo((2000.0 - 1800) / 1800 * 100).Within(1e-9));
        }

        [Test]
        public void ChangePercent_frameTime下降_LowerIsBetter得负值()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "frameTime.cpuP95", 25);
            var newer = Point("2026-09-02T00:00:00Z", "s", "frameTime.cpuP95", 20);

            var change = TrendSummary.ChangePercent(older, newer);

            Assert.That(change, Is.EqualTo(-20.0).Within(1e-9), "(20-25)/25*100 = -20%，下降=改善=负。");
        }

        [Test]
        public void ChangePercent_未知键_Symmetric升降同幅()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "drawcall.count", 100);

            var up = TrendSummary.ChangePercent(older,
                Point("2026-09-02T00:00:00Z", "s", "drawcall.count", 120));
            var down = TrendSummary.ChangePercent(older,
                Point("2026-09-02T00:00:00Z", "s", "drawcall.count", 80));

            Assert.That(up, Is.EqualTo(20.0).Within(1e-9), "Symmetric 带符号：(120-100)/100*100 = +20%。");
            Assert.That(down, Is.EqualTo(-20.0).Within(1e-9), "Symmetric 带符号：(80-100)/100*100 = -20%。");
        }

        [Test]
        public void ChangePercent_值不变_为零()
        {
            var older = Point("2026-09-01T00:00:00Z", "s", "fps.avg", 48);
            var newer = Point("2026-09-02T00:00:00Z", "s", "fps.avg", 48);

            Assert.That(TrendSummary.ChangePercent(older, newer), Is.EqualTo(0.0).Within(1e-9));
        }
    }
}
