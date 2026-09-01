using AxuTestFramework.Agent.Capabilities.Monkey;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] MonkeyParams 默认值 / region clamp / 时长与频率 clamp 契约（Phase 3 P1）。
    /// 关键契约点：
    ///  - 默认：Region=null 表示全屏、duration 60s、actionsPerSecond 2、stopOnError true；
    ///  - MonkeyRegion.Clamped 把 x/y/w/h 收进 [0,1]，w/h 下限 0.01，x+w/y+h 不越 [0,1]；
    ///  - ClampedActionsPerSecond clamp 0.1..30；ClampedDurationSec clamp 0.1..7200。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class MonkeyParamsTests
    {
        // ------------------------------------------------------------------ 默认值

        [Test]
        public void 默认构造_各字段为文档默认值()
        {
            var parameters = new MonkeyParams();

            Assert.That(parameters.Scene, Is.EqualTo(string.Empty));
            Assert.That(parameters.DurationSec, Is.EqualTo(60f));
            Assert.That(parameters.ActionsPerSecond, Is.EqualTo(2f));
            Assert.That(parameters.Seed, Is.EqualTo(0));
            Assert.That(parameters.Region, Is.Null);
            Assert.That(parameters.StopOnError, Is.True);
            Assert.That(parameters.HangThresholdSec, Is.EqualTo(5f));
        }

        [Test]
        public void 默认Region_构造即全屏()
        {
            var region = new MonkeyRegion();

            Assert.That(region.X, Is.EqualTo(0f));
            Assert.That(region.Y, Is.EqualTo(0f));
            Assert.That(region.W, Is.EqualTo(1f));
            Assert.That(region.H, Is.EqualTo(1f));
            Assert.That(region.Clamped, Is.EqualTo((0f, 0f, 1f, 1f)));
        }

        // ------------------------------------------------------------------ region clamp

        [Test]
        public void RegionClamped_坐标越界_clamp到0到1()
        {
            var region = new MonkeyRegion { X = -0.5f, Y = 1.5f, W = 0.2f, H = 0.2f };

            var clamped = region.Clamped;

            Assert.That(clamped.X, Is.EqualTo(0f));
            Assert.That(clamped.Y, Is.EqualTo(0.8f).Within(1e-6));
            Assert.That(clamped.W, Is.EqualTo(0.2f).Within(1e-6));
            Assert.That(clamped.H, Is.EqualTo(0.2f).Within(1e-6));
        }

        [Test]
        public void RegionClamped_宽高为0_clamp到最小0点01()
        {
            var region = new MonkeyRegion { X = 0.5f, Y = 0.5f, W = 0f, H = -1f };

            var clamped = region.Clamped;

            Assert.That(clamped.W, Is.EqualTo(0.01f).Within(1e-6));
            Assert.That(clamped.H, Is.EqualTo(0.01f).Within(1e-6));
        }

        [Test]
        public void RegionClamped_区域右上有溢出_x_y回收到不越界()
        {
            var region = new MonkeyRegion { X = 0.9f, Y = 0.9f, W = 0.5f, H = 0.5f };

            var clamped = region.Clamped;

            // w/h clamp 到 [0.01,1] 后，x/y 上限收紧为 1-w / 1-h，保证 x+w/y+h 不越 [0,1]
            Assert.That(clamped.X + clamped.W, Is.LessThanOrEqualTo(1f + 1e-6));
            Assert.That(clamped.Y + clamped.H, Is.LessThanOrEqualTo(1f + 1e-6));
            Assert.That(clamped.X, Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(clamped.Y, Is.EqualTo(0.5f).Within(1e-6));
        }

        // ------------------------------------------------------------------ clamp：actionsPerSecond / durationSec

        [Test]
        public void ClampedActionsPerSecond_零或负数_clamp为0点1()
        {
            var parameters = new MonkeyParams { ActionsPerSecond = 0f };
            Assert.That(parameters.ClampedActionsPerSecond, Is.EqualTo(0.1f));

            parameters.ActionsPerSecond = -5f;
            Assert.That(parameters.ClampedActionsPerSecond, Is.EqualTo(0.1f));
        }

        [Test]
        public void ClampedActionsPerSecond_超上限_clamp为30()
        {
            Assert.That(new MonkeyParams { ActionsPerSecond = 999f }.ClampedActionsPerSecond, Is.EqualTo(30f));
        }

        [Test]
        public void ClampedDurationSec_零_clamp为0点1()
        {
            Assert.That(new MonkeyParams { DurationSec = 0f }.ClampedDurationSec, Is.EqualTo(0.1f));
        }

        [Test]
        public void ClampedDurationSec_超上限_clamp为7200()
        {
            Assert.That(new MonkeyParams { DurationSec = 99999f }.ClampedDurationSec, Is.EqualTo(7200f));
        }

        [Test]
        public void ClampedHangThresholdSec_低于下限_clamp为0点5()
        {
            Assert.That(new MonkeyParams { HangThresholdSec = 0.1f }.ClampedHangThresholdSec, Is.EqualTo(0.5f));
        }

        // ------------------------------------------------------------------ JSON 反序列化

        [Test]
        public void ToObject_含region的JSON_各字段正确还原()
        {
            var parameters = AxuJson.ToObject<MonkeyParams>(JObject.Parse(
                "{\"scene\":\"Assets/Scenes/Demo.unity\",\"durationSec\":30,\"actionsPerSecond\":5," +
                "\"seed\":42,\"region\":{\"x\":0.1,\"y\":0.2,\"w\":0.5,\"h\":0.6},\"stopOnError\":false}"));

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.Scene, Is.EqualTo("Assets/Scenes/Demo.unity"));
            Assert.That(parameters.DurationSec, Is.EqualTo(30f).Within(1e-6));
            Assert.That(parameters.ActionsPerSecond, Is.EqualTo(5f).Within(1e-6));
            Assert.That(parameters.Seed, Is.EqualTo(42));
            Assert.That(parameters.StopOnError, Is.False);
            Assert.That(parameters.Region, Is.Not.Null);
            Assert.That(parameters.Region.X, Is.EqualTo(0.1f).Within(1e-6));
            Assert.That(parameters.Region.Y, Is.EqualTo(0.2f).Within(1e-6));
            Assert.That(parameters.Region.W, Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(parameters.Region.H, Is.EqualTo(0.6f).Within(1e-6));
        }

        [Test]
        public void ToObject_空对象_回落默认值且region为null()
        {
            var parameters = AxuJson.ToObject<MonkeyParams>(new JObject());

            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters.DurationSec, Is.EqualTo(60f));
            Assert.That(parameters.ActionsPerSecond, Is.EqualTo(2f));
            Assert.That(parameters.Region, Is.Null);
        }
    }
}
