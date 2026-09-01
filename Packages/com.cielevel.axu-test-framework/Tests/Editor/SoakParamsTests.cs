using AxuTestFramework.Agent.Capabilities.Soak;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] SoakParams 默认值 / clamp / JSON 反序列化契约（Phase 3 P1）。
    /// 关键契约点：
    ///  - 默认：duration 60min / sampleInterval 30s / gcCollect true / flush 5min；
    ///  - ClampedDurationMinutes clamp 0.1..1440；ClampedSampleIntervalSec clamp 1..600；
    ///    ClampedFlushEveryMinutes ≤0 回落默认 5、上限不超过 duration；
    ///  - JSON 反序列化经 AxuJson.ToObject&lt;T&gt;（与 Runner ParseParams 同一通道）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class SoakParamsTests
    {
        // ------------------------------------------------------------------ 默认值

        [Test]
        public void 默认构造_各字段为文档默认值()
        {
            var parameters = new SoakParams();

            Assert.That(parameters.DurationMinutes, Is.EqualTo(60f));
            Assert.That(parameters.SampleIntervalSec, Is.EqualTo(30f));
            Assert.That(parameters.GcCollectBeforeSample, Is.True);
            Assert.That(parameters.ScreenshotEveryMinutes, Is.EqualTo(0f));
            Assert.That(parameters.LeakSlopeMbPerMin, Is.EqualTo(5.0).Within(1e-9));
            Assert.That(parameters.FpsDecayPercent, Is.EqualTo(15f));
            Assert.That(parameters.FlushEveryMinutes, Is.EqualTo(5f));
        }

        // ------------------------------------------------------------------ clamp：durationMinutes

        [Test]
        public void ClampedDurationMinutes_低于下限_clamp为0点1()
        {
            Assert.That(new SoakParams { DurationMinutes = 0f }.ClampedDurationMinutes, Is.EqualTo(0.1f));
        }

        [Test]
        public void ClampedDurationMinutes_超上限_clamp为1440()
        {
            Assert.That(new SoakParams { DurationMinutes = 10000f }.ClampedDurationMinutes, Is.EqualTo(1440f));
        }

        // ------------------------------------------------------------------ clamp：sampleIntervalSec

        [Test]
        public void ClampedSampleIntervalSec_低于下限_clamp为1()
        {
            Assert.That(new SoakParams { SampleIntervalSec = 0f }.ClampedSampleIntervalSec, Is.EqualTo(1f));
        }

        [Test]
        public void ClampedSampleIntervalSec_超上限_clamp为600()
        {
            Assert.That(new SoakParams { SampleIntervalSec = 9999f }.ClampedSampleIntervalSec, Is.EqualTo(600f));
        }

        // ------------------------------------------------------------------ clamp：flushEveryMinutes

        [Test]
        public void ClampedFlushEveryMinutes_负数或0_回落默认5()
        {
            Assert.That(new SoakParams { FlushEveryMinutes = -1f }.ClampedFlushEveryMinutes, Is.EqualTo(5f));
            Assert.That(new SoakParams { FlushEveryMinutes = 0f }.ClampedFlushEveryMinutes, Is.EqualTo(5f));
        }

        [Test]
        public void ClampedFlushEveryMinutes_超短时长_下限保护为0点5()
        {
            // duration clamp 到 0.1 时上限为 max(0.5, 0.1) = 0.5，避免 flush 区间非法
            var parameters = new SoakParams { DurationMinutes = 0.05f, FlushEveryMinutes = 10f };

            Assert.That(parameters.ClampedFlushEveryMinutes, Is.EqualTo(0.5f));
        }

        [Test]
        public void ClampedFlushEveryMinutes_超长flush_clamp到时长上限()
        {
            var parameters = new SoakParams { DurationMinutes = 30f, FlushEveryMinutes = 999f };

            Assert.That(parameters.ClampedFlushEveryMinutes, Is.EqualTo(30f));
        }

        // ------------------------------------------------------------------ clamp：screenshotEveryMinutes

        [Test]
        public void ClampedScreenshotEveryMinutes_负数或0_返回0不截图()
        {
            Assert.That(new SoakParams { ScreenshotEveryMinutes = -3f }.ClampedScreenshotEveryMinutes, Is.EqualTo(0f));
            Assert.That(new SoakParams { ScreenshotEveryMinutes = 0f }.ClampedScreenshotEveryMinutes, Is.EqualTo(0f));
        }

        [Test]
        public void ClampedScreenshotEveryMinutes_超时长_clamp到时长上限()
        {
            var parameters = new SoakParams { DurationMinutes = 10f, ScreenshotEveryMinutes = 999f };

            Assert.That(parameters.ClampedScreenshotEveryMinutes, Is.EqualTo(10f));
        }

        // ------------------------------------------------------------------ JSON 反序列化

        [Test]
        public void ToObject_全键JSON_各字段正确还原()
        {
            var parameters = AxuJson.ToObject<SoakParams>(JObject.Parse(
                "{\"durationMinutes\":120,\"sampleIntervalSec\":15,\"gcCollectBeforeSample\":false," +
                "\"screenshotEveryMinutes\":10,\"leakSlopeMBPerMin\":2.5,\"fpsDecayPercent\":25,\"flushEveryMinutes\":3}"));

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.DurationMinutes, Is.EqualTo(120f).Within(1e-6));
            Assert.That(parameters.SampleIntervalSec, Is.EqualTo(15f).Within(1e-6));
            Assert.That(parameters.GcCollectBeforeSample, Is.False);
            Assert.That(parameters.ScreenshotEveryMinutes, Is.EqualTo(10f).Within(1e-6));
            Assert.That(parameters.LeakSlopeMbPerMin, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(parameters.FpsDecayPercent, Is.EqualTo(25f).Within(1e-6));
            Assert.That(parameters.FlushEveryMinutes, Is.EqualTo(3f).Within(1e-6));
        }

        [Test]
        public void ToObject_空对象_回落默认值()
        {
            var parameters = AxuJson.ToObject<SoakParams>(new JObject());

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.DurationMinutes, Is.EqualTo(60f));
            Assert.That(parameters.SampleIntervalSec, Is.EqualTo(30f));
            Assert.That(parameters.GcCollectBeforeSample, Is.True);
            Assert.That(parameters.FlushEveryMinutes, Is.EqualTo(5f));
        }
    }
}
