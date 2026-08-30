using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] GameObjectStrPools：Prewarm / Get / Release 与归还复用。
    /// 通过反射注入私有序列化字段 _objectUnitList（PlayMode 程序集无 UnityEditor，不能用 SerializedObject），
    /// 全程零 SO 资产依赖，测试后彻底清理。
    /// </summary>
    [Category("Axu/Play")]
    public class GameObjectPoolPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            yield return AxuWait.ForFrames(1);
        }

        [UnityTest]
        public IEnumerator GameObjectStrPools_PrewarmGetRelease_归还后复用()
        {
            // Arrange：构造池配置（模板对象 = 场景中的 PoolableStr 组件）
            var pools = ScriptableObject.CreateInstance<GameObjectStrPools>();
            var prefabGo = new GameObject("poolable-prefab");
            var prefab = prefabGo.AddComponent<PoolableStr>();
            var rootGo = new GameObject("pool-root");

            var field = typeof(GameObjectPools<string>).GetField("_objectUnitList",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "GameObjectPools._objectUnitList 字段应存在（反射注入依赖）");
            field.SetValue(pools, new List<ObjectUnit<string>>
            {
                new ObjectUnit<string> { ObjID = "soldier", Prefab = prefab }
            });

            pools.Initialize(null, null, rootGo.transform);
            pools.Prewarm("soldier", 2);

            try
            {
                // Act：取出两个（应为不同实例），归还后再次取出
                var a = pools.Get("soldier");
                var b = pools.Get("soldier");

                // Assert：Get 正常、ObjId 正确、实例独立
                Assert.That(a, Is.Not.Null);
                Assert.That(b, Is.Not.Null);
                Assert.That(ReferenceEquals(a, b), Is.False, "Prewarm 后连续 Get 应得到不同实例");
                Assert.That(a.ObjId, Is.EqualTo("soldier"));

                a.Release();
                var c = pools.Get("soldier");

                Assert.That(c, Is.SameAs(a), "归还的实例应被池复用");
                c.Release();
            }
            finally
            {
                Object.Destroy(pools);
                Object.Destroy(prefabGo);
                Object.Destroy(rootGo);
            }

            yield break;
        }
    }
}
