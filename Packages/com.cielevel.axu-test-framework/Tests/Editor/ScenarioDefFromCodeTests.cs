using System.Collections.Generic;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] ScenarioDef.FromCode 代码态注册契约（Phase 3 P1 gamehook，Core 纯 C#）。
    /// 关键契约点：
    ///  - FromCode 填 id/capability，params 字典深转 JObject（用法与 JSON params 一致）；
    ///  - setup 委托写入 CodeSetup（可空），供宿主在能力分发前同步调用；
    ///  - 空字典/不传参数得到空 JObject 而非 null。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class ScenarioDefFromCodeTests
    {
        [Test]
        public void FromCode_id与capability_正确落字段()
        {
            var def = ScenarioDef.FromCode("game.boss.fight", "gamehook");

            Assert.That(def.Id, Is.EqualTo("game.boss.fight"));
            Assert.That(def.Capability, Is.EqualTo("gamehook"));
            Assert.That(def.Enabled, Is.True);
        }

        [Test]
        public void FromCode_params字典_深转为JObject()
        {
            var parameters = new Dictionary<string, object>
            {
                ["levelId"] = 3,
                ["hardMode"] = true,
            };

            var def = ScenarioDef.FromCode("game.level", "gamehook", null, parameters);

            Assert.That(def.Params, Is.Not.Null);
            Assert.That(def.Params["levelId"].Value<int>(), Is.EqualTo(3));
            Assert.That(def.Params["hardMode"].Value<bool>(), Is.True);
        }

        [Test]
        public void FromCode_不传参数_Params为空JObject()
        {
            var def = ScenarioDef.FromCode("game.plain", "gamehook");

            Assert.That(def.Params, Is.Not.Null);
            Assert.That(def.Params, Is.Empty);
        }

        [Test]
        public void FromCode_setup委托_赋值到CodeSetup()
        {
            var called = false;
            void Setup(ScenarioContext context) => called = true;

            var def = ScenarioDef.FromCode("game.setup", "gamehook", Setup);

            Assert.That(def.CodeSetup, Is.Not.Null);

            def.CodeSetup(null);
            Assert.That(called, Is.True);
        }

        [Test]
        public void FromCode_不传setup_CodeSetup为null()
        {
            var def = ScenarioDef.FromCode("game.nosetup", "gamehook");

            Assert.That(def.CodeSetup, Is.Null);
        }
    }
}
