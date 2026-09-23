using System.Collections.Generic;
using System.IO;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] SuiteLoader 的 ref 解析契约（两条链路）：
    /// ① <c>Resolve</c> 的 externalById（gamehook 进程内注册）——补充语义（纯 ref 由注册定义供定义）、
    ///    覆盖语义（与 suite 内联同 id 时注册定义胜出）、未被引用的注册定义不产出条目；
    /// ② <b>文件加载链路</b>（阶段 1 修复）：<c>LoadSuite</c> 支持把运行时注册 id 作为
    ///    "externalIds" 传入，使**只含纯 ref 的 suite 文件**能通过校验；未提供的 ref 仍报明确错误。
    /// 回归背景：旧实现要求 ref 必须指向同文件内联定义，文件加载先于 Resolve 校验，纯 ref 的
    /// 代码态 suite 文件一律被拦下（Agent/DOC 支持的用法与文件加载链路不一致）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class SuiteLoaderExternalTests
    {
        static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AxuSuiteLoaderExternalTests");

        [SetUp]
        public void SetUp() => Directory.CreateDirectory(TempDir);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TempDir)) Directory.Delete(TempDir, recursive: true);
        }

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

        /// <summary>写一份只含纯 ref 的 suite 文件（真实文件形态，走 LoadSuite 全链路）。</summary>
        static string WritePureRefSuiteFile(string fileName)
        {
            var path = Path.Combine(TempDir, fileName);
            File.WriteAllText(path,
                "{\n" +
                "  \"schema\": \"axu-suite/1\",\n" +
                "  \"id\": \"suite.pure-ref\",\n" +
                "  \"scenarios\": [ { \"ref\": \"game.boss\" }, { \"ref\": \"game.missing\" } ]\n" +
                "}\n");
            return path;
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

        // ------------------------------------------------------------------ 文件加载链路（纯 ref）

        [Test]
        public void LoadSuite_纯ref且已注册_校验通过并可解析()
        {
            var path = WritePureRefSuiteFile("pure-ref.suite.json");

            // 只提供 game.boss 一个注册 id：文件里另一个 ref（game.missing）仍缺定义
            var registered = new[] { "game.boss" };
            var suite = SuiteLoader.LoadSuite(path, registered, ignoreUnresolvedRefs: true);
            var unresolved = SuiteLoader.FindUnresolvedRefs(suite, registered);

            Assert.That(suite.Id, Is.EqualTo("suite.pure-ref"));
            Assert.That(unresolved, Has.Count.EqualTo(1));
            Assert.That(unresolved[0], Does.Contain("game.missing"));
        }

        [Test]
        public void LoadSuite_纯ref全部已注册_校验通过且无未解析引用()
        {
            var path = WritePureRefSuiteFile("pure-ref.suite.json");
            var registeredIds = new[] { "game.boss", "game.missing" };

            var suite = SuiteLoader.LoadSuite(path, registeredIds);

            Assert.That(SuiteLoader.FindUnresolvedRefs(suite, registeredIds), Is.Empty);
            Assert.That(SuiteLoader.LoadSuite(path, registeredIds).Id, Is.EqualTo("suite.pure-ref"),
                "严格加载（不忽略未解析 ref）在全量注册下也必须通过");

            // 全链路：加载 → 扇出解析（注册定义供定义）
            var registered = new Dictionary<string, ScenarioDef>
            {
                ["game.boss"] = ScenarioDef.FromCode("game.boss", "gamehook"),
                ["game.missing"] = ScenarioDef.FromCode("game.missing", "gamehook"),
            };
            var resolved = SuiteLoader.Resolve(suite, null, TempDir, registered);
            Assert.That(resolved, Has.Count.EqualTo(2));
            Assert.That(resolved[0].Id, Is.EqualTo("game.boss"));
            Assert.That(resolved[1].Id, Is.EqualTo("game.missing"));
        }

        [Test]
        public void LoadSuite_纯ref无注册_抛带ref名的校验错误()
        {
            var path = WritePureRefSuiteFile("pure-ref.suite.json");

            var ex = Assert.Throws<System.InvalidOperationException>(
                () => SuiteLoader.LoadSuite(path));

            Assert.That(ex.Message, Does.Contain("game.boss"));
            Assert.That(ex.Message, Does.Contain("game.missing"));
            Assert.That(ex.Message, Does.Contain("未在 suite 内联定义中"));
        }

        [Test]
        public void LoadSuite_宽松模式_结构错误仍然拦下()
        {
            var path = Path.Combine(TempDir, "no-scenarios.suite.json");
            File.WriteAllText(path, "{ \"schema\": \"axu-suite/1\", \"id\": \"suite.empty\", \"scenarios\": [] }");

            var ex = Assert.Throws<System.InvalidOperationException>(
                () => SuiteLoader.LoadSuite(path, null, ignoreUnresolvedRefs: true));

            Assert.That(ex.Message, Does.Contain("scenarios 不能为空"));
        }

        [Test]
        public void Validate_内联与外部id混合_仅未命中的ref记为未解析()
        {
            var suite = BuildSuite(
                InlineEntry(InlineDef("game.inline", "smoke", "inline")),
                new SuiteScenarioRef { Ref = "game.inline" },
                new SuiteScenarioRef { Ref = "game.registered" },
                new SuiteScenarioRef { Ref = "game.unknown" });

            var errors = new List<string>();
            var unresolved = new List<string>();
            suite.Validate(new[] { "game.registered" }, errors, unresolved);

            Assert.That(errors, Is.Empty);
            Assert.That(unresolved, Has.Count.EqualTo(1));
            Assert.That(unresolved[0], Does.Contain("game.unknown"));
        }
    }
}
