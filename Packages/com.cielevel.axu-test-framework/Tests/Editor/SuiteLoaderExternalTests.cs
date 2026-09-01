using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] SuiteLoader.Resolve 的 externalById（gamehook 进程内注册）契约（Phase 3 P1）。
    /// 关键契约点：
    ///  - 补充语义：ref 条目无内联定义时命中 externalById 供定义；
    ///  - 覆盖语义：与 suite 内联同 id 时注册定义胜出（capability/params 取注册侧）；
    ///  - 未被 suite 引用的注册定义不产出条目（Resolve 是拉取式扇出，不做注册表遍历）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class SuiteLoaderExternalTests
    {
        static SuiteDef BuildSuite(params SuiteScenarioRef[] entries)
        {
            return new SuiteDef { Id = "suite.test", Scenarios = new List<SuiteScenarioRef>(entries) };
        }

        static ScenarioDef InlineDef(string id, string capability, string paramValue)
        {
            return new ScenarioDef
            {
                Id = id,
                Capability = capability,
                Params = new JObject { ["mode"] = paramValue },
            };
        }

        // ------------------------------------------------------------------ 补充语义

        [Test]
        public void Resolve_ref无内联定义_命中externalById补充供定义()
        {
            var suite = BuildSuite(new SuiteScenarioRef { Ref = "game.boss" });
            var external = new Dictionary<string, ScenarioDef>
            {
                ["game.boss"] = ScenarioDef.FromCode("game.boss", "gamehook", null,
                    new Dictionary<string, object> { ["mode"] = "registered" }),
            };

            var results = SuiteLoader.Resolve(suite, externalById: external);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo("game.boss"));
            Assert.That(results[0].Capability, Is.EqualTo("gamehook"));
            Assert.That(results[0].Params["mode"].Value<string>(), Is.EqualTo("registered"));
        }

        [Test]
        public void Resolve_ref无定义也无注册_抛InvalidOperationException()
        {
            var suite = BuildSuite(new SuiteScenarioRef { Ref = "game.missing" });

            Assert.That(() => SuiteLoader.Resolve(suite),
                Throws.InvalidOperationException.With.Message.Contains("game.missing"));
        }

        // ------------------------------------------------------------------ 覆盖语义

        [Test]
        public void Resolve_内联与注册同id_注册定义胜出()
        {
            var suite = BuildSuite(
                InlineEntry(InlineDef("game.overlap", "smoke", "inline")),
                new SuiteScenarioRef { Ref = "game.overlap" });
            var external = new Dictionary<string, ScenarioDef>
            {
                ["game.overlap"] = ScenarioDef.FromCode("game.overlap", "gamehook", null,
                    new Dictionary<string, object> { ["mode"] = "registered" }),
            };

            var results = SuiteLoader.Resolve(suite, externalById: external);

            // 内联条目与 ref 条目均被同 id 注册定义覆盖，两条产出都用注册侧定义
            Assert.That(results, Has.Count.EqualTo(2));
            foreach (var resolved in results)
            {
                Assert.That(resolved.Capability, Is.EqualTo("gamehook"));
                Assert.That(resolved.Params["mode"].Value<string>(), Is.EqualTo("registered"));
            }
        }

        [Test]
        public void Resolve_覆盖时CodeSetup透传到产出条目()
        {
            var setupCalled = 0;
            var suite = BuildSuite(InlineEntry(InlineDef("game.hooked", "smoke", "inline")));
            var external = new Dictionary<string, ScenarioDef>
            {
                ["game.hooked"] = ScenarioDef.FromCode("game.hooked", "gamehook",
                    _ => setupCalled++, null),
            };

            var results = SuiteLoader.Resolve(suite, externalById: external);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].CodeSetup, Is.Not.Null);
            results[0].CodeSetup(null);
            Assert.That(setupCalled, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ 未引用不产出

        [Test]
        public void Resolve_未被suite引用的注册定义_不产出条目()
        {
            var suite = BuildSuite(new SuiteScenarioRef { Ref = "game.used" });
            var external = new Dictionary<string, ScenarioDef>
            {
                ["game.used"] = ScenarioDef.FromCode("game.used", "gamehook"),
                ["game.unused"] = ScenarioDef.FromCode("game.unused", "gamehook"),
            };

            var results = SuiteLoader.Resolve(suite, externalById: external);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo("game.used"));
        }

        // ------------------------------------------------------------------ null 容错

        [Test]
        public void Resolve_externalById为null_ref条目走内联定义()
        {
            var suite = BuildSuite(
                InlineEntry(InlineDef("game.inline", "smoke", "inline")),
                new SuiteScenarioRef { Ref = "game.inline" });

            var results = SuiteLoader.Resolve(suite, externalById: null);

            Assert.That(results, Has.Count.EqualTo(2));
            foreach (var resolved in results)
            {
                Assert.That(resolved.Capability, Is.EqualTo("smoke"));
                Assert.That(resolved.Params["mode"].Value<string>(), Is.EqualTo("inline"));
            }
        }

        static SuiteScenarioRef InlineEntry(ScenarioDef def)
        {
            return new SuiteScenarioRef { Inline = def };
        }
    }
}
