using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 触发器配置分发 EditMode 测试（依赖框架家族试点）：
    /// 覆盖 TriggerCommentConfigSo 的精确 / 透传 / 正则三路分发，以及
    /// TriggerGiftConfigSo 的直传与按数量切割两种礼物触发模式。
    /// 触发器为测试程序集内定义的 ScriptableObject 桩；SO 私有序列化成员经反射注入
    /// （与 M3 PlayMode 侧的反射注入纪律一致，EditMode 下亦可避免依赖 Inspector 赋值）。
    /// </summary>
    [Category("Axu/Module/LiveStream")]
    public class LiveTriggerConfigTests
    {
        private readonly List<ScriptableObject> _createdObjects = new List<ScriptableObject>();

        #region 测试桩（ScriptableObject 触发器，测试程序集内定义）

        private class StubCommentTrigger : LiveTriggerCommentBase
        {
            public int CallCount;
            public UserDataComment LastUnit;
            public string LastMessage;
            public bool LastWasRegex;
            public bool LastMatchSuccess;

            public override void Trigger(UserDataComment userDataUnit)
            {
                CallCount++;
                LastUnit = userDataUnit;
                LastMessage = null;
                LastWasRegex = false;
            }

            public override void Trigger(UserDataComment userDataUnit, string chatMessage)
            {
                CallCount++;
                LastUnit = userDataUnit;
                LastMessage = chatMessage;
                LastWasRegex = false;
            }

            public override void Trigger(UserDataComment userDataUnit, Match match)
            {
                CallCount++;
                LastUnit = userDataUnit;
                LastWasRegex = true;
                LastMatchSuccess = match.Success;
            }
        }

        private class StubGiftTrigger : LiveTriggerGiftBase
        {
            public int CallCount;
            public UserDataGift LastUnit;
            public readonly List<int> DeliveredCounts = new List<int>();

            public override void Trigger(UserDataGift userDataUnit, int count)
            {
                CallCount++;
                LastUnit = userDataUnit;
                DeliveredCounts.Add(count);
            }
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            // 清理本轮自建的 ScriptableObject，避免跨测试残留（工程关闭域重载）
            foreach (var created in _createdObjects)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void TriggerCommentConfigSo_PushComment_精确透传与正则三路分发()
        {
            // Arrange：三路配置单元（精确不透传 / 精确透传 / 正则）
            var config = Track(ScriptableObject.CreateInstance<TriggerCommentConfigSo>());
            var stubExact = Track(ScriptableObject.CreateInstance<StubCommentTrigger>());
            var stubDeliver = Track(ScriptableObject.CreateInstance<StubCommentTrigger>());
            var stubRegex = Track(ScriptableObject.CreateInstance<StubCommentTrigger>());

            SetProperty(config, "TriggerCommentConfigUnitList", new List<TriggerCommentConfigUnit>
            {
                MakeCommentUnit("666", useRegex: false, deliver: false, stubExact),
                MakeCommentUnit("主播", useRegex: false, deliver: true, stubDeliver),
                MakeCommentUnit(@"赞\d+", useRegex: true, deliver: false, stubRegex)
            });

            // Act & Assert：精确匹配、不透传 → Trigger(unit) 单参形态
            config.PushComment(new UserDataComment { Comment = "666" });
            Assert.That(stubExact.CallCount, Is.EqualTo(1), "精确匹配应触发一次");
            Assert.That(stubExact.LastUnit.Comment, Is.EqualTo("666"));
            Assert.That(stubExact.LastMessage, Is.Null, "不透传时不应携带消息文本");
            Assert.That(stubDeliver.CallCount, Is.EqualTo(0));
            Assert.That(stubRegex.CallCount, Is.EqualTo(0));

            // 精确匹配（整串相等）+ 透传原文 → Trigger(unit, chatMessage)
            config.PushComment(new UserDataComment { Comment = "主播" });
            Assert.That(stubDeliver.CallCount, Is.EqualTo(1), "精确匹配应触发透传单元一次");
            Assert.That(stubDeliver.LastMessage, Is.EqualTo("主播"), "透传应携带评论原文");

            // 正则匹配 → Trigger(unit, match)
            config.PushComment(new UserDataComment { Comment = "赞999" });
            Assert.That(stubRegex.CallCount, Is.EqualTo(1), "正则单元应触发一次");
            Assert.That(stubRegex.LastWasRegex, Is.True);
            Assert.That(stubRegex.LastMatchSuccess, Is.True);

            // 无匹配 → 各桩计数不变
            config.PushComment(new UserDataComment { Comment = "不匹配的内容" });
            Assert.That(stubExact.CallCount, Is.EqualTo(1), "无匹配不应追加触发");
            Assert.That(stubDeliver.CallCount, Is.EqualTo(1));
            Assert.That(stubRegex.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void TriggerGiftConfigSo_PushGift_直传与按数量切割两种模式触发()
        {
            // Arrange：UseGiftIndex 默认 false → 按礼物名匹配
            var config = Track(ScriptableObject.CreateInstance<TriggerGiftConfigSo>());
            var stubDirect = Track(ScriptableObject.CreateInstance<StubGiftTrigger>());
            var stubSplit = Track(ScriptableObject.CreateInstance<StubGiftTrigger>());

            SetProperty(config, "TriggerGiftConfigUnitWithCountList", new List<TriggerGiftConfigUnit>
            {
                MakeGiftUnit("火箭", stubDirect) // 直传：每次礼物按实际数量原样触发
            });
            SetProperty(config, "TriggerGiftConfigUnitList", new List<TriggerGiftConfigUnit>
            {
                MakeGiftUnit("火箭", stubSplit, giftCount: 10) // 切割：每 10 个触发一次
            });

            // Act：一次送 23 个火箭
            config.PushGift(new UserDataGift { UserId = "u1", GiftName = "火箭", GiftCount = 23 });

            // Assert：直传恰好一次且携带实际数量
            Assert.That(stubDirect.CallCount, Is.EqualTo(1), "直传单元应恰好触发一次");
            Assert.That(stubDirect.DeliveredCounts, Is.EqualTo(new[] { 23 }), "直传应传递实际礼物数量");

            // Assert：23 个按每 10 个切割 → 触发 2 次，每次传配置数量
            Assert.That(stubSplit.CallCount, Is.EqualTo(2), "23 个应按每 10 个切割触发 2 次");
            Assert.That(stubSplit.DeliveredCounts, Is.EqualTo(new[] { 10, 10 }), "切割触发应传递配置数量");

            // 非配置的礼物名不触发
            config.PushGift(new UserDataGift { UserId = "u2", GiftName = "小心心", GiftCount = 5 });
            Assert.That(stubDirect.CallCount, Is.EqualTo(1), "未配置礼物不应追加触发");
            Assert.That(stubSplit.CallCount, Is.EqualTo(2));
        }

        #region 注入与登记辅助

        private T Track<T>(T created) where T : ScriptableObject
        {
            _createdObjects.Add(created);
            return created;
        }

        private static TriggerCommentConfigUnit MakeCommentUnit(
            string comment, bool useRegex, bool deliver, LiveTriggerCommentBase trigger)
        {
            var unit = new TriggerCommentConfigUnit();
            SetField(unit, "_comment", comment);
            SetField(unit, "_useRegex", useRegex);
            SetField(unit, "_deliverMessage", deliver);
            SetField(unit, "_regexComment", comment);
            SetField(unit, "_trigger", trigger);
            return unit;
        }

        private static TriggerGiftConfigUnit MakeGiftUnit(
            string giftName, LiveTriggerGiftBase trigger, int giftCount = 0)
        {
            var unit = new TriggerGiftConfigUnit();
            SetField(unit, "_giftName", giftName);
            SetField(unit, "_giftIndex", 0);
            SetField(unit, "_giftCount", giftCount);
            SetField(unit, "_trigger", trigger);
            return unit;
        }

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"属性 {name} 不存在");
            property.SetValue(target, value);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"字段 {name} 不存在");
            field.SetValue(target, value);
        }

        #endregion
    }
}
