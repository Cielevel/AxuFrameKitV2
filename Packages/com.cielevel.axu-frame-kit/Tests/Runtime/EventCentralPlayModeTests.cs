using System.Collections;
using AxuFrameKit.Common.Utility.Event;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] EventCentral 字符串/泛型事件中心：发布订阅、手动退订、宿主对象销毁生命周期退订。
    /// 使用局部 EventCentral 实例（非静态 Default），避免跨测试静态残留。
    /// </summary>
    [Category("Axu/Play")]
    public class EventCentralPlayModeTests
    {
        private sealed class TestEvt
        {
            public int V;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            yield return AxuWait.ForFrames(1);
        }

        [UnityTest]
        public IEnumerator EventCentral_发布订阅_退订后不再收到()
        {
            // Arrange
            var central = new EventCentral();
            int got = 0;
            var handle = central.Register<TestEvt>(e => got += e.V);

            // Act：发布 → 收到；退订 → 再发布
            central.Send(new TestEvt { V = 4 });
            Assert.That(got, Is.EqualTo(4));
            handle.UnRegister();
            central.Send(new TestEvt { V = 6 });

            // Assert
            Assert.That(got, Is.EqualTo(4), "手动退订后不应再收到事件");
            yield break;
        }

        [UnityTest]
        public IEnumerator EventCentral_宿主对象销毁_生命周期自动退订()
        {
            // Arrange：订阅挂接到宿主 GameObject 的销毁触发器
            var central = new EventCentral();
            var host = new GameObject("evt-host");
            int got = 0;
            central.Register<TestEvt>(e => got += e.V).UnRegisterWhenGameObjectDestroyed(host);

            // Act：销毁前收到；销毁后（帧末 OnDestroy）再发布
            central.Send(new TestEvt { V = 2 });
            Assert.That(got, Is.EqualTo(2));

            Object.Destroy(host);
            yield return null; // 帧末执行销毁与 OnDestroy

            central.Send(new TestEvt { V = 3 });

            // Assert
            Assert.That(got, Is.EqualTo(2), "宿主销毁后订阅应已自动注销");
        }
    }
}
