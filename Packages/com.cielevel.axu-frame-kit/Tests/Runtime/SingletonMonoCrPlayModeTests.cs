using System.Collections;
using AxuFrameKit.Common.Utility.Singleton;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] SingletonMonoCr&lt;T&gt; 自动创建单例：首次访问创建、同实例复用、销毁后访问自动重建。
    /// 测试专用类型；销毁动作登记进 AxuTestState，防止跨测试残留。
    /// </summary>
    [Category("Axu/Play")]
    public class SingletonMonoCrPlayModeTests
    {
        /// <summary>测试专用单例类型（避免污染任何产品单例）。</summary>
        private sealed class TestCrSingleton : SingletonMonoCr<TestCrSingleton>
        {
            public int Tag;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            AxuTestState.Register("Singleton/TestCrSingleton", DestroyExisting);
            yield return AxuWait.ForFrames(1);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyExisting();
            yield return null;
        }

        /// <summary>按约定名销毁单例 GameObject（幂等）。</summary>
        private static void DestroyExisting()
        {
            var go = GameObject.Find("[TestCrSingleton]");
            if (go != null)
                Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SingletonMonoCr_生命周期_首次访问自动创建销毁后重建()
        {
            // Act 1：首次访问 Instance
            var first = TestCrSingleton.Instance;
            yield return null;

            // Assert 1：自动创建的 GameObject 与实例复用
            Assert.That(first, Is.Not.Null);
            Assert.That(first.gameObject.name, Is.EqualTo("[TestCrSingleton]"));
            Assert.That(TestCrSingleton.Instance, Is.SameAs(first));

            // Act 2：销毁后再访问
            Object.Destroy(first.gameObject);
            yield return null; // 帧末销毁，静态 _instance 变为 Unity-null
            var second = TestCrSingleton.Instance;
            yield return null;

            // Assert 2：自动重建，且为新实例
            Assert.That(second, Is.Not.Null, "实例销毁后访问 Instance 应自动重建");
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.gameObject.activeInHierarchy, Is.True);
        }
    }
}
