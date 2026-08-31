using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] ScenarioCli `-axu*` 命令行解析语义（被测实现为 Phase 1 已落地源码，非契约推测）。
    /// 关键契约点：
    ///  - 兼容单/双横线与大小写（token 去前导 '-' 后小写比较，取值保留原样）；
    ///  - 取值型：-axuSuite/-axuScenario(可重复)/-axuReport/-axuArtifacts/-axuSeed；
    ///    开关型：-axuQuitOnDone/-axuWarnAsFail → true；-axuOverlay/-axuNoOverlay → OverlayHint true/false；
    ///  - 未知 -axu* 参数原串记入 IgnoredArgs（不吞取值）；非 -axu* 参数一律静默忽略；
    ///  - 空参数表恒返回非 null 结果，SuitePath 为空；
    ///  - -axuBaselineSave 为 Phase 2 新增参数：实现落地前以反射探测跳过（Ignore），落地后自动生效。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class ScenarioCliTests
    {
        // ------------------------------------------------------------------ 全参数解析

        [Test]
        public void Parse_全参数表_各字段正确解析()
        {
            var options = ScenarioCli.Parse(new[]
            {
                "-batchmode", "-axuSuite", "suite.json", "-axuScenario", "perf.a", "-axuScenario", "smoke.b",
                "-axuReport", "report.json", "-axuArtifacts", "artifacts", "-axuQuitOnDone", "-axuWarnAsFail",
                "-axuSeed", "12345", "-axuOverlay",
            });

            Assert.That(options, Is.Not.Null);
            Assert.That(options.SuitePath, Is.EqualTo("suite.json"));
            Assert.That(options.ScenarioIds, Is.EqualTo(new[] { "perf.a", "smoke.b" }));
            Assert.That(options.ReportPath, Is.EqualTo("report.json"));
            Assert.That(options.ArtifactsDir, Is.EqualTo("artifacts"));
            Assert.That(options.QuitOnDone, Is.True);
            Assert.That(options.WarnAsFail, Is.True);
            Assert.That(options.Seed, Is.EqualTo(12345));
            Assert.That(options.OverlayHint, Is.True);
        }

        [Test]
        public void Parse_axuNoOverlay_OverlayHint为false()
        {
            var options = ScenarioCli.Parse(new[] { "-axuNoOverlay" });

            Assert.That(options.OverlayHint, Is.False);
        }

        // ------------------------------------------------------------------ 未知与非 axu 参数

        [Test]
        public void Parse_未知axu参数_原串记入IgnoredArgs且不吞非axu参数()
        {
            var options = ScenarioCli.Parse(new[] { "-axuCustomFlag", "extra", "-batchmode" });

            Assert.That(options.IgnoredArgs, Is.EqualTo(new[] { "-axuCustomFlag" }));
            Assert.That(options.SuitePath, Is.Null);
        }

        [Test]
        public void Parse_仅非axu参数_IgnoredArgs保持为空()
        {
            var options = ScenarioCli.Parse(new[] { "-batchmode", "-nographics", "positional" });

            Assert.That(options.IgnoredArgs, Is.Empty);
        }

        // ------------------------------------------------------------------ 边界

        [Test]
        public void Parse_空参数表_返回默认空选项()
        {
            var empty = ScenarioCli.Parse(new string[0]);

            Assert.That(empty, Is.Not.Null);
            Assert.That(empty.SuitePath, Is.Null);
            Assert.That(empty.ScenarioIds, Is.Empty);
            Assert.That(empty.IgnoredArgs, Is.Empty);

            var nullArgs = ScenarioCli.Parse(null);
            Assert.That(nullArgs, Is.Not.Null);
            Assert.That(nullArgs.SuitePath, Is.Null);
        }

        [Test]
        public void Parse_双横线大小写变体_正常解析且取值大小写保留()
        {
            var options = ScenarioCli.Parse(new[] { "--AxuSuite", "MySuite.JSON", "--axuSeed", "7" });

            Assert.That(options.SuitePath, Is.EqualTo("MySuite.JSON"));
            Assert.That(options.Seed, Is.EqualTo(7));
        }

        [Test]
        public void Parse_axuSeed非法整数_Seed保持null()
        {
            var options = ScenarioCli.Parse(new[] { "-axuSeed", "abc" });

            Assert.That(options.Seed, Is.Null);
        }

        [Test]
        public void Parse_取值型参数缺值_对应字段为null且不崩溃()
        {
            var options = ScenarioCli.Parse(new[] { "-axuReport" });

            Assert.That(options.ReportPath, Is.Null);
        }

        // ------------------------------------------------------------------ Phase 2 新增：-axuBaselineSave

        [Test]
        public void Parse_axuBaselineSave_解析基线保存路径()
        {
            // Phase 2 开工决策③：-axuBaselineSave <path> 本期落地；
            // 实现未合入前该参数会落进 IgnoredArgs——这里以属性探测代替编译期依赖，落地后本用例自动生效。
            var property = Array.Find(
                new[] { "BaselineSavePath", "BaselinePath", "BaselineSave" },
                name => typeof(ScenarioCliOptions).GetProperty(name) != null);
            if (property == null)
                Assert.Ignore("ScenarioCliOptions 尚无 -axuBaselineSave 对应属性（契约名 BaselineSavePath）；实现落地后本用例自动生效。");

            var options = ScenarioCli.Parse(new[] { "-axuBaselineSave", "baseline.json" });

            Assert.That(options.IgnoredArgs, Does.Not.Contain("-axuBaselineSave"),
                "-axuBaselineSave 已是受支持参数，不应落入 IgnoredArgs。");
            Assert.That(typeof(ScenarioCliOptions).GetProperty(property).GetValue(options),
                Is.EqualTo("baseline.json"));
        }
    }
}
