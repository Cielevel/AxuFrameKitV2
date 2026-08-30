using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Structure;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Smoke] _Core Structure 层冒烟：PriorityQueue / IndexedList / ReactiveProperty
    /// （BiDictionary、CircularBuffer 已由 Assets/Tests/Editor 工程冒烟覆盖，此处不重复）。
    /// </summary>
    [Category("Axu/Smoke")]
    public class CoreStructureSmokeTests
    {
        [Test]
        public void PriorityQueue_乱序入队_按优先级升序出队()
        {
            // Arrange
            var pq = new PriorityQueue<int>();
            foreach (var n in new[] { 5, 1, 4, 1, 3 })
                pq.Enqueue(n);

            // Assert：Peek / Count / Contains
            Assert.That(pq.Peek(), Is.EqualTo(1));
            Assert.That(pq.Count, Is.EqualTo(5));
            Assert.That(pq.Contains(4), Is.True);

            // Act：逐个出队
            var ordered = new List<int>();
            while (pq.Count > 0)
                ordered.Add(pq.Dequeue());

            // Assert：最小堆语义（值越小越先出，重复值保留）
            CollectionAssert.AreEqual(new[] { 1, 1, 3, 4, 5 }, ordered);

            // 空队列 Dequeue / Peek 抛 InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => pq.Dequeue());
            Assert.Throws<InvalidOperationException>(() => pq.Peek());
        }

        [Test]
        public void IndexedList_重复Add拒绝_移除后索引同步()
        {
            // Arrange
            var list = new IndexedList<string>();

            // Act & Assert：Add 与去重
            Assert.That(list.Add("a"), Is.True);
            Assert.That(list.Add("b"), Is.True);
            Assert.That(list.Add("c"), Is.True);
            Assert.That(list.Add("a"), Is.False, "重复元素应被拒绝");
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.IndexOf("c"), Is.EqualTo(2));

            // 无序移除：尾元素补位到被删位置，索引同步（O(1) 语义，顺序不保证）
            Assert.That(list.Remove("a"), Is.True);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("c"));
            Assert.That(list.IndexOf("c"), Is.EqualTo(0));
            Assert.That(list.IndexOf("b"), Is.EqualTo(1));

            // 有序移除：顺序保持
            var ordered = new IndexedList<string>();
            ordered.Add("x");
            ordered.Add("y");
            ordered.Add("z");
            Assert.That(ordered.RemoveOrdered("y"), Is.True);
            Assert.That(ordered[0], Is.EqualTo("x"));
            Assert.That(ordered[1], Is.EqualTo("z"));
            Assert.That(ordered.Contains("y"), Is.False);
        }

        [Test]
        public void ReactiveProperty_值变更通知_Dispose退订后不再通知()
        {
            // Arrange
            var rp = new ReactiveProperty<int>(10);
            var received = new List<int>();

            // Act：订阅（立即触发一次）
            var handle = rp.Subscribe(received.Add);
            rp.Value = 20;
            rp.Value = 20; // 相同值不通知
            handle.Dispose();
            rp.Value = 30; // 退订后不再通知

            // Assert
            CollectionAssert.AreEqual(new[] { 10, 20 }, received);
            Assert.That(rp.Value, Is.EqualTo(30));
        }
    }
}
