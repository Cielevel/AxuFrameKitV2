using AxuTestFramework.Agent.Capabilities.LoadTime;
using AxuTestFramework.Agent.Capabilities.Smoke;
using AxuTestFramework.Agent.Metrics;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] LoadTime/Smoke 结果采集器产出键契约（Phase 3 P0，对齐已落地实现）。
    /// 直接构造采集器（不经注册表），经 MetricSession.TryAttachCollector 挂入会话后驱动
    /// BeginScene/EndScene，检查 BuildSummary 的 stat 键名与聚合值。
    /// 关键契约点：
    ///  - loadtime（collector="loadtime"）：scene.&lt;safe&gt;.loadMs/.firstFrameMs/.stableMs（Last）；
    ///    sceneCount（Sum）、loadMs.max（Max）、loadMs.avg（Avg）；unstableCount（Sum，Start 播种恒产出含 0）；
    ///  - smoke（collector="smoke"）：scene.&lt;safe&gt;.loadMs/.errors（Last）；
    ///    sceneCount（Sum）、loadMs.max（Max）、errors.total（Sum）、loadFailed.total（Sum）——
    ///    total 类聚合恒产出（Start 播种，含 0 值）；
    ///  - 键片段经 AgentPaths.SanitizeFileName 清洗（Windows 非法文件名字符替换为下划线）；
    ///  - 重复场景名自动扩展后缀 _2；未 BeginScene 的 EndScene 为 no-op（仅 total 类播种键，无场景行）；
    ///    per-scene stat 与 loadMs 聚合无采样不产出（绝不静默 0）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class ScenarioCollectorOutputTests
    {
        [TearDown]
        public void TearDown()
        {
            MetricSession.EndCurrent();
        }

        // ------------------------------------------------------------------ LoadTimeCollector

        [Test]
        public void LoadTimeCollector_单场景_EndScene后输出每场景键与聚合值()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new LoadTimeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 120, firstFrameMs: 40, stableMs: 100, stable: true);

            var loadtime = session.BuildSummary()["loadtime"];
            Assert.That(loadtime["scene.Alpha.loadMs"], Is.EqualTo(120d).Within(1e-9));
            Assert.That(loadtime["scene.Alpha.firstFrameMs"], Is.EqualTo(40d).Within(1e-9));
            Assert.That(loadtime["scene.Alpha.stableMs"], Is.EqualTo(100d).Within(1e-9));
            Assert.That(loadtime["sceneCount"], Is.EqualTo(1d).Within(1e-9));
            Assert.That(loadtime["loadMs.max"], Is.EqualTo(120d).Within(1e-9));
            Assert.That(loadtime["loadMs.avg"], Is.EqualTo(120d).Within(1e-9));
            Assert.That(loadtime["unstableCount"], Is.EqualTo(0d).Within(1e-9),
                "total 类恒产出：全部 stable 时 unstableCount 为合法 0 值（可命中预算），不再缺键。");
        }

        [Test]
        public void LoadTimeCollector_多场景_聚合取max_avg且不稳定计入unstableCount()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new LoadTimeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 100, firstFrameMs: 30, stableMs: 90, stable: true);
            collector.BeginScene("Beta");
            collector.EndScene(loadMs: 300, firstFrameMs: 60, stableMs: 60, stable: false);

            var loadtime = session.BuildSummary()["loadtime"];
            Assert.That(loadtime["sceneCount"], Is.EqualTo(2d).Within(1e-9));
            Assert.That(loadtime["loadMs.max"], Is.EqualTo(300d).Within(1e-9));
            Assert.That(loadtime["loadMs.avg"], Is.EqualTo(200d).Within(1e-9));
            Assert.That(loadtime["unstableCount"], Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void LoadTimeCollector_场景名含非法文件名字符_键片段清洗为下划线()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new LoadTimeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            // ':' 为 Windows 非法文件名字符（AgentPaths.SanitizeFileName 语义）→ 替换为 '_'
            collector.BeginScene("Demo:Scene");
            collector.EndScene(loadMs: 88, firstFrameMs: 22, stableMs: 80, stable: true);

            var loadtime = session.BuildSummary()["loadtime"];
            Assert.That(loadtime, Does.ContainKey("scene.Demo_Scene.loadMs"));
            Assert.That(loadtime["scene.Demo_Scene.loadMs"], Is.EqualTo(88d).Within(1e-9));
        }

        [Test]
        public void LoadTimeCollector_重复场景名_第二个键自动扩展后缀()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new LoadTimeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 100, firstFrameMs: 30, stableMs: 90, stable: true);
            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 200, firstFrameMs: 40, stableMs: 180, stable: true);

            var loadtime = session.BuildSummary()["loadtime"];
            Assert.That(loadtime["scene.Alpha.loadMs"], Is.EqualTo(100d).Within(1e-9));
            Assert.That(loadtime["scene.Alpha_2.loadMs"], Is.EqualTo(200d).Within(1e-9));
            Assert.That(loadtime["sceneCount"], Is.EqualTo(2d).Within(1e-9));
        }

        [Test]
        public void LoadTimeCollector_未BeginScene就EndScene_no_op仅剩total播种键()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new LoadTimeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.EndScene(loadMs: 100, firstFrameMs: 30, stableMs: 90, stable: true);

            // EndScene 为 no-op：无场景行、无 loadMs 聚合；仅 Start 播种的 total 类键恒产出（0 值）
            var loadtime = session.BuildSummary()["loadtime"];
            Assert.That(loadtime["sceneCount"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(loadtime["unstableCount"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(loadtime.Keys, Has.None.StartsWith("scene."), "未 BeginScene 不应有场景行。");
            Assert.That(loadtime, Does.Not.ContainKey("loadMs.max"), "无成功场景时 loadMs 聚合不产出（缺失=fail）。");
            Assert.That(loadtime, Does.Not.ContainKey("loadMs.avg"));
        }

        // ------------------------------------------------------------------ SmokeCollector

        [Test]
        public void SmokeCollector_混合成败_每场景键与聚合值正确()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new SmokeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 100, errorCount: 2, loaded: true);
            collector.BeginScene("Beta");
            collector.EndScene(loadMs: 50, errorCount: 0, loaded: false);

            var smoke = session.BuildSummary()["smoke"];
            Assert.That(smoke["scene.Alpha.loadMs"], Is.EqualTo(100d).Within(1e-9));
            Assert.That(smoke["scene.Alpha.errors"], Is.EqualTo(2d).Within(1e-9));
            Assert.That(smoke["scene.Beta.loadMs"], Is.EqualTo(50d).Within(1e-9));
            Assert.That(smoke["scene.Beta.errors"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(smoke["sceneCount"], Is.EqualTo(2d).Within(1e-9));
            Assert.That(smoke["loadMs.max"], Is.EqualTo(100d).Within(1e-9));
            Assert.That(smoke["errors.total"], Is.EqualTo(2d).Within(1e-9));
            Assert.That(smoke["loadFailed.total"], Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void SmokeCollector_全部加载成功_loadFailed_total产出0值()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new SmokeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.BeginScene("Alpha");
            collector.EndScene(loadMs: 100, errorCount: 0, loaded: true);

            // total 类恒产出（Start 播种，含 0 值）：全 0 是可命中预算的合法产出，不再缺键被误判 fail
            var smoke = session.BuildSummary()["smoke"];
            Assert.That(smoke["loadFailed.total"], Is.EqualTo(0d).Within(1e-9),
                "无失败场景时 loadFailed.total 为合法 0 值。");
            Assert.That(smoke["errors.total"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(smoke["sceneCount"], Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void SmokeCollector_未BeginScene就EndScene_no_op仅剩total播种键()
        {
            var session = MetricSession.Begin(null, 0);
            var collector = new SmokeCollector();
            Assert.That(session.TryAttachCollector(collector), Is.True);

            collector.EndScene(loadMs: 100, errorCount: 1, loaded: true);

            // EndScene 为 no-op：无场景行、无 loadMs 聚合；仅 Start 播种的 total 类键恒产出（0 值）
            var smoke = session.BuildSummary()["smoke"];
            Assert.That(smoke["sceneCount"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(smoke["errors.total"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(smoke["loadFailed.total"], Is.EqualTo(0d).Within(1e-9));
            Assert.That(smoke.Keys, Has.None.StartsWith("scene."), "未 BeginScene 不应有场景行。");
            Assert.That(smoke, Does.Not.ContainKey("loadMs.max"), "无场景观测时 loadMs 聚合不产出（缺失=fail）。");
        }
    }
}
