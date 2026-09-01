using AxuTestFramework.Agent.Collectors;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Scenarios;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] MetricSession 挂起语义 / custom 通道 / TryAttachCollector 契约（Phase 3 P0，对齐已落地实现）。
    /// 关键契约点：
    ///  - Suspend 引用计数：挂起期 Record/RecordCounter/RecordGauge/BeginScope 全部 no-op（BuildSummary 不含对应键）；
    ///    嵌套 Suspend 两次需 Resume 两次才恢复；未挂起时 Resume 为 no-op（计数不减为负）；
    ///  - custom 内建通道：counter.&lt;key&gt;（Sum）/ gauge.&lt;key&gt;.avg/.max / scope.&lt;name&gt;.avg/.p95/.max；
    ///  - 无采集器声明的序列不产出（Record 到未知 collector 键不出现在 summary）；
    ///  - TryAttachCollector：成功返回 true 并立即 Start、Output 纳入 BuildSummary；id 重复返回 false；null/空 id 返回 false。
    /// 注意：Current 为静态，SetUp/TearDown 均调 EndCurrent() 兜底，防污染其他测试。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class MetricSessionTests
    {
        /// <summary>最小 stub 采集器：固定单 stat 输出声明（Avg），容忍 null context。</summary>
        sealed class StubCollector : MetricCollector
        {
            readonly string _stat;

            public StubCollector(string id, string stat)
            {
                Id = id;
                _stat = stat;
            }

            public override string Id { get; }

            public override CollectorOutput Output =>
                new CollectorOutput(new CollectorStatOutput(_stat, MetricAggregator.Avg));

            public bool StartCalled { get; private set; }

            public override void Start(ScenarioContext context, MetricSession session) => StartCalled = true;
        }

        [SetUp]
        public void SetUp()
        {
            MetricSession.EndCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            MetricSession.EndCurrent();
        }

        // ------------------------------------------------------------------ Record（采集器通道）

        [Test]
        public void Record_经附加采集器声明_纳入BuildSummary()
        {
            var session = MetricSession.Begin(null, 0);
            var stub = new StubCollector("stub", "s");
            Assert.That(session.TryAttachCollector(stub), Is.True);

            session.Record("stub", "s", 42);

            var summary = session.BuildSummary();
            Assert.That(summary, Does.ContainKey("stub"));
            Assert.That(summary["stub"]["s"], Is.EqualTo(42d).Within(1e-9));
        }

        [Test]
        public void Record_未知collector键_BuildSummary不产出()
        {
            var session = MetricSession.Begin(null, 0);

            session.Record("ghost", "s", 1);

            Assert.That(session.BuildSummary(), Does.Not.ContainKey("ghost"));
        }

        // ------------------------------------------------------------------ Suspend/Resume（引用计数）

        [Test]
        public void Suspend期间Record_RecordCounter_RecordGauge全部no_op_BuildSummary不含对应键()
        {
            var session = MetricSession.Begin(null, 0);
            var stub = new StubCollector("stub", "s");
            Assert.That(session.TryAttachCollector(stub), Is.True);
            session.Suspend();

            session.Record("stub", "s", 1);
            session.RecordCounter("hits", 1);
            session.RecordGauge("queue", 7);

            var summary = session.BuildSummary();
            Assert.That(summary, Does.Not.ContainKey("stub"));
            Assert.That(summary, Does.Not.ContainKey(MetricSession.CustomCollectorId));
        }

        [Test]
        public void Resume后再Record_对应键出现在summary()
        {
            var session = MetricSession.Begin(null, 0);
            var stub = new StubCollector("stub", "s");
            Assert.That(session.TryAttachCollector(stub), Is.True);
            session.Suspend();
            session.Resume();

            session.Record("stub", "s", 3);
            session.RecordCounter("hits", 2);

            var summary = session.BuildSummary();
            Assert.That(summary["stub"]["s"], Is.EqualTo(3d).Within(1e-9));
            Assert.That(summary["custom"]["counter.hits"], Is.EqualTo(2d).Within(1e-9));
        }

        [Test]
        public void Suspend嵌套两次_需Resume两次才恢复()
        {
            var session = MetricSession.Begin(null, 0);
            session.Suspend();
            session.Suspend();

            session.Resume();
            session.RecordCounter("hits", 1);
            Assert.That(session.BuildSummary(), Does.Not.ContainKey(MetricSession.CustomCollectorId),
                "第一次 Resume 后仍处挂起态（引用计数未清零）。");

            session.Resume();
            session.RecordCounter("hits", 1);
            Assert.That(session.BuildSummary()["custom"]["counter.hits"], Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void Resume_未挂起时调用_no_op且后续挂起恢复语义正确()
        {
            var session = MetricSession.Begin(null, 0);

            Assert.DoesNotThrow(() =>
            {
                session.Resume();
                session.Resume();
            });

            // 多余的 Resume 不能把计数减为负：随后一次 Suspend 仍须真正挂起，一次 Resume 即恢复
            session.Suspend();
            session.RecordCounter("hits", 1);
            Assert.That(session.BuildSummary(), Does.Not.ContainKey(MetricSession.CustomCollectorId));

            session.Resume();
            session.RecordCounter("hits", 1);
            Assert.That(session.BuildSummary()["custom"]["counter.hits"], Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void Suspend期间BeginScope_返回空作用域且Dispose不落序列()
        {
            var session = MetricSession.Begin(null, 0);
            session.Suspend();

            using (session.BeginScope("op"))
            {
            }

            Assert.That(session.BuildSummary(), Does.Not.ContainKey(MetricSession.CustomCollectorId));
        }

        [Test]
        public void BeginScope_未挂起_Dispose后scope键按avg_p95_max产出()
        {
            var session = MetricSession.Begin(null, 0);

            using (session.BeginScope("op"))
            {
                // EditMode 下无帧推进，作用域耗时近似 0ms；键存在性即契约
            }

            var custom = session.BuildSummary()[MetricSession.CustomCollectorId];
            Assert.That(custom, Does.ContainKey("scope.op.avg"));
            Assert.That(custom, Does.ContainKey("scope.op.p95"));
            Assert.That(custom, Does.ContainKey("scope.op.max"));
        }

        // ------------------------------------------------------------------ custom 通道聚合

        [Test]
        public void RecordGauge_counter累计与gauge聚合_按声明键产出()
        {
            var session = MetricSession.Begin(null, 0);

            session.RecordCounter("hits", 2);
            session.RecordCounter("hits", 3);
            session.RecordGauge("queue", 1);
            session.RecordGauge("queue", 9);

            var custom = session.BuildSummary()[MetricSession.CustomCollectorId];
            Assert.That(custom["counter.hits"], Is.EqualTo(5d).Within(1e-9));
            Assert.That(custom["gauge.queue.avg"], Is.EqualTo(5d).Within(1e-9));
            Assert.That(custom["gauge.queue.max"], Is.EqualTo(9d).Within(1e-9));
        }

        // ------------------------------------------------------------------ TryAttachCollector

        [Test]
        public void TryAttachCollector_新id_返回true并立即Start且Output纳入summary()
        {
            var session = MetricSession.Begin(null, 0);
            var stub = new StubCollector("stub", "s");

            var attached = session.TryAttachCollector(stub);
            session.Record("stub", "s", 7);

            Assert.That(attached, Is.True);
            Assert.That(stub.StartCalled, Is.True, "附加成功时应对采集器立即调 Start。");
            Assert.That(session.BuildSummary()["stub"]["s"], Is.EqualTo(7d).Within(1e-9));
        }

        [Test]
        public void TryAttachCollector_重复id_返回false()
        {
            var session = MetricSession.Begin(null, 0);

            Assert.That(session.TryAttachCollector(new StubCollector("dup", "s")), Is.True);
            Assert.That(session.TryAttachCollector(new StubCollector("dup", "s2")), Is.False);
        }

        [Test]
        public void TryAttachCollector_null或空id_返回false()
        {
            var session = MetricSession.Begin(null, 0);

            Assert.That(session.TryAttachCollector(null), Is.False);
            Assert.That(session.TryAttachCollector(new StubCollector("", "s")), Is.False);
        }

        // ------------------------------------------------------------------ Begin/EndCurrent 静态语义

        [Test]
        public void Begin_隐式结束上一会话_Current指向新会话()
        {
            var first = MetricSession.Begin(null, 0);
            var second = MetricSession.Begin(null, 0);

            Assert.That(MetricSession.Current, Is.SameAs(second));
            Assert.That(MetricSession.Current, Is.Not.SameAs(first));
        }

        [Test]
        public void EndCurrent_Current清null()
        {
            MetricSession.Begin(null, 0);

            MetricSession.EndCurrent();

            Assert.That(MetricSession.Current, Is.Null);
        }
    }
}
