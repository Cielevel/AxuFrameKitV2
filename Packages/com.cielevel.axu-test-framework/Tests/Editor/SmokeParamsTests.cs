using AxuTestFramework.Agent.Capabilities.Smoke;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] SmokeParams 默认值 / 归一化 / clamp / JSON 反序列化契约（Phase 3 P0，对齐已落地实现）。
    /// 关键契约点：
    ///  - 默认：source "list" / screenshot true / stabilitySeconds 1 / stabilityFrames 10 / scenes 空清单；
    ///  - NormalizedSource = trim + 小写；list/buildsettings/directory 之外（含 null/空串）一律归 "list"；
    ///  - ClampedStabilitySeconds clamp 0..60；ClampedStabilityFrames clamp 1..1000。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class SmokeParamsTests
    {
        // ------------------------------------------------------------------ 默认值

        [Test]
        public void 默认构造_各字段为文档默认值()
        {
            var parameters = new SmokeParams();

            Assert.That(parameters.Source, Is.EqualTo("list"));
            Assert.That(parameters.Scenes, Is.Not.Null.And.Empty);
            Assert.That(parameters.DirectoryPath, Is.EqualTo(string.Empty));
            Assert.That(parameters.StabilitySeconds, Is.EqualTo(1f));
            Assert.That(parameters.StabilityFrames, Is.EqualTo(10));
            Assert.That(parameters.Screenshot, Is.True);
        }

        // ------------------------------------------------------------------ NormalizedSource

        [Test]
        public void NormalizedSource_带空白大小写混合_归一化为list()
        {
            Assert.That(new SmokeParams { Source = " List " }.NormalizedSource, Is.EqualTo("list"));
        }

        [Test]
        public void NormalizedSource_全大写_归一化为buildsettings()
        {
            Assert.That(new SmokeParams { Source = "BUILDSETTINGS" }.NormalizedSource, Is.EqualTo("buildsettings"));
        }

        [Test]
        public void NormalizedSource_带空白directory_归一化为directory()
        {
            Assert.That(new SmokeParams { Source = "  Directory\t" }.NormalizedSource, Is.EqualTo("directory"));
        }

        [Test]
        public void NormalizedSource_未知值_归list()
        {
            Assert.That(new SmokeParams { Source = "bogus" }.NormalizedSource, Is.EqualTo("list"));
        }

        [Test]
        public void NormalizedSource_null或空串_归list()
        {
            Assert.That(new SmokeParams { Source = null }.NormalizedSource, Is.EqualTo("list"));
            Assert.That(new SmokeParams { Source = "   " }.NormalizedSource, Is.EqualTo("list"));
        }

        // ------------------------------------------------------------------ clamp

        [Test]
        public void ClampedStabilitySeconds_负数_clamp为0()
        {
            Assert.That(new SmokeParams { StabilitySeconds = -1f }.ClampedStabilitySeconds, Is.EqualTo(0f));
        }

        [Test]
        public void ClampedStabilitySeconds_超上限_clamp为60()
        {
            Assert.That(new SmokeParams { StabilitySeconds = 120f }.ClampedStabilitySeconds, Is.EqualTo(60f));
        }

        [Test]
        public void ClampedStabilityFrames_低于下限_clamp为1()
        {
            Assert.That(new SmokeParams { StabilityFrames = 0 }.ClampedStabilityFrames, Is.EqualTo(1));
        }

        [Test]
        public void ClampedStabilityFrames_超上限_clamp为1000()
        {
            Assert.That(new SmokeParams { StabilityFrames = 5000 }.ClampedStabilityFrames, Is.EqualTo(1000));
        }

        // ------------------------------------------------------------------ JSON 反序列化

        [Test]
        public void ToObject_全键JSON_各字段正确还原()
        {
            var parameters = AxuJson.ToObject<SmokeParams>(JObject.Parse(
                "{\"source\":\"Directory\",\"scenes\":[\"Assets/Scenes/A.unity\"],\"directory\":\"Assets/Scenes\"," +
                "\"stabilitySeconds\":2.5,\"stabilityFrames\":20,\"screenshot\":false}"));

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Source, Is.EqualTo("Directory"));
            Assert.That(parameters.NormalizedSource, Is.EqualTo("directory"));
            Assert.That(parameters.DirectoryPath, Is.EqualTo("Assets/Scenes"));
            Assert.That(parameters.StabilitySeconds, Is.EqualTo(2.5f).Within(1e-6));
            Assert.That(parameters.StabilityFrames, Is.EqualTo(20));
            Assert.That(parameters.Screenshot, Is.False);
        }

        [Test]
        public void ToObject_缺省source键_回落list且NormalizedSource为list()
        {
            var parameters = AxuJson.ToObject<SmokeParams>(new JObject());

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Source, Is.EqualTo("list"));
            Assert.That(parameters.NormalizedSource, Is.EqualTo("list"));
            Assert.That(parameters.Screenshot, Is.True);
        }
    }
}
