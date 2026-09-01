using AxuTestFramework.Agent.Capabilities.Perf;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] PerfParams 默认值 / clamp / JSON 反序列化契约（Phase 3 P0，对齐已落地实现）。
    /// 关键契约点：
    ///  - 默认：warmup 0 / measure 30 / screenshot false / scene 空串；
    ///  - ClampedWarmupSeconds clamp 0..3600；ClampedMeasureSeconds clamp 0.1..3600；
    ///  - JSON 反序列化经 AxuJson.ToObject&lt;T&gt;（与 Runner ParseParams 同一通道），缺省键回落默认值。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class PerfParamsTests
    {
        // ------------------------------------------------------------------ 默认值

        [Test]
        public void 默认构造_各字段为文档默认值()
        {
            var parameters = new PerfParams();

            Assert.That(parameters.Scene, Is.EqualTo(string.Empty));
            Assert.That(parameters.WarmupSeconds, Is.EqualTo(0f));
            Assert.That(parameters.MeasureSeconds, Is.EqualTo(30f));
            Assert.That(parameters.Screenshot, Is.False);
        }

        // ------------------------------------------------------------------ clamp

        [Test]
        public void ClampedWarmupSeconds_负数_clamp为0()
        {
            Assert.That(new PerfParams { WarmupSeconds = -5f }.ClampedWarmupSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void ClampedWarmupSeconds_超上限_clamp为3600()
        {
            Assert.That(new PerfParams { WarmupSeconds = 4000f }.ClampedWarmupSeconds, Is.EqualTo(3600f));
        }

        [Test]
        public void ClampedWarmupSeconds_合法范围_原值返回()
        {
            Assert.That(new PerfParams { WarmupSeconds = 12.5f }.ClampedWarmupSeconds, Is.EqualTo(12.5f));
        }

        [Test]
        public void ClampedMeasureSeconds_低于下限_clamp为0点1()
        {
            Assert.That(new PerfParams { MeasureSeconds = 0.05f }.ClampedMeasureSeconds, Is.EqualTo(0.1f));
        }

        [Test]
        public void ClampedMeasureSeconds_超上限_clamp为3600()
        {
            Assert.That(new PerfParams { MeasureSeconds = 99999f }.ClampedMeasureSeconds, Is.EqualTo(3600f));
        }

        // ------------------------------------------------------------------ JSON 反序列化

        [Test]
        public void ToObject_全键JSON_各字段正确还原()
        {
            var parameters = AxuJson.ToObject<PerfParams>(JObject.Parse(
                "{\"scene\":\"Assets/Scenes/Demo.unity\",\"warmupSeconds\":2.5,\"measureSeconds\":10,\"screenshot\":true}"));

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Scene, Is.EqualTo("Assets/Scenes/Demo.unity"));
            Assert.That(parameters.WarmupSeconds, Is.EqualTo(2.5f).Within(1e-6));
            Assert.That(parameters.MeasureSeconds, Is.EqualTo(10f).Within(1e-6));
            Assert.That(parameters.Screenshot, Is.True);
        }

        [Test]
        public void ToObject_空对象_回落默认值()
        {
            var parameters = AxuJson.ToObject<PerfParams>(new JObject());

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Scene, Is.EqualTo(string.Empty));
            Assert.That(parameters.WarmupSeconds, Is.EqualTo(0f));
            Assert.That(parameters.MeasureSeconds, Is.EqualTo(30f));
            Assert.That(parameters.Screenshot, Is.False);
        }
    }
}
