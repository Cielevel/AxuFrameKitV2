using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 配置 SO 只读访问器 EditMode 测试：
    /// 覆盖 UserConfigSo.Users / GiftConfigSo.Gifts 对私有序列化列表的只读视图暴露
    /// （元素同引用、数量一致），供运行时控制台面板读取。
    /// 列表经反射注入私有序列化字段（与既有测试纪律一致）。
    /// </summary>
    [Category("Axu/Module/LiveStream")]
    public class LiveStreamConfigSoAccessorTests
    {
        private readonly List<ScriptableObject> _createdSo = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var so in _createdSo)
            {
                if (so != null)
                {
                    Object.DestroyImmediate(so);
                }
            }

            _createdSo.Clear();
        }

        [Test]
        public void UserConfigSo_Users_返回配置列表的只读视图且元素同引用()
        {
            // Arrange
            var so = TrackSo(ScriptableObject.CreateInstance<UserConfigSo>());
            var users = new List<UserDataUnit>
            {
                new UserDataUnit { UserId = "u1", Nickname = "甲", AvatarUrl = "http://a/1.png" },
                new UserDataUnit { UserId = "u2", Nickname = "乙" }
            };
            LiveStreamTestReflection.SetField(so, "_userDataUnitList", users);

            // Act
            IReadOnlyList<UserDataUnit> view = so.Users;

            // Assert：同源列表、数量与元素一致
            Assert.That(view, Is.Not.Null, "配置后 Users 不应为 null");
            Assert.That(view, Is.SameAs(users), "Users 应为注入列表本身（只读接口视图）");
            Assert.That(view, Has.Count.EqualTo(2));
            Assert.That(view[0].UserId, Is.EqualTo("u1"));
            Assert.That(view[0].Nickname, Is.EqualTo("甲"));
            Assert.That(view[0].AvatarUrl, Is.EqualTo("http://a/1.png"));
            Assert.That(view[1].UserId, Is.EqualTo("u2"));
            Assert.That(view[1].Nickname, Is.EqualTo("乙"));
        }

        [Test]
        public void GiftConfigSo_Gifts_返回配置列表的只读视图且元素同引用()
        {
            // Arrange
            var so = TrackSo(ScriptableObject.CreateInstance<GiftConfigSo>());
            var gifts = new List<GiftDataUnit>
            {
                new GiftDataUnit { Name = "火箭", Price = 1000, ImgUrl = "http://i/rocket.png" },
                new GiftDataUnit { Name = "星星", Price = 10 },
                new GiftDataUnit { Name = "小心心", Price = 1 }
            };
            LiveStreamTestReflection.SetField(so, "_giftDataUnitList", gifts);

            // Act
            IReadOnlyList<GiftDataUnit> view = so.Gifts;

            // Assert：同源列表、数量与元素一致
            Assert.That(view, Is.Not.Null, "配置后 Gifts 不应为 null");
            Assert.That(view, Is.SameAs(gifts), "Gifts 应为注入列表本身（只读接口视图）");
            Assert.That(view, Has.Count.EqualTo(3));
            Assert.That(view[0].Name, Is.EqualTo("火箭"));
            Assert.That(view[0].Price, Is.EqualTo(1000));
            Assert.That(view[0].ImgUrl, Is.EqualTo("http://i/rocket.png"));
            Assert.That(view[1].Name, Is.EqualTo("星星"));
            Assert.That(view[2].Name, Is.EqualTo("小心心"));
        }

        private T TrackSo<T>(T so) where T : ScriptableObject
        {
            _createdSo.Add(so);
            return so;
        }
    }
}
