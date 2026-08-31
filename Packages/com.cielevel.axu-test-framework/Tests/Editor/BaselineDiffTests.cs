using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Core.Baseline;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] BaselineDiff 基线对比分级语义（Phase 2；方向感知版）。
    /// 被测类型：BaselineDiff.Compare(baseline, current) → BaselineDiffEntry
    /// {Metric, Base, Current, DeltaRatio, Verdict("pass"/"warn"/"fail")}。
    /// 关键契约点：
    ///  - DeltaRatio = (current - base) / base，正=升、负=降（仅记录，分级看方向敏感度）；
    ///  - 方向语义由 MetricDirection.Of(key) 决定：fps=HigherIsBetter（仅下降算劣化）、
    ///    frameTime/memory=LowerIsBetter（仅上升算劣化）、未知 collector=Symmetric（升降都算）；
    ///  - 劣化幅度 ≤0.10 → pass；≤0.25 → warn；&gt;0.25 → fail（端点归低档）；改善方向一律 pass；
    ///  - base == 0 时退化为绝对差分支（劣化方向的绝对差，同一阈值）；
    ///  - 基线有而 current 没有的键：忽略；双方均空 → 空结果。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class BaselineDiffTests
    {
        static BaselineDiffEntry FindEntry(IReadOnlyList<BaselineDiffEntry> entries, string metric)
        {
            var match = entries.FirstOrDefault(e => e.Metric == metric);
            Assert.That(match, Is.Not.Null, $"缺少指标 \"{metric}\" 的对比条目。");
            return match;
        }

        // ------------------------------------------------------------------ HigherIsBetter（fps）

        [Test]
        public void Compare_fps下降超过25百分比_fail()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["fps.avg"] = 100 },
                new Dictionary<string, double> { ["fps.avg"] = 70 });

            Assert.That(FindEntry(entries, "fps.avg").Verdict, Is.EqualTo("fail"));
        }

        [Test]
        public void Compare_fps下降在10到25之间_warn()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["fps.avg"] = 100 },
                new Dictionary<string, double> { ["fps.avg"] = 80 });

            var entry = FindEntry(entries, "fps.avg");
            Assert.That(entry.Verdict, Is.EqualTo("warn"));
            Assert.That(entry.DeltaRatio, Is.EqualTo(-0.20).Within(1e-9));
        }

        [Test]
        public void Compare_fps上升无论幅度_一律pass_改善不算回归()
        {
            IReadOnlyList<BaselineDiffEntry> entries;

            // 升 20%（在 warn 区间内，但 fps 上升=改善）
            entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["fps.avg"] = 100 },
                new Dictionary<string, double> { ["fps.avg"] = 120 });
            Assert.That(FindEntry(entries, "fps.avg").Verdict, Is.EqualTo("pass"));

            // 升 2.5 倍（对称口径下必 fail 的改善）
            entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["fps.min"] = 100 },
                new Dictionary<string, double> { ["fps.min"] = 350 });
            Assert.That(FindEntry(entries, "fps.min").Verdict, Is.EqualTo("pass"));
        }

        [Test]
        public void Compare_fps下降恰为10百分比_端点归pass()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["fps.avg"] = 100 },
                new Dictionary<string, double> { ["fps.avg"] = 90 });

            Assert.That(FindEntry(entries, "fps.avg").Verdict, Is.EqualTo("pass"));
        }

        // ------------------------------------------------------------------ LowerIsBetter（frameTime）

        [Test]
        public void Compare_frameTime上升超过25百分比_fail()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 10 },
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 14 });

            Assert.That(FindEntry(entries, "frameTime.cpuAvg").Verdict, Is.EqualTo("fail"));
        }

        [Test]
        public void Compare_frameTime下降无论幅度_一律pass()
        {
            IReadOnlyList<BaselineDiffEntry> entries;

            entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 10 },
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 14 });
            Assert.That(FindEntry(entries, "frameTime.cpuAvg").Verdict, Is.EqualTo("fail")); // 先证上升(+40%)确实判 fail

            entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 40 },
                new Dictionary<string, double> { ["frameTime.cpuAvg"] = 1.5 });
            Assert.That(FindEntry(entries, "frameTime.cpuAvg").Verdict, Is.EqualTo("pass")); // 大幅改善
        }

        // ------------------------------------------------------------------ Symmetric（未知 collector）

        [Test]
        public void Compare_未知collector_升降两侧对称分级()
        {
            var baseline = new Dictionary<string, double> { ["unknown.metric"] = 100 };
            IReadOnlyList<BaselineDiffEntry> entries;

            entries = BaselineDiff.Compare(baseline, new Dictionary<string, double> { ["unknown.metric"] = 120 });
            Assert.That(FindEntry(entries, "unknown.metric").Verdict, Is.EqualTo("warn"));

            entries = BaselineDiff.Compare(baseline, new Dictionary<string, double> { ["unknown.metric"] = 130 });
            Assert.That(FindEntry(entries, "unknown.metric").Verdict, Is.EqualTo("fail"));

            entries = BaselineDiff.Compare(baseline, new Dictionary<string, double> { ["unknown.metric"] = 70 });
            Assert.That(FindEntry(entries, "unknown.metric").Verdict, Is.EqualTo("fail"));
        }

        // ------------------------------------------------------------------ base=0 绝对差分支

        [Test]
        public void Compare_基线为0且绝对差小_pass()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["gc.count"] = 0 },
                new Dictionary<string, double> { ["gc.count"] = 0.05 });

            Assert.That(FindEntry(entries, "gc.count").Verdict, Is.EqualTo("pass"));
        }

        [Test]
        public void Compare_基线为0且绝对差大_fail()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double> { ["gc.count"] = 0 },
                new Dictionary<string, double> { ["gc.count"] = 1.0 });

            Assert.That(FindEntry(entries, "gc.count").Verdict, Is.EqualTo("fail"));
        }

        // ------------------------------------------------------------------ 键集合与空边界

        [Test]
        public void Compare_基线独有键被忽略_不产生条目()
        {
            var baseline = new Dictionary<string, double> { ["fps.avg"] = 48, ["gone.metric"] = 10 };
            var current = new Dictionary<string, double> { ["fps.avg"] = 48 };

            var entries = BaselineDiff.Compare(baseline, current);

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Metric, Is.EqualTo("fps.avg"));
        }

        [Test]
        public void Compare_双方均为空字典_返回空结果()
        {
            var entries = BaselineDiff.Compare(
                new Dictionary<string, double>(), new Dictionary<string, double>());

            Assert.That(entries, Is.Not.Null);
            Assert.That(entries, Has.Count.EqualTo(0));
        }
    }
}
