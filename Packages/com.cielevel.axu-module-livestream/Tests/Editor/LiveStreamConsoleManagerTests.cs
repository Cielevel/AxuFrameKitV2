using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 直播控制台总控 EditMode 测试：
    /// 覆盖 LiveStreamConsoleManager 的 Start 逐项 null 校验（指名报错且不订阅）、
    /// 三类私有 handler 的空选择 / 空评论告警分支，以及正常路径下
    /// 构造 UserDataComment/Like/Gift 并经 TirggerCollectionSo 分发到 stub 触发器的完整链路。
    /// 触发器为测试程序集内定义的 ScriptableObject 桩；SO/面板序列化引用经反射注入。
    /// EditMode 不执行 Start，校验与订阅断言均经反射手动调用 Start / handler。
    /// </summary>
    [Category("Axu/Module/LiveStream")]
    public class LiveStreamConsoleManagerTests
    {
        private readonly List<ScriptableObject> _createdSo = new List<ScriptableObject>();
        private readonly List<GameObject> _createdGo = new List<GameObject>();
        private LiveStreamConsoleUiRig _rig;

        #region 测试桩（ScriptableObject 触发器，测试程序集内定义）

        private class StubCommentTrigger : LiveTriggerCommentBase
        {
            public readonly List<UserDataComment> Received = new List<UserDataComment>();
            public bool LastWasRegex;
            public bool LastMatchSuccess;

            public override void Trigger(UserDataComment userDataUnit)
            {
                Received.Add(userDataUnit);
                LastWasRegex = false;
            }

            public override void Trigger(UserDataComment userDataUnit, string chatMessage)
            {
                Received.Add(userDataUnit);
                LastWasRegex = false;
            }

            public override void Trigger(UserDataComment userDataUnit, Match match)
            {
                Received.Add(userDataUnit);
                LastWasRegex = true;
                LastMatchSuccess = match.Success;
            }
        }

        private class StubGiftTrigger : LiveTriggerGiftBase
        {
            public readonly List<UserDataGift> Received = new List<UserDataGift>();
            public readonly List<int> DeliveredCounts = new List<int>();

            public override void Trigger(UserDataGift userDataUnit, int count)
            {
                Received.Add(userDataUnit);
                DeliveredCounts.Add(count);
            }
        }

        private class StubLikeTrigger : LiveTriggerLikeBase
        {
            public readonly List<UserDataLike> Received = new List<UserDataLike>();
            public readonly List<int> DeliveredCounts = new List<int>();

            public override void Trigger(UserDataLike userDataUnit, int count)
            {
                Received.Add(userDataUnit);
                DeliveredCounts.Add(count);
            }
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _createdGo)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _createdGo.Clear();

            foreach (var so in _createdSo)
            {
                if (so != null)
                {
                    Object.DestroyImmediate(so);
                }
            }

            _createdSo.Clear();
            _rig = null;
        }

        [Test]
        public void Start_引用逐项缺失_逐项指名报错且不订阅面板事件()
        {
            // Arrange：四项引用全部缺失的面板（rig 暂不激活，最后阶段再激活）
            _rig = LiveStreamConsoleUiRig.Build();
            Track(_rig.Root);
            var manager = CreateManager();

            // Act & Assert 阶段0：全部缺失 → 只报第一项 _userConfigSo
            LogAssert.Expect(LogType.Error, new Regex(@"未在 Inspector 上挂载 _userConfigSo"));
            LiveStreamTestReflection.Call(manager, "Start");
            AssertEventNotSubscribed(_rig.Panel);

            // Act & Assert 阶段1：补 _userConfigSo → 报 _giftConfigSo
            LiveStreamTestReflection.SetField(manager, "_userConfigSo", MakeUserConfig(MakeUser("u1", "一号")));
            LogAssert.Expect(LogType.Error, new Regex(@"未在 Inspector 上挂载 _giftConfigSo"));
            LiveStreamTestReflection.Call(manager, "Start");
            AssertEventNotSubscribed(_rig.Panel);

            // Act & Assert 阶段2：补 _giftConfigSo → 报 _tirggerCollectionSo
            LiveStreamTestReflection.SetField(manager, "_giftConfigSo", MakeGiftConfig(MakeGift("火箭", 1)));
            LogAssert.Expect(LogType.Error, new Regex(@"未在 Inspector 上挂载 _tirggerCollectionSo"));
            LiveStreamTestReflection.Call(manager, "Start");
            AssertEventNotSubscribed(_rig.Panel);

            // Act & Assert 阶段3：补 _tirggerCollectionSo → 报 _panel
            LiveStreamTestReflection.SetField(manager, "_tirggerCollectionSo",
                MakeCollection(out _, out _, out _));
            LogAssert.Expect(LogType.Error, new Regex(@"未在 Inspector 上挂载 _panel"));
            LiveStreamTestReflection.Call(manager, "Start");
            AssertEventNotSubscribed(_rig.Panel);

            // Act & Assert 阶段4：补 _panel → 引用齐全，正常订阅（此阶段不应再有任何报错）
            _rig.Activate();
            LiveStreamTestReflection.SetField(manager, "_panel", _rig.Panel);
            LiveStreamTestReflection.Call(manager, "Start");
            AssertEventSubscribed(_rig.Panel);
        }

        [Test]
        public void Start_引用齐全_订阅三类请求事件并按配置克隆条目()
        {
            // Arrange
            var manager = CreateWiredManager(out _rig, out _, out _, out _);

            // Act
            LiveStreamTestReflection.Call(manager, "Start");

            // Assert：三类事件均已订阅
            AssertEventSubscribed(_rig.Panel);

            // Assert：Start 内部调用面板 Setup，按配置克隆条目
            Assert.That(_rig.UserItems, Has.Count.EqualTo(2), "Start 应触发面板克隆用户条目");
            Assert.That(_rig.GiftItems, Has.Count.EqualTo(2), "Start 应触发面板克隆礼物条目");
            Assert.That(_rig.UserItems[0].Data.UserId, Is.EqualTo("u1"));
            Assert.That(_rig.GiftItems[1].Data.Name, Is.EqualTo("星星"));
        }

        [Test]
        public void HandleCommentRequested_未选用户_警告且不分发()
        {
            // Arrange：不勾选任何用户
            var manager = CreateWiredManager(out _rig, out var commentStub, out _, out _);
            LiveStreamTestReflection.Call(manager, "Start");

            // Act
            LogAssert.Expect(LogType.Warning, "请先选择至少一个用户！");
            LiveStreamTestReflection.Call(manager, "HandleCommentRequested");

            // Assert：无分发、评论输入不被清空
            Assert.That(commentStub.Received, Is.Empty, "空选择不应分发评论");
        }

        [Test]
        public void HandleCommentRequested_评论为空_警告且不分发()
        {
            // Arrange：勾选用户但不输入评论
            var manager = CreateWiredManager(out _rig, out var commentStub, out _, out _);
            LiveStreamTestReflection.Call(manager, "Start");
            _rig.SetUserSelected(_rig.UserItems[0], true);

            // Act
            LogAssert.Expect(LogType.Warning, "评论不能为空！");
            LiveStreamTestReflection.Call(manager, "HandleCommentRequested");

            // Assert
            Assert.That(commentStub.Received, Is.Empty, "空评论不应分发");
        }

        [Test]
        public void HandleCommentRequested_正常选择_逐用户构造评论分发并清空输入()
        {
            // Arrange：勾选 2 个用户，输入含首尾空白的评论
            var manager = CreateWiredManager(out _rig, out var commentStub, out _, out _);
            LiveStreamTestReflection.Call(manager, "Start");
            _rig.SetUserSelected(_rig.UserItems[0], true);
            _rig.SetUserSelected(_rig.UserItems[1], true);
            _rig.SetCommentText("  主播好  ");

            // Act
            LiveStreamTestReflection.Call(manager, "HandleCommentRequested");

            // Assert：逐用户各分发一条，字段与选中一致
            Assert.That(commentStub.Received, Has.Count.EqualTo(2), "2 个选中用户应分发 2 条评论");
            Assert.That(commentStub.Received[0].UserId, Is.EqualTo("u1"));
            Assert.That(commentStub.Received[0].Nickname, Is.EqualTo("一号"));
            Assert.That(commentStub.Received[0].AvatarUrl, Is.EqualTo("http://a/u1.png"));
            Assert.That(commentStub.Received[0].Comment, Is.EqualTo("主播好"), "评论应为 Trim 后文本");
            Assert.That(commentStub.Received[1].UserId, Is.EqualTo("u2"));
            Assert.That(commentStub.Received[1].Comment, Is.EqualTo("主播好"));
            Assert.That(commentStub.LastWasRegex, Is.True, "分发应经配置正则入口");
            Assert.That(commentStub.LastMatchSuccess, Is.True);

            // Assert：分发后评论输入被清空
            Assert.That(_rig.GetCommentInputText(), Is.Empty, "分发完成后应清空评论输入");
        }

        [Test]
        public void HandleLikeRequested_未选用户_警告且不分发()
        {
            // Arrange：不勾选任何用户
            var manager = CreateWiredManager(out _rig, out _, out var likeStub, out _);
            LiveStreamTestReflection.Call(manager, "Start");

            // Act
            LogAssert.Expect(LogType.Warning, "请先选择至少一个用户！");
            LiveStreamTestReflection.Call(manager, "HandleLikeRequested");

            // Assert
            Assert.That(likeStub.Received, Is.Empty, "空选择不应分发点赞");
        }

        [Test]
        public void HandleLikeRequested_正常选择_逐用户分发点赞数量()
        {
            // Arrange：勾选 2 个用户，点赞数输入 5
            var manager = CreateWiredManager(out _rig, out _, out var likeStub, out _);
            LiveStreamTestReflection.Call(manager, "Start");
            _rig.SetUserSelected(_rig.UserItems[0], true);
            _rig.SetUserSelected(_rig.UserItems[1], true);
            _rig.SetLikeCountText("5");

            // Act
            LiveStreamTestReflection.Call(manager, "HandleLikeRequested");

            // Assert：逐用户各分发一条，携带输入数量与用户字段
            Assert.That(likeStub.Received, Has.Count.EqualTo(2), "2 个选中用户应分发 2 条点赞");
            Assert.That(likeStub.DeliveredCounts, Is.EqualTo(new[] { 5, 5 }), "点赞数应取输入框值");
            Assert.That(likeStub.Received[0].UserId, Is.EqualTo("u1"));
            Assert.That(likeStub.Received[0].Nickname, Is.EqualTo("一号"));
            Assert.That(likeStub.Received[0].LikeCount, Is.EqualTo(5));
            Assert.That(likeStub.Received[1].UserId, Is.EqualTo("u2"));
        }

        [Test]
        public void HandleGiftRequested_未选用户或未选礼物_警告且不分发()
        {
            // Arrange
            var manager = CreateWiredManager(out _rig, out _, out _, out var giftStub);
            LiveStreamTestReflection.Call(manager, "Start");

            // Act & Assert A：未选用户 → 用户告警
            LogAssert.Expect(LogType.Warning, "请先选择至少一个用户！");
            LiveStreamTestReflection.Call(manager, "HandleGiftRequested");
            Assert.That(giftStub.Received, Is.Empty, "空选择不应分发礼物");

            // Act & Assert B：选了用户但未选礼物 → 礼物告警
            _rig.SetUserSelected(_rig.UserItems[0], true);
            LogAssert.Expect(LogType.Warning, "请先选择至少一个礼物！");
            LiveStreamTestReflection.Call(manager, "HandleGiftRequested");
            Assert.That(giftStub.Received, Is.Empty, "未选礼物不应分发");
        }

        [Test]
        public void HandleGiftRequested_正常选择_用户乘礼物逐条分发携带名称下标数量()
        {
            // Arrange：勾选 2 用户 × 2 礼物，数量分别为 3 / 10
            var manager = CreateWiredManager(out _rig, out _, out _, out var giftStub);
            LiveStreamTestReflection.Call(manager, "Start");
            _rig.SetUserSelected(_rig.UserItems[0], true);
            _rig.SetUserSelected(_rig.UserItems[1], true);
            _rig.SetGiftSelected(_rig.GiftItems[0], true);
            _rig.SetGiftSelected(_rig.GiftItems[1], true);
            _rig.SetGiftItemCount(_rig.GiftItems[0], "3");
            _rig.SetGiftItemCount(_rig.GiftItems[1], "10");

            // Act
            LiveStreamTestReflection.Call(manager, "HandleGiftRequested");

            // Assert：2 × 2 = 4 条，用户外层 × 礼物内层顺序
            Assert.That(giftStub.Received, Has.Count.EqualTo(4), "2 用户 × 2 礼物应分发 4 条");
            Assert.That(giftStub.DeliveredCounts, Is.EqualTo(new[] { 3, 10, 3, 10 }),
                "数量应按用户外层、礼物内层顺序展开");

            AssertGift(giftStub.Received[0], "u1", "一号", "火箭", 0, 3);
            AssertGift(giftStub.Received[1], "u1", "一号", "星星", 1, 10);
            AssertGift(giftStub.Received[2], "u2", "二号", "火箭", 0, 3);
            AssertGift(giftStub.Received[3], "u2", "二号", "星星", 1, 10);
        }

        #region 搭建与断言辅助

        private LiveStreamConsoleManager CreateManager()
        {
            var go = new GameObject("LiveStreamConsoleManager");
            Track(go);
            return go.AddComponent<LiveStreamConsoleManager>();
        }

        /// <summary>搭建引用齐全的总控（SO 三件套 + 面板 Rig 已激活，Start 由测试手动触发）。</summary>
        private LiveStreamConsoleManager CreateWiredManager(
            out LiveStreamConsoleUiRig rig,
            out StubCommentTrigger commentStub,
            out StubLikeTrigger likeStub,
            out StubGiftTrigger giftStub)
        {
            rig = LiveStreamConsoleUiRig.Build();
            Track(rig.Root);
            rig.Activate();

            var manager = CreateManager();
            LiveStreamTestReflection.SetField(manager, "_userConfigSo",
                MakeUserConfig(MakeUser("u1", "一号"), MakeUser("u2", "二号")));
            LiveStreamTestReflection.SetField(manager, "_giftConfigSo",
                MakeGiftConfig(MakeGift("火箭", 1000), MakeGift("星星", 10)));
            LiveStreamTestReflection.SetField(manager, "_tirggerCollectionSo",
                MakeCollection(out commentStub, out likeStub, out giftStub));
            LiveStreamTestReflection.SetField(manager, "_panel", rig.Panel);
            return manager;
        }

        private UserConfigSo MakeUserConfig(params UserDataUnit[] users)
        {
            var so = TrackSo(ScriptableObject.CreateInstance<UserConfigSo>());
            LiveStreamTestReflection.SetField(so, "_userDataUnitList", new List<UserDataUnit>(users));
            return so;
        }

        private GiftConfigSo MakeGiftConfig(params GiftDataUnit[] gifts)
        {
            var so = TrackSo(ScriptableObject.CreateInstance<GiftConfigSo>());
            LiveStreamTestReflection.SetField(so, "_giftDataUnitList", new List<GiftDataUnit>(gifts));
            return so;
        }

        /// <summary>
        /// 组装 TirggerCollectionSo：仅挂 MainTrigger 三件套（PreTrigger 留空验证可空链不炸）；
        /// 评论走正则 ".*" 任意命中，礼物按名称直传，点赞取首单元入口。
        /// </summary>
        private TirggerCollectionSo MakeCollection(
            out StubCommentTrigger commentStub,
            out StubLikeTrigger likeStub,
            out StubGiftTrigger giftStub)
        {
            var collection = TrackSo(ScriptableObject.CreateInstance<TirggerCollectionSo>());

            var commentConfig = TrackSo(ScriptableObject.CreateInstance<TriggerCommentConfigSo>());
            commentStub = TrackSo(ScriptableObject.CreateInstance<StubCommentTrigger>());
            var commentUnit = new TriggerCommentConfigUnit();
            LiveStreamTestReflection.SetField(commentUnit, "_comment", "unused-exact");
            LiveStreamTestReflection.SetField(commentUnit, "_useRegex", true);
            LiveStreamTestReflection.SetField(commentUnit, "_regexComment", ".*");
            LiveStreamTestReflection.SetField(commentUnit, "_deliverMessage", false);
            LiveStreamTestReflection.SetField(commentUnit, "_trigger", commentStub);
            LiveStreamTestReflection.SetProperty(commentConfig, "TriggerCommentConfigUnitList",
                new List<TriggerCommentConfigUnit> { commentUnit });

            var likeConfig = TrackSo(ScriptableObject.CreateInstance<TriggerLikeConfigSo>());
            likeStub = TrackSo(ScriptableObject.CreateInstance<StubLikeTrigger>());
            var likeUnit = new TriggerLikeConfigUnit();
            LiveStreamTestReflection.SetField(likeUnit, "_likeCount", 0);
            LiveStreamTestReflection.SetField(likeUnit, "_trigger", likeStub);
            LiveStreamTestReflection.SetProperty(likeConfig, "TriggerLikeConfigUnitList",
                new List<TriggerLikeConfigUnit> { likeUnit });

            var giftConfig = TrackSo(ScriptableObject.CreateInstance<TriggerGiftConfigSo>());
            giftStub = TrackSo(ScriptableObject.CreateInstance<StubGiftTrigger>());
            LiveStreamTestReflection.SetProperty(giftConfig, "TriggerGiftConfigUnitWithCountList",
                new List<TriggerGiftConfigUnit>
                {
                    MakeGiftUnit("火箭", giftStub),
                    MakeGiftUnit("星星", giftStub)
                });

            // 仅挂 MainTrigger；PreTrigger 留空 → Trigger 内 ?. 空引用链不应抛异常
            LiveStreamTestReflection.SetField(collection, "_mainTriggerCommentConfigSo", commentConfig);
            LiveStreamTestReflection.SetField(collection, "_mainTriggerLikeConfigSo", likeConfig);
            LiveStreamTestReflection.SetField(collection, "_mainTriggerGiftConfigSo", giftConfig);
            return collection;
        }

        private TriggerGiftConfigUnit MakeGiftUnit(string giftName, LiveTriggerGiftBase trigger)
        {
            var unit = new TriggerGiftConfigUnit();
            LiveStreamTestReflection.SetField(unit, "_giftName", giftName);
            LiveStreamTestReflection.SetField(unit, "_giftIndex", 0);
            LiveStreamTestReflection.SetField(unit, "_giftCount", 0);
            LiveStreamTestReflection.SetField(unit, "_trigger", trigger);
            return unit;
        }

        private static void AssertEventNotSubscribed(UIPanelLiveStreamConsole panel)
        {
            Assert.That(LiveStreamTestReflection.GetField(panel, "CommentRequested"), Is.Null,
                "校验失败后不应订阅 CommentRequested");
            Assert.That(LiveStreamTestReflection.GetField(panel, "LikeRequested"), Is.Null,
                "校验失败后不应订阅 LikeRequested");
            Assert.That(LiveStreamTestReflection.GetField(panel, "GiftRequested"), Is.Null,
                "校验失败后不应订阅 GiftRequested");
        }

        private static void AssertEventSubscribed(UIPanelLiveStreamConsole panel)
        {
            Assert.That(LiveStreamTestReflection.GetField(panel, "CommentRequested"), Is.Not.Null,
                "Start 成功后应订阅 CommentRequested");
            Assert.That(LiveStreamTestReflection.GetField(panel, "LikeRequested"), Is.Not.Null,
                "Start 成功后应订阅 LikeRequested");
            Assert.That(LiveStreamTestReflection.GetField(panel, "GiftRequested"), Is.Not.Null,
                "Start 成功后应订阅 GiftRequested");
        }

        private static void AssertGift(
            UserDataGift gift, string userId, string nickname, string giftName, int giftIndex, int giftCount)
        {
            Assert.That(gift.UserId, Is.EqualTo(userId));
            Assert.That(gift.Nickname, Is.EqualTo(nickname));
            Assert.That(gift.GiftName, Is.EqualTo(giftName), "GiftName 应取配置单元名称");
            Assert.That(gift.GiftIndex, Is.EqualTo(giftIndex), "GiftIndex 应取选中下标");
            Assert.That(gift.GiftCount, Is.EqualTo(giftCount), "GiftCount 应取条目数量输入");
        }

        private void Track(GameObject go) => _createdGo.Add(go);

        private T TrackSo<T>(T so) where T : ScriptableObject
        {
            _createdSo.Add(so);
            return so;
        }

        private static UserDataUnit MakeUser(string userId, string nickname) =>
            new UserDataUnit { UserId = userId, Nickname = nickname, AvatarUrl = $"http://a/{userId}.png" };

        private static GiftDataUnit MakeGift(string name, int price) =>
            new GiftDataUnit { Name = name, Price = price, ImgUrl = $"http://i/{name}.png" };

        #endregion
    }
}
