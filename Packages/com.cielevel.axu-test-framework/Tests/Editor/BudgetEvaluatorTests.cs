using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Core.Budget;
using AxuTestFramework.Core.Scenarios;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] BudgetEvaluator 预算判定语义（Phase 2 契约驱动测试；规则类型复用既有 BudgetRule）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - BudgetEvaluator.Evaluate(metrics, rules) → 每条规则一个 BudgetOutcome（顺序与规则一致）；
    ///  - metrics 形如 { "fps": {"avg": 50, "min": 30}, "memory": {"allocatedMax": 1024} }，
    ///    预算键为 &lt;collector&gt;.&lt;stat&gt; 两段式（Phase 2 开工决策①，stat 名与报告 metrics 内层键一致）；
    ///  - BudgetOutcome{Metric, Verdict("pass"/"warn"/"fail"), Actual, Missing}；
    ///  - 比较含等号（"&gt;=45" 时 45 判 pass）；range 含端点；
    ///  - collector 或 stat 缺失 → Missing=true 且 Verdict="fail"（绝不静默 0 值）；
    ///  - severity 仅在违约时生效：Warn 违约判 warn、达标仍判 pass。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class BudgetEvaluatorTests
    {
        static Dictionary<string, Dictionary<string, double>> MetricsOf(string collector, string stat, double value) =>
            new Dictionary<string, Dictionary<string, double>>
            {
                [collector] = new Dictionary<string, double> { [stat] = value },
            };

        static BudgetRule Rule(string metric, string op, double? value = null, double? min = null, double? max = null,
            BudgetSeverity severity = BudgetSeverity.Fail) => new BudgetRule
        {
            Metric = metric, Op = op, Value = value, ValueMin = min, ValueMax = max, Severity = severity,
        };

        static BudgetOutcome Single(Dictionary<string, Dictionary<string, double>> metrics, BudgetRule rule)
        {
            IReadOnlyList<BudgetOutcome> outcomes = BudgetEvaluator.Evaluate(metrics, new List<BudgetRule> { rule });
            Assert.That(outcomes, Has.Count.EqualTo(1), "每条规则应恰好产出一个 BudgetOutcome。");
            return outcomes[0];
        }

        // ------------------------------------------------------------------ 阈值判定

        [Test]
        public void Evaluate_fpsAvg达到下限阈值_pass()
        {
            var outcome = Single(MetricsOf("fps", "avg", 50.0), Rule("fps.avg", ">=", 45));

            Assert.That(outcome.Verdict, Is.EqualTo("pass"));
            Assert.That(outcome.Actual, Is.EqualTo(50.0).Within(1e-9));
            Assert.That(outcome.Missing, Is.False);
        }

        [Test]
        public void Evaluate_实际值等于阈值_含等号判pass()
        {
            var outcome = Single(MetricsOf("fps", "avg", 45.0), Rule("fps.avg", ">=", 45));

            Assert.That(outcome.Verdict, Is.EqualTo("pass"));
        }

        [Test]
        public void Evaluate_fpsAvg低于下限阈值_按默认severity判fail()
        {
            var outcome = Single(MetricsOf("fps", "avg", 50.0), Rule("fps.avg", ">=", 55));

            Assert.That(outcome.Verdict, Is.EqualTo("fail"));
        }

        [Test]
        public void Evaluate_违约且severity为Warn_判warn()
        {
            var outcome = Single(MetricsOf("frameTime", "cpuP95", 30.0),
                Rule("frameTime.cpuP95", "<=", 25, severity: BudgetSeverity.Warn));

            Assert.That(outcome.Verdict, Is.EqualTo("warn"));
        }

        [Test]
        public void Evaluate_warn规则达标_判pass不判warn()
        {
            var outcome = Single(MetricsOf("frameTime", "cpuP95", 20.0),
                Rule("frameTime.cpuP95", "<=", 25, severity: BudgetSeverity.Warn));

            Assert.That(outcome.Verdict, Is.EqualTo("pass"));
        }

        [Test]
        public void Evaluate_两段键memory_allocatedMax未超上限_pass()
        {
            var outcome = Single(MetricsOf("memory", "allocatedMax", 1024.0), Rule("memory.allocatedMax", "<=", 2048));

            Assert.That(outcome.Verdict, Is.EqualTo("pass"));
        }

        // ------------------------------------------------------------------ range 区间

        [Test]
        public void Evaluate_range规则值在区间内_pass()
        {
            var outcome = Single(MetricsOf("fps", "avg", 50.0), Rule("fps.avg", "range", min: 30, max: 60));

            Assert.That(outcome.Verdict, Is.EqualTo("pass"));
        }

        [Test]
        public void Evaluate_range规则值超出上限_fail()
        {
            var outcome = Single(MetricsOf("fps", "avg", 70.0), Rule("fps.avg", "range", min: 30, max: 60));

            Assert.That(outcome.Verdict, Is.EqualTo("fail"));
        }

        [Test]
        public void Evaluate_range规则值低于下限_fail()
        {
            var outcome = Single(MetricsOf("fps", "avg", 10.0), Rule("fps.avg", "range", min: 30, max: 60));

            Assert.That(outcome.Verdict, Is.EqualTo("fail"));
        }

        // ------------------------------------------------------------------ 指标缺失

        [Test]
        public void Evaluate_collector缺失_Missing为true且判fail()
        {
            // metrics 只有 memory，规则要 fps.avg → 不静默 0 值
            var metrics = new Dictionary<string, Dictionary<string, double>>
            {
                ["memory"] = new Dictionary<string, double> { ["allocatedMax"] = 1024.0 },
            };

            IReadOnlyList<BudgetOutcome> outcomes =
                BudgetEvaluator.Evaluate(metrics, new List<BudgetRule> { Rule("fps.avg", ">=", 45) });

            Assert.That(outcomes[0].Missing, Is.True);
            Assert.That(outcomes[0].Verdict, Is.EqualTo("fail"));
        }

        [Test]
        public void Evaluate_stat缺失_Missing为true且判fail()
        {
            // collector fps 在、但无 min 聚合值
            var metrics = new Dictionary<string, Dictionary<string, double>>
            {
                ["fps"] = new Dictionary<string, double> { ["avg"] = 50.0 },
            };

            IReadOnlyList<BudgetOutcome> outcomes =
                BudgetEvaluator.Evaluate(metrics, new List<BudgetRule> { Rule("fps.min", ">=", 25) });

            Assert.That(outcomes[0].Missing, Is.True);
            Assert.That(outcomes[0].Verdict, Is.EqualTo("fail"));
        }

        // ------------------------------------------------------------------ 边界

        [Test]
        public void Evaluate_空规则列表_返回空结果()
        {
            IReadOnlyList<BudgetOutcome> outcomes = BudgetEvaluator.Evaluate(
                MetricsOf("fps", "avg", 50.0), new List<BudgetRule>());

            Assert.That(outcomes, Is.Not.Null);
            Assert.That(outcomes, Has.Count.EqualTo(0));
        }

        [Test]
        public void Evaluate_空metrics_每条规则均Missing且判fail()
        {
            var rules = new List<BudgetRule> { Rule("fps.avg", ">=", 45), Rule("memory.allocatedMax", "<=", 2048) };

            IReadOnlyList<BudgetOutcome> outcomes = BudgetEvaluator.Evaluate(
                new Dictionary<string, Dictionary<string, double>>(), rules);

            Assert.That(outcomes, Has.Count.EqualTo(2));
            Assert.That(outcomes.All(o => o.Missing && o.Verdict == "fail"), Is.True);
        }
    }
}
