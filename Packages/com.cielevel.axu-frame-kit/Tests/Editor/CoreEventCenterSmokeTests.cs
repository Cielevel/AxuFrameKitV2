using AxuFrameKit._Core.Advanced.EventSystem;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Smoke] _Core 类型安全事件中心（EventCenter）冒烟：订阅 / 发布 / 退订。
    /// 使用局部实例（非 EventCenter.Default），测试间天然隔离。
    /// </summary>
    [Category("Axu/Smoke")]
    public class CoreEventCenterSmokeTests
    {
        private sealed class TestEvent
        {
            public int Value;
        }

        [Test]
        public void EventCenter_发布_订阅者收到事件数据()
        {
            // Arrange：两个订阅者
            var ec = new EventCenter();
            int sum = 0;
            ec.Subscribe<TestEvent>(e => sum += e.Value);
            ec.Subscribe<TestEvent>(e => sum += 100);

            // Act & Assert：带数据发布
            ec.Publish(new TestEvent { Value = 5 });
            Assert.That(sum, Is.EqualTo(105));

            // 无参泛型发布（new T()，Value=0）同样送达：sum = 105 + 0 + 100 = 205
            ec.Publish<TestEvent>();
            Assert.That(sum, Is.EqualTo(205));
        }

        [Test]
        public void EventCenter_句柄Dispose退订_不再收到()
        {
            // Arrange
            var ec = new EventCenter();
            int count = 0;
            var handle = ec.Subscribe<TestEvent>(_ => count++);

            // Act & Assert：退订前收到
            ec.Publish(new TestEvent());
            Assert.That(count, Is.EqualTo(1));

            // 退订后不再收到
            handle.Dispose();
            ec.Publish(new TestEvent());
            Assert.That(count, Is.EqualTo(1));
        }
    }
}
