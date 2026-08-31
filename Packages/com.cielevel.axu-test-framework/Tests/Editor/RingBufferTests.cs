using System.Collections.Generic;
using System.Linq;
using AxuTestFramework.Core.Metrics;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] RingBuffer&lt;T&gt; 环形缓冲 FIFO 淘汰语义（Phase 2 契约驱动测试）。
    /// 被测类型由并行实现落地，本套件签名即契约；实现若有出入，集成期以实现核对后同步修正。
    /// 关键契约点：
    ///  - RingBuffer&lt;T&gt;(int capacity)，Capacity/Count 只读；
    ///  - Add 入队；未满时按插入序可读；满后覆盖最旧（FIFO 淘汰），Count 恒不超容量；
    ///  - Items 按最旧 → 最新顺序枚举。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class RingBufferTests
    {
        [Test]
        public void Add_未满时_按插入顺序读取()
        {
            var buffer = new RingBuffer<int>(4);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Add_满后_最旧被淘汰且保留最新容量个元素()
        {
            var buffer = new RingBuffer<int>(3);
            foreach (var v in new[] { 1, 2, 3, 4, 5 }) buffer.Add(v);

            // 容量 3 写入 5 个 → 前两个（1、2）按 FIFO 淘汰
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 3, 4, 5 }));
        }

        [Test]
        public void Add_持续超容量写入_Count不超容量()
        {
            var buffer = new RingBuffer<int>(3);

            for (var i = 0; i < 10; i++) buffer.Add(i);

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.Count, Is.LessThanOrEqualTo(buffer.Capacity));
        }

        [Test]
        public void Items_从半满到溢出_枚举顺序恒为最旧到最新()
        {
            var buffer = new RingBuffer<int>(4);

            buffer.Add(1);
            buffer.Add(2);
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 1, 2 }));

            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5); // 淘汰 1
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 2, 3, 4, 5 }));
        }

        [Test]
        public void Add_容量1_仅保留最新元素()
        {
            var buffer = new RingBuffer<int>(1);

            buffer.Add(1);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 1 }));

            buffer.Add(2);
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.Items.ToList(), Is.EqualTo(new[] { 2 }));
        }
    }
}
