using System.Collections.Generic;
using AxuTestFramework.Agent.Capabilities.LoadTime;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] LoadTimeParams 默认值 / clamp / JSON 反序列化契约（Phase 3 P0，对齐已落地实现）。
    /// 关键契约点：
    ///  - 默认：scenes 空清单（非 null）/ targetFps 30 / stableFrames 30 / stabilityTimeoutSeconds 30；
    ///  - ClampedTargetFps clamp 1..1000；ClampedStableFrames clamp 1..10000；
    ///    ClampedStabilityTimeoutSeconds clamp 1..600；
    ///  - JSON 反序列化经 AxuJson.ToObject&lt;T&gt;（与 Runner ParseParams 同一通道），缺省键回落默认值。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class LoadTimeParamsTests
    {
        // ------------------------------------------------------------------ 默认值

        [Test]
        public void 默认构造_各字段为文档默认值()
        {
            var parameters = new LoadTimeParams();

            Assert.That(parameters.Scenes, Is.Not.Null);
            Assert.That(parameters.Scenes, Is.Empty);
            Assert.That(parameters.TargetFps, Is.EqualTo(30f));
            Assert.That(parameters.StableFrames, Is.EqualTo(30));
            Assert.That(parameters.StabilityTimeoutSeconds, Is.EqualTo(30f));
        }

        // ------------------------------------------------------------------ clamp

        [Test]
        public void ClampedTargetFps_低于下限_clamp为1()
        {
            Assert.That(new LoadTimeParams { TargetFps = 0f }.ClampedTargetFps, Is.EqualTo(1f));
            Assert.That(new LoadTimeParams { TargetFps = -30f }.ClampedTargetFps, Is.EqualTo(1f));
        }

        [Test]
        public void ClampedTargetFps_超上限_clamp为1000()
        {
            Assert.That(new LoadTimeParams { TargetFps = 5000f }.ClampedTargetFps, Is.EqualTo(1000f));
        }

        [Test]
        public void ClampedStableFrames_低于下限_clamp为1()
        {
            Assert.That(new LoadTimeParams { StableFrames = 0 }.ClampedStableFrames, Is.EqualTo(1));
            Assert.That(new LoadTimeParams { StableFrames = -5 }.ClampedStableFrames, Is.EqualTo(1));
        }

        [Test]
        public void ClampedStableFrames_超上限_clamp为10000()
        {
            Assert.That(new LoadTimeParams { StableFrames = 99999 }.ClampedStableFrames, Is.EqualTo(10000));
        }

        [Test]
        public void ClampedStabilityTimeoutSeconds_低于下限_clamp为1()
        {
            Assert.That(new LoadTimeParams { StabilityTimeoutSeconds = 0f }.ClampedStabilityTimeoutSeconds, Is.EqualTo(1f));
        }

        [Test]
        public void ClampedStabilityTimeoutSeconds_超上限_clamp为600()
        {
            Assert.That(new LoadTimeParams { StabilityTimeoutSeconds = 9999f }.ClampedStabilityTimeoutSeconds, Is.EqualTo(600f));
        }

        // ------------------------------------------------------------------ JSON 反序列化

        [Test]
        public void ToObject_全键JSON_各字段正确还原()
        {
            var parameters = AxuJson.ToObject<LoadTimeParams>(JObject.Parse(
                "{\"scenes\":[\"Assets/Scenes/A.unity\",\"Assets/Scenes/B.unity\"],\"targetFps\":60," +
                "\"stableFrames\":45,\"stabilityTimeoutSeconds\":15}"));

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Scenes, Is.EqualTo(new List<string> { "Assets/Scenes/A.unity", "Assets/Scenes/B.unity" }));
            Assert.That(parameters.TargetFps, Is.EqualTo(60f).Within(1e-6));
            Assert.That(parameters.StableFrames, Is.EqualTo(45));
            Assert.That(parameters.StabilityTimeoutSeconds, Is.EqualTo(15f).Within(1e-6));
        }

        [Test]
        public void ToObject_空对象_回落默认值且scenes为空清单()
        {
            var parameters = AxuJson.ToObject<LoadTimeParams>(new JObject());

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Scenes, Is.Not.Null.And.Empty);
            Assert.That(parameters.TargetFps, Is.EqualTo(30f));
            Assert.That(parameters.StableFrames, Is.EqualTo(30));
            Assert.That(parameters.StabilityTimeoutSeconds, Is.EqualTo(30f));
        }
    }
}
