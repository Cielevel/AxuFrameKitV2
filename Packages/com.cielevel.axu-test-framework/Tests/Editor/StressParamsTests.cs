using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Agent.Capabilities.Stress;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] StressParams phases 解析 / 各工具参数 clamp / 未知 tool 保留契约（Phase 3 P1）。
    /// 关键契约点：
    ///  - phases 为顺序 POCO 列表，多工具混排保持声明顺序；
    ///  - 平铺参数各工具只读自己那组，clamp 访问器把脏参数落到安全区间；
    ///  - 未知 tool 字符串在 params 层不吞不报错（NormalizedTool 规范化保留小写原值，由 Runner 判未知工具）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class StressParamsTests
    {
        // ------------------------------------------------------------------ 默认值 / phases 解析

        [Test]
        public void 默认构造_Phases为空列表()
        {
            var parameters = new StressParams();

            Assert.That(parameters.Phases, Is.Not.Null);
            Assert.That(parameters.Phases, Is.Empty);
        }

        [Test]
        public void ToObject_多工具混排JSON_Phases按声明顺序还原()
        {
            var parameters = AxuJson.ToObject<StressParams>(JObject.Parse(
                "{\"phases\":[" +
                "{\"tool\":\"spawner\",\"startCount\":50,\"maxCount\":500}," +
                "{\"tool\":\"capacity\",\"targetFps\":30}," +
                "{\"tool\":\"churn\",\"createPerSec\":200}," +
                "{\"tool\":\"flood\",\"eventsPerFrame\":5000}]}"));

            Assert.That(parameters.Phases.Select(p => p.NormalizedTool),
                Is.EqualTo(new[] { "spawner", "capacity", "churn", "flood" }));
            Assert.That(parameters.Phases[0].StartCount, Is.EqualTo(50));
            Assert.That(parameters.Phases[0].MaxCount, Is.EqualTo(500));
            Assert.That(parameters.Phases[1].TargetFps, Is.EqualTo(30f).Within(1e-6));
            Assert.That(parameters.Phases[2].CreatePerSec, Is.EqualTo(200));
            Assert.That(parameters.Phases[3].EventsPerFrame, Is.EqualTo(5000));
        }

        [Test]
        public void ToObject_phase缺tool_回落默认spawner()
        {
            var parameters = AxuJson.ToObject<StressParams>(JObject.Parse("{\"phases\":[{\"startCount\":10}]}"));

            Assert.That(parameters.Phases[0].Tool, Is.EqualTo("spawner"));
            Assert.That(parameters.Phases[0].StartCount, Is.EqualTo(10));
        }

        // ------------------------------------------------------------------ NormalizedTool 规范化

        [Test]
        public void NormalizedTool_未知工具字符串_保留小写原值不吞()
        {
            var phase = new StressPhaseParams { Tool = "Teleport " };

            // params 层不吞未知工具：规范化后原样（小写），由 Runner 报"未知工具"
            Assert.That(phase.NormalizedTool, Is.EqualTo("teleport"));
        }

        [Test]
        public void NormalizedTool_空或null_返回空串()
        {
            Assert.That(new StressPhaseParams { Tool = null }.NormalizedTool, Is.EqualTo(string.Empty));
            Assert.That(new StressPhaseParams { Tool = "  " }.NormalizedTool, Is.EqualTo(string.Empty));
        }

        [Test]
        public void NormalizedTool_大小写与空白_规范化为小写()
        {
            Assert.That(new StressPhaseParams { Tool = " Spawner " }.NormalizedTool, Is.EqualTo("spawner"));
            Assert.That(new StressPhaseParams { Tool = "CAPACITY" }.NormalizedTool, Is.EqualTo("capacity"));
        }

        // ------------------------------------------------------------------ 各工具参数 clamp

        [Test]
        public void ClampedStartCount_零或负数_clamp为1()
        {
            var phase = new StressPhaseParams { StartCount = 0 };
            Assert.That(phase.ClampedStartCount, Is.EqualTo(1));

            phase.StartCount = -50;
            Assert.That(phase.ClampedStartCount, Is.EqualTo(1));
        }

        [Test]
        public void ClampedMaxCount_超上限_clamp为100000()
        {
            Assert.That(new StressPhaseParams { MaxCount = 9999999 }.ClampedMaxCount, Is.EqualTo(100000));
        }

        [Test]
        public void ClampedRampStep_负数_clamp为1()
        {
            Assert.That(new StressPhaseParams { RampStep = -1 }.ClampedRampStep, Is.EqualTo(1));
        }

        [Test]
        public void ClampedRampIntervalSec_负数_clamp为0()
        {
            Assert.That(new StressPhaseParams { RampIntervalSec = -5f }.ClampedRampIntervalSec, Is.EqualTo(0f));
        }

        [Test]
        public void ClampedSettleSec_超上限_clamp为600()
        {
            Assert.That(new StressPhaseParams { SettleSec = 9999f }.ClampedSettleSec, Is.EqualTo(600f));
        }

        [Test]
        public void ClampedTargetFps_零或超界_clamp到1到1000()
        {
            Assert.That(new StressPhaseParams { TargetFps = 0f }.ClampedTargetFps, Is.EqualTo(1f));
            Assert.That(new StressPhaseParams { TargetFps = 5000f }.ClampedTargetFps, Is.EqualTo(1000f));
        }

        [Test]
        public void ClampedBelowThresholdSec_低于下限_clamp为0点5()
        {
            Assert.That(new StressPhaseParams { BelowThresholdSec = 0.1f }.ClampedBelowThresholdSec, Is.EqualTo(0.5f));
        }

        [Test]
        public void ClampedConvergeTolerancePercent_超上限_clamp为50()
        {
            Assert.That(new StressPhaseParams { ConvergeTolerancePercent = 90f }.ClampedConvergeTolerancePercent, Is.EqualTo(50f));
        }

        [Test]
        public void ClampedCreatePerSec_零或超上限_clamp到1到10000()
        {
            Assert.That(new StressPhaseParams { CreatePerSec = 0 }.ClampedCreatePerSec, Is.EqualTo(1));
            Assert.That(new StressPhaseParams { CreatePerSec = 999999 }.ClampedCreatePerSec, Is.EqualTo(10000));
        }

        [Test]
        public void ClampedDurationSec_低于下限_clamp为0点1()
        {
            Assert.That(new StressPhaseParams { DurationSec = 0f }.ClampedDurationSec, Is.EqualTo(0.1f));
        }

        [Test]
        public void ClampedEventsPerFrame_负数_clamp为1()
        {
            Assert.That(new StressPhaseParams { EventsPerFrame = -100 }.ClampedEventsPerFrame, Is.EqualTo(1));
        }

        [Test]
        public void ClampedSubscribers_超上限_clamp为256()
        {
            Assert.That(new StressPhaseParams { Subscribers = 9999 }.ClampedSubscribers, Is.EqualTo(256));
        }

        // ------------------------------------------------------------------ capacity 默认值

        [Test]
        public void 默认phase_capacity探测默认值正确()
        {
            var phase = new StressPhaseParams { Tool = "capacity" };

            Assert.That(phase.StartCount, Is.EqualTo(100));
            Assert.That(phase.RampStep, Is.EqualTo(100));
            Assert.That(phase.MaxCount, Is.EqualTo(10000));
            Assert.That(phase.SettleSec, Is.EqualTo(2f).Within(1e-6));
            Assert.That(phase.TargetFps, Is.EqualTo(60f).Within(1e-6));
            Assert.That(phase.BelowThresholdSec, Is.EqualTo(2f).Within(1e-6));
            Assert.That(phase.ConvergeTolerancePercent, Is.EqualTo(5f).Within(1e-6));
        }
    }
}
