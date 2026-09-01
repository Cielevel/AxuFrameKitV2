using AxuTestFramework.Agent.Tools;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] AgentEnvironment 通用参数解析/应用/还原契约（Phase 3 P0，对齐已落地实现）。
    /// 关键契约点：
    ///  - Parse：缺键或类型不符 → 对应字段 null，绝不抛异常；resolution 为 [w,h] 数组，分量非数字整体 null；
    ///  - Apply：只应用非 null 项，Prev* 记录应用前值；seed 优先级 seedOverride &gt; params.seed &gt; 0；
    ///    越界值 clamp（timeScale 0..100、vsync 0..4）；resolution 仅 Player 应用（Editor 下跳过）；
    ///  - Restore：还原快照中非 null 项；null 快照幂等不抛。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AgentEnvironmentTests
    {
        float _originalTimeScale;
        int _originalVSync;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            _originalVSync = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            QualitySettings.vSyncCount = _originalVSync;
        }

        // ------------------------------------------------------------------ Parse

        [Test]
        public void Parse_null参数_全字段null()
        {
            var overrides = AgentEnvironmentOverrides.Parse(null);

            Assert.That(overrides, Is.Not.Null);
            Assert.That(overrides.TimeScale, Is.Null);
            Assert.That(overrides.VSync, Is.Null);
            Assert.That(overrides.TargetFrameRate, Is.Null);
            Assert.That(overrides.QualityLevel, Is.Null);
            Assert.That(overrides.Resolution, Is.Null);
            Assert.That(overrides.Seed, Is.Null);
        }

        [Test]
        public void Parse_空对象_全字段null()
        {
            var overrides = AgentEnvironmentOverrides.Parse(new JObject());

            Assert.That(overrides.TimeScale, Is.Null);
            Assert.That(overrides.VSync, Is.Null);
            Assert.That(overrides.TargetFrameRate, Is.Null);
            Assert.That(overrides.QualityLevel, Is.Null);
            Assert.That(overrides.Resolution, Is.Null);
            Assert.That(overrides.Seed, Is.Null);
        }

        [Test]
        public void Parse_合法键_逐项解析()
        {
            var overrides = AgentEnvironmentOverrides.Parse(JObject.Parse(
                "{\"timeScale\":2.5,\"vsync\":1,\"targetFrameRate\":60,\"qualityLevel\":2," +
                "\"resolution\":[1280,720],\"seed\":42}"));

            Assert.That(overrides.TimeScale, Is.EqualTo(2.5f).Within(1e-6));
            Assert.That(overrides.VSync, Is.EqualTo(1));
            Assert.That(overrides.TargetFrameRate, Is.EqualTo(60));
            Assert.That(overrides.QualityLevel, Is.EqualTo(2));
            Assert.That(overrides.Resolution, Is.EqualTo(new[] { 1280, 720 }));
            Assert.That(overrides.Seed, Is.EqualTo(42));
        }

        [Test]
        public void Parse_resolution单元素或非数组_整体null()
        {
            Assert.That(
                AgentEnvironmentOverrides.Parse(JObject.Parse("{\"resolution\":[1280]}")).Resolution,
                Is.Null);
            Assert.That(
                AgentEnvironmentOverrides.Parse(JObject.Parse("{\"resolution\":\"1280x720\"}")).Resolution,
                Is.Null);
        }

        [Test]
        public void Parse_resolution分量非数字_整体null()
        {
            var overrides = AgentEnvironmentOverrides.Parse(
                JObject.Parse("{\"resolution\":[1280,\"abc\"]}"));

            Assert.That(overrides.Resolution, Is.Null);
        }

        [Test]
        public void Parse_类型不符字符串_对应字段null且不抛异常()
        {
            var overrides = AgentEnvironmentOverrides.Parse(JObject.Parse(
                "{\"timeScale\":\"abc\",\"vsync\":\"x\",\"targetFrameRate\":true,\"seed\":\"abc\"}"));

            Assert.That(overrides.TimeScale, Is.Null);
            Assert.That(overrides.VSync, Is.Null);
            Assert.That(overrides.TargetFrameRate, Is.Null);
            Assert.That(overrides.Seed, Is.Null);
        }

        // ------------------------------------------------------------------ Apply

        [Test]
        public void Apply_空参数_快照Prev全null且AppliedSeed缺省0()
        {
            var snapshot = AgentEnvironment.Apply(new JObject(), null, null);

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.PrevTimeScale, Is.Null);
            Assert.That(snapshot.PrevVSync, Is.Null);
            Assert.That(snapshot.PrevTargetFrameRate, Is.Null);
            Assert.That(snapshot.PrevQualityLevel, Is.Null);
            Assert.That(snapshot.AppliedSeed, Is.EqualTo(0));
        }

        [Test]
        public void Apply_seed优先级_seedOverride优先于paramsSeed()
        {
            Assert.That(
                AgentEnvironment.Apply(JObject.Parse("{\"seed\":5}"), 7, null).AppliedSeed,
                Is.EqualTo(7));
            Assert.That(
                AgentEnvironment.Apply(JObject.Parse("{\"seed\":5}"), null, null).AppliedSeed,
                Is.EqualTo(5));
        }

        [Test]
        public void Apply_timeScale_写入且快照记录应用前值_Restore还原()
        {
            var snapshot = AgentEnvironment.Apply(JObject.Parse("{\"timeScale\":2.5}"), null, null);

            Assert.That(Time.timeScale, Is.EqualTo(2.5f).Within(1e-6));
            Assert.That(snapshot.PrevTimeScale, Is.EqualTo(_originalTimeScale));

            AgentEnvironment.Restore(snapshot, null);

            Assert.That(Time.timeScale, Is.EqualTo(_originalTimeScale).Within(1e-6));
        }

        [Test]
        public void Apply_vsync越界_clamp为4且Restore还原()
        {
            var snapshot = AgentEnvironment.Apply(JObject.Parse("{\"vsync\":9}"), null, null);

            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(4));
            Assert.That(snapshot.PrevVSync, Is.EqualTo(_originalVSync));

            AgentEnvironment.Restore(snapshot, null);

            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(_originalVSync));
        }

        [Test]
        public void Apply_类型不符timeScale_不应用且快照Prev为null()
        {
            var snapshot = AgentEnvironment.Apply(JObject.Parse("{\"timeScale\":\"abc\"}"), null, null);

            Assert.That(Time.timeScale, Is.EqualTo(_originalTimeScale).Within(1e-6));
            Assert.That(snapshot.PrevTimeScale, Is.Null);
        }

        [Test]
        public void Apply_resolution在Editor下_跳过应用且不抛异常()
        {
            var snapshot = AgentEnvironment.Apply(JObject.Parse("{\"resolution\":[128,72]}"), null, null);

            // resolution 不进快照（Editor 本就不应用）；应用/还原全过程不抛异常即契约达成
            Assert.That(snapshot.PrevTimeScale, Is.Null);
            Assert.That(snapshot.PrevVSync, Is.Null);
            Assert.DoesNotThrow(() => AgentEnvironment.Restore(snapshot, null));
        }

        // ------------------------------------------------------------------ Restore

        [Test]
        public void Restore_null快照_幂等不抛()
        {
            Assert.DoesNotThrow(() => AgentEnvironment.Restore(null, null));
        }
    }
}
