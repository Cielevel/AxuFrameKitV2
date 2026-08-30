using System;
using AxuFrameKit._Core.Utility.Computed;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] Computed&lt;T&gt; 响应式派生值：惰性求值、上游失效重算、连锁失效、Dispose 语义。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreComputedTests
    {
        [Test]
        public void Computed_上游源变更_惰性重算与连锁失效()
        {
            // Arrange：source → doubled → quadrupled 两级派生
            var src = new ComputedSource<int>(2);
            int evalCount = 0;
            var doubled = new Computed<int>(() =>
            {
                evalCount++;
                return src.Value * 2;
            });
            var quadrupled = new Computed<int>(() => doubled.Value * 2);

            // Act & Assert：首次读取触发一次求值，重复读取命中缓存
            Assert.That(quadrupled.Value, Is.EqualTo(8));
            Assert.That(doubled.Value, Is.EqualTo(4));
            Assert.That(evalCount, Is.EqualTo(1), "重复读取应命中缓存，只求值一次");

            // 上游变更 → 失效；读取时惰性重算
            src.Value = 5;
            Assert.That(evalCount, Is.EqualTo(1), "变更只置脏，读取前不重算");
            Assert.That(doubled.Value, Is.EqualTo(10));
            Assert.That(evalCount, Is.EqualTo(2));
            Assert.That(quadrupled.Value, Is.EqualTo(20), "上游失效应经中间层连锁传播");

            // Dispose 后读取抛 ObjectDisposedException
            doubled.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = doubled.Value);
        }
    }
}
