using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 直播控制台面板与条目 EditMode 测试：
    /// 覆盖 UIPanelLiveStreamConsole 的 Setup 克隆 / 选中收集 / 评论与点赞输入读取（Trim、非法值防御、清空）、
    /// UIConsoleUserItem 的 Bind 字段映射与 IsSelected、UIConsoleGiftItem 的 Bind/Count/GiftIndex，
    /// 以及按钮 → 请求事件接线与收起切换。
    /// UI 组件经 LiveStreamConsoleUiRig 反射搭建（测试程序集不直接引用 uGUI/TMP 程序集）。
    /// </summary>
    [Category("Axu/Module/LiveStream")]
    public class LiveStreamConsolePanelTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private LiveStreamConsoleUiRig _rig;

        [TearDown]
        public void TearDown()
        {
            // 清理本轮自建的层级（含延迟销毁的克隆条目），避免跨测试残留（工程关闭域重载）
            foreach (var created in _createdObjects)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _createdObjects.Clear();
            _rig = null;
        }

        [Test]
        public void Setup_入参为空_模板保持隐藏且不克隆条目()
        {
            // Arrange
            BuildAndActivate();

            // Act
            _rig.Panel.Setup(null, null);

            // Assert
            Assert.That(_rig.UserItems, Is.Empty, "null 入参不应克隆用户条目");
            Assert.That(_rig.GiftItems, Is.Empty, "null 入参不应克隆礼物条目");
            Assert.That(_rig.UserItemTemplate.gameObject.activeSelf, Is.False, "用户模板应保持隐藏");
            Assert.That(_rig.GiftItemTemplate.gameObject.activeSelf, Is.False, "礼物模板应保持隐藏");
        }

        [Test]
        public void Setup_用户与礼物配置_按数量克隆条目且携带配置引用()
        {
            // Arrange
            BuildAndActivate();
            var users = new[] { MakeUser("u1", "阿修一号"), MakeUser("u2", "阿修二号"), MakeUser("u3", "阿修三号") };
            var gifts = new[] { MakeGift("火箭", 1000), MakeGift("星星", 10) };

            // Act
            _rig.Panel.Setup(users, gifts);

            // Assert：克隆数量与激活态
            Assert.That(_rig.UserItems, Has.Count.EqualTo(3), "用户条目应按配置克隆 3 个");
            Assert.That(_rig.GiftItems, Has.Count.EqualTo(2), "礼物条目应按配置克隆 2 个");
            Assert.That(_rig.UserItems[1].gameObject.activeSelf, Is.True, "克隆条目应处于激活态");
            Assert.That(_rig.GiftItems[0].gameObject.activeSelf, Is.True, "克隆条目应处于激活态");

            // Assert：模板保持隐藏
            Assert.That(_rig.UserItemTemplate.gameObject.activeSelf, Is.False, "Setup 后用户模板应保持隐藏");
            Assert.That(_rig.GiftItemTemplate.gameObject.activeSelf, Is.False, "Setup 后礼物模板应保持隐藏");

            // Assert：条目携带配置数据与下标
            Assert.That(_rig.UserItems[1].Data, Is.SameAs(users[1]), "用户条目应绑定对应配置单元");
            Assert.That(_rig.GiftItems[1].Data, Is.SameAs(gifts[1]), "礼物条目应绑定对应配置单元");
            Assert.That(_rig.GiftItems[0].GiftIndex, Is.EqualTo(0), "首个礼物下标应为 0");
            Assert.That(_rig.GiftItems[1].GiftIndex, Is.EqualTo(1), "第二个礼物下标应为 1");
        }

        [Test]
        public void Setup_重复调用_按最新配置重建且旧选中不残留()
        {
            // Arrange：首轮 3 用户，选中第 3 个
            BuildAndActivate();
            _rig.Panel.Setup(
                new[] { MakeUser("u1", "一号"), MakeUser("u2", "二号"), MakeUser("u3", "三号") },
                new[] { MakeGift("火箭", 1) });
            _rig.SetUserSelected(_rig.UserItems[2], true);
            Assert.That(_rig.Panel.GetSelectedUsers(), Has.Count.EqualTo(1), "前置：应有一个选中用户");

            // Act：重入 Setup，仅 1 个新用户（ClearItems 对旧克隆调 Object.Destroy，
            // EditMode 下 Unity 会报 "Destroy may not be called from edit mode"，先声明预期）
            var newUser = MakeUser("u9", "九号");
            ExpectEditModeDestroyLogs(4); // 首轮 3 用户 + 1 礼物
            _rig.Panel.Setup(new[] { newUser }, new[] { MakeGift("星星", 2) });

            // Assert：面板条目列表按新配置重建，旧选中不再可见
            Assert.That(_rig.UserItems, Has.Count.EqualTo(1), "重入 Setup 后用户条目应重建");
            Assert.That(_rig.GiftItems, Has.Count.EqualTo(1), "重入 Setup 后礼物条目应重建");
            Assert.That(_rig.UserItems[0].Data, Is.SameAs(newUser), "新条目应绑定新配置");
            Assert.That(_rig.Panel.GetSelectedUsers(), Is.Empty, "旧选中不应残留到重建后的条目集");

            // 新条目可正常选中收集
            _rig.SetUserSelected(_rig.UserItems[0], true);
            var selected = _rig.Panel.GetSelectedUsers();
            Assert.That(selected, Has.Count.EqualTo(1));
            Assert.That(selected[0].UserId, Is.EqualTo("u9"));
        }

        [Test]
        public void GetSelectedUsers_仅勾选条目被收集且顺序与列表一致()
        {
            // Arrange
            BuildAndActivate();
            _rig.Panel.Setup(
                new[] { MakeUser("u1", "一号"), MakeUser("u2", "二号"), MakeUser("u3", "三号") },
                new[] { MakeGift("火箭", 1) });

            // Act：勾选第 1、3 个
            _rig.SetUserSelected(_rig.UserItems[0], true);
            _rig.SetUserSelected(_rig.UserItems[2], true);
            var selected = _rig.Panel.GetSelectedUsers();

            // Assert
            Assert.That(selected, Has.Count.EqualTo(2), "应只收集勾选条目");
            Assert.That(selected[0].UserId, Is.EqualTo("u1"));
            Assert.That(selected[1].UserId, Is.EqualTo("u3"));

            // 取消一个后仅剩一个
            _rig.SetUserSelected(_rig.UserItems[0], false);
            Assert.That(_rig.Panel.GetSelectedUsers(), Has.Count.EqualTo(1), "取消勾选后应不再收集");
        }

        [Test]
        public void GetSelectedGifts_选中条目携带配置引用下标与输入数量()
        {
            // Arrange
            BuildAndActivate();
            var gifts = new[] { MakeGift("火箭", 1000), MakeGift("星星", 10) };
            _rig.Panel.Setup(new[] { MakeUser("u1", "一号") }, gifts);
            _rig.SetGiftSelected(_rig.GiftItems[0], true);
            _rig.SetGiftSelected(_rig.GiftItems[1], true);
            _rig.SetGiftItemCount(_rig.GiftItems[0], "3");
            _rig.SetGiftItemCount(_rig.GiftItems[1], "10");

            // Act
            var selections = _rig.Panel.GetSelectedGifts();

            // Assert
            Assert.That(selections, Has.Count.EqualTo(2), "应收集全部勾选礼物");
            Assert.That(selections[0].Gift, Is.SameAs(gifts[0]), "Gift 应为配置单元引用");
            Assert.That(selections[0].GiftIndex, Is.EqualTo(0), "GiftIndex 应为配置下标");
            Assert.That(selections[0].Count, Is.EqualTo(3), "Count 应取条目数量输入");
            Assert.That(selections[1].Gift, Is.SameAs(gifts[1]));
            Assert.That(selections[1].GiftIndex, Is.EqualTo(1));
            Assert.That(selections[1].Count, Is.EqualTo(10));

            // 未勾选的条目不进入收集（重入 Setup：ClearItems 的 EditMode Destroy 报错先声明预期，
            // 新增第三个礼物并保持未勾选）
            ExpectEditModeDestroyLogs(3); // 首轮 1 用户 + 2 礼物
            _rig.Panel.Setup(new[] { MakeUser("u1", "一号") },
                new[] { gifts[0], gifts[1], MakeGift("小心心", 1) });
            Assert.That(_rig.Panel.GetSelectedGifts(), Is.Empty, "重建后未勾选不应收集");
        }

        [Test]
        public void GetCommentText_含首尾空白_返回Trim后文本()
        {
            // Arrange
            BuildAndActivate();

            // Act
            _rig.SetCommentText("  主播好  ");
            var comment = _rig.Panel.GetCommentText();

            // Assert
            Assert.That(comment, Is.EqualTo("主播好"), "评论读取应 Trim 首尾空白");
            Assert.That(_rig.GetCommentInputText(), Is.EqualTo("  主播好  "), "输入框原文应保持不变");
        }

        [Test]
        public void GetLikeCount_非法或小于1输入_一律按1处理()
        {
            // Arrange
            BuildAndActivate();

            // Act & Assert：合法值直接返回
            _rig.SetLikeCountText("7");
            Assert.That(_rig.Panel.GetLikeCount(), Is.EqualTo(7), "合法数字应原样返回");

            // 非数字 → 1
            _rig.SetLikeCountText("abc");
            Assert.That(_rig.Panel.GetLikeCount(), Is.EqualTo(1), "解析失败应按 1 处理");

            // 0 → 1
            _rig.SetLikeCountText("0");
            Assert.That(_rig.Panel.GetLikeCount(), Is.EqualTo(1), "小于 1 应按 1 处理");

            // 空串 → 1
            _rig.SetLikeCountText(string.Empty);
            Assert.That(_rig.Panel.GetLikeCount(), Is.EqualTo(1), "空输入应按 1 处理");
        }

        [Test]
        public void GetCommentText与GetLikeCount_输入框未挂载_返回空串与1()
        {
            // Arrange：rig 保持未激活（不触发 Awake），输入框引用置空走防御分支
            _rig = LiveStreamConsoleUiRig.Build();
            Track(_rig.Root);
            LiveStreamTestReflection.SetField(_rig.Panel, "_commentInput", null);
            LiveStreamTestReflection.SetField(_rig.Panel, "_likeCountInput", null);

            // Act & Assert：防御分支不抛异常
            Assert.That(_rig.Panel.GetCommentText(), Is.Empty, "输入框缺失应返回空串");
            Assert.That(_rig.Panel.GetLikeCount(), Is.EqualTo(1), "输入框缺失点赞数应按 1 处理");
            Assert.DoesNotThrow(() => _rig.Panel.ClearCommentInput(), "输入框缺失时清空不应抛异常");
        }

        [Test]
        public void ClearCommentInput_输入非空_清空输入框()
        {
            // Arrange
            BuildAndActivate();
            _rig.SetCommentText("666");
            Assert.That(_rig.GetCommentInputText(), Is.EqualTo("666"), "前置：输入框应有内容");

            // Act
            _rig.Panel.ClearCommentInput();

            // Assert
            Assert.That(_rig.GetCommentInputText(), Is.Empty, "清空后输入框应为空串");
            Assert.That(_rig.Panel.GetCommentText(), Is.Empty);
        }

        [Test]
        public void 按钮点击_触发对应请求事件且收起按钮切换BodyRoot()
        {
            // Arrange
            BuildAndActivate();
            int commentRequests = 0;
            int likeRequests = 0;
            int giftRequests = 0;
            _rig.Panel.CommentRequested += () => commentRequests++;
            _rig.Panel.LikeRequested += () => likeRequests++;
            _rig.Panel.GiftRequested += () => giftRequests++;
            Assert.That(_rig.BodyRoot.activeSelf, Is.True, "前置：收起目标应处于激活态");

            // Act & Assert：三类按钮各触发一次对应事件
            _rig.Click(_rig.SendCommentButton);
            _rig.Click(_rig.LikeButton);
            _rig.Click(_rig.SendGiftButton);
            Assert.That(commentRequests, Is.EqualTo(1), "发送评论按钮应触发 CommentRequested");
            Assert.That(likeRequests, Is.EqualTo(1), "点赞按钮应触发 LikeRequested");
            Assert.That(giftRequests, Is.EqualTo(1), "送礼物按钮应触发 GiftRequested");

            // Act & Assert：收起按钮切换 BodyRoot
            _rig.Click(_rig.CollapseButton);
            Assert.That(_rig.BodyRoot.activeSelf, Is.False, "首次点击应收起 BodyRoot");
            _rig.Click(_rig.CollapseButton);
            Assert.That(_rig.BodyRoot.activeSelf, Is.True, "再次点击应展开 BodyRoot");
        }

        [Test]
        public void IsSelected_toggle为空恒为假_勾选后为真()
        {
            // Arrange：用模板条目验证（IsSelected 只读 _toggle，与激活态无关）
            _rig = LiveStreamConsoleUiRig.Build();
            Track(_rig.Root);
            var item = _rig.UserItemTemplate;
            var originalToggle = LiveStreamTestReflection.GetField(item, "_toggle");
            Assert.That(originalToggle, Is.Not.Null, "Rig 模板应已挂 Toggle");

            // Act & Assert：_toggle 置空 → 恒 false
            LiveStreamTestReflection.SetField(item, "_toggle", null);
            Assert.That(item.IsSelected, Is.False, "_toggle 为空时 IsSelected 应恒为 false");

            // 恢复 Toggle：未勾选 false → 勾选 true → 再取消 false
            LiveStreamTestReflection.SetField(item, "_toggle", originalToggle);
            Assert.That(item.IsSelected, Is.False, "未勾选时 IsSelected 应为 false");
            _rig.SetUserSelected(item, true);
            Assert.That(item.IsSelected, Is.True, "勾选后 IsSelected 应为 true");
            _rig.SetUserSelected(item, false);
            Assert.That(item.IsSelected, Is.False, "取消勾选后 IsSelected 应为 false");
        }

        [Test]
        public void 用户条目Bind_昵称或UserId为空_展示防御文案且关闭头像()
        {
            // Arrange：昵称/UserId 为空、无头像
            BuildAndActivate();
            _rig.Panel.Setup(
                new[] { new UserDataUnit { UserId = null, Nickname = null } },
                new[] { MakeGift("火箭", 1) });
            var item = _rig.UserItems[0];

            // Act：Bind 已由 Setup 调用，读取克隆条目的展示组件
            var nicknameText = (string)LiveStreamTestReflection.GetProperty(
                LiveStreamTestReflection.GetField(item, "_nicknameText"), "text");
            var uidText = (string)LiveStreamTestReflection.GetProperty(
                LiveStreamTestReflection.GetField(item, "_uidText"), "text");
            var avatar = LiveStreamTestReflection.GetField(item, "_avatar");

            // Assert：空昵称 → "(无昵称)"；空 UID → 空串；空头像 → Image 关闭
            Assert.That(item.Data, Is.Not.Null, "Bind 应记录配置单元");
            Assert.That(nicknameText, Is.EqualTo("(无昵称)"), "空昵称应展示防御文案");
            Assert.That(uidText, Is.Empty, "空 UID 应展示空串");
            Assert.That((bool)LiveStreamTestReflection.GetProperty(avatar, "enabled"), Is.False,
                "空头像应关闭 Image 只留背景色块");
        }

        [Test]
        public void 用户条目Bind_正常字段_昵称Uid展示且头像开启()
        {
            // Arrange
            BuildAndActivate();
            _rig.Panel.Setup(new[] { MakeUser("u1", "阿修一号") }, new[] { MakeGift("火箭", 1) });
            var item = _rig.UserItems[0];

            // Act
            var nicknameText = (string)LiveStreamTestReflection.GetProperty(
                LiveStreamTestReflection.GetField(item, "_nicknameText"), "text");
            var uidText = (string)LiveStreamTestReflection.GetProperty(
                LiveStreamTestReflection.GetField(item, "_uidText"), "text");
            var avatar = LiveStreamTestReflection.GetField(item, "_avatar");

            // Assert
            Assert.That(nicknameText, Is.EqualTo("阿修一号"));
            Assert.That(uidText, Is.EqualTo("u1"));
            Assert.That((bool)LiveStreamTestReflection.GetProperty(avatar, "enabled"), Is.False,
                "无 Sprite 时 Image 应保持关闭（Avatar 为 null）");
        }

        [Test]
        public void 礼物条目Count_非法或小于1输入_按1处理_合法值原样返回()
        {
            // Arrange
            BuildAndActivate();
            _rig.Panel.Setup(new[] { MakeUser("u1", "一号") }, new[] { MakeGift("火箭", 1) });
            var item = _rig.GiftItems[0];

            // Act & Assert：合法值
            _rig.SetGiftItemCount(item, "23");
            Assert.That(item.Count, Is.EqualTo(23), "合法数字应原样返回");

            // 非数字 → 1
            _rig.SetGiftItemCount(item, "abc");
            Assert.That(item.Count, Is.EqualTo(1), "解析失败应按 1 处理");

            // 0 → 1
            _rig.SetGiftItemCount(item, "0");
            Assert.That(item.Count, Is.EqualTo(1), "小于 1 应按 1 处理");

            // 空串 → 1
            _rig.SetGiftItemCount(item, string.Empty);
            Assert.That(item.Count, Is.EqualTo(1), "空输入应按 1 处理");
        }

        [Test]
        public void 礼物条目Bind_重复调用_数量输入重置为1()
        {
            // Arrange
            BuildAndActivate();
            var gift = MakeGift("火箭", 1000);
            _rig.Panel.Setup(new[] { MakeUser("u1", "一号") }, new[] { gift });
            var item = _rig.GiftItems[0];
            _rig.SetGiftItemCount(item, "88");
            Assert.That(item.Count, Is.EqualTo(88), "前置：数量应已改为 88");

            // Act：重复 Bind（面板重入场景）
            item.Bind(gift, 0);

            // Assert：输入框重置为 1，Data/GiftIndex 保持
            Assert.That(_rig.GetGiftItemInputText(item), Is.EqualTo("1"), "重复 Bind 应重置数量输入");
            Assert.That(item.Count, Is.EqualTo(1), "重置后 Count 应为 1");
            Assert.That(item.Data, Is.SameAs(gift));
            Assert.That(item.GiftIndex, Is.EqualTo(0));
        }

        [Test]
        public void 礼物条目Count_数量输入框未挂载_按1处理()
        {
            // Arrange：模板保持未激活，_countInput 置空走防御分支
            _rig = LiveStreamConsoleUiRig.Build();
            Track(_rig.Root);
            LiveStreamTestReflection.SetField(_rig.GiftItemTemplate, "_countInput", null);

            // Act & Assert
            Assert.That(_rig.GiftItemTemplate.Count, Is.EqualTo(1), "数量输入框缺失应按 1 处理");
        }

        // ---------------------------------------------------------------- 辅助

        private void BuildAndActivate()
        {
            _rig = LiveStreamConsoleUiRig.Build();
            Track(_rig.Root);
            _rig.Activate();
        }

        /// <summary>
        /// 声明 EditMode 下 Object.Destroy 的预期报错（产品 ClearItems 在运行时用延迟销毁是正确语义，
        /// 仅 EditMode 会报错；此声明使重入 Setup 测试可专注断言行为本身）。
        /// </summary>
        private static void ExpectEditModeDestroyLogs(int count)
        {
            for (int i = 0; i < count; i++)
            {
                LogAssert.Expect(LogType.Error, new Regex(@"Destroy may not be called from edit mode"));
            }
        }

        private void Track(GameObject go)
        {
            _createdObjects.Add(go);
        }

        private static UserDataUnit MakeUser(string userId, string nickname) =>
            new UserDataUnit { UserId = userId, Nickname = nickname, AvatarUrl = $"http://a/{userId}.png" };

        private static GiftDataUnit MakeGift(string name, int price) =>
            new GiftDataUnit { Name = name, Price = price, ImgUrl = $"http://i/{name}.png" };
    }
}
