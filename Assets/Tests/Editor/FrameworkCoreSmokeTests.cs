using System;
using AxuFrameKit._Core.Structure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKitV2.Tests
{
    /// <summary>
    /// _Core 数据结构冒烟测试（EditMode）。
    /// </summary>
    public class FrameworkCoreSmokeTests
    {
        [Test]
        public void BiDictionary_ForwardAndReverseLookup()
        {
            var bd = new BiDictionary<int, string>();
            bd.Add(1, "one");
            bd.Add(2, "two");

            Assert.That(bd.Count, Is.EqualTo(2));
            Assert.That(bd.TryGetByKey(1, out var v), Is.True);
            Assert.That(v, Is.EqualTo("one"));
            Assert.That(bd.TryGetByValue("two", out var k), Is.True);
            Assert.That(k, Is.EqualTo(2));
        }

        [Test]
        public void BiDictionary_AddDuplicateKeyOrValue_Throws()
        {
            var bd = new BiDictionary<int, string>();
            bd.Add(1, "one");

            // Add 拒绝重复键/值时直接抛异常；不再自行记录日志，避免与调用方日志形成双重上报
            Assert.Throws<ArgumentException>(() => bd.Add(1, "uno"));
            Assert.Throws<ArgumentException>(() => bd.Add(11, "one"));
        }

        [Test]
        public void BiDictionary_SetRebindsBothDirections()
        {
            var bd = new BiDictionary<int, string>();
            bd.Add(1, "one");
            bd.Set(1, "uno");
            bd.Set(2, "one");

            Assert.That(bd.TryGetByKey(1, out var v), Is.True);
            Assert.That(v, Is.EqualTo("uno"));
            Assert.That(bd.TryGetByValue("one", out var k), Is.True);
            Assert.That(k, Is.EqualTo(2));
            Assert.That(bd.Count, Is.EqualTo(2));
        }

        [Test]
        public void BiDictionary_RemoveByKeyAndByValue()
        {
            var bd = new BiDictionary<int, string>();
            bd.Add(1, "one");
            Assert.That(bd.RemoveByValue("one"), Is.True);
            Assert.That(bd.Count, Is.EqualTo(0));
            Assert.That(bd.ContainsKey(1), Is.False);

            bd.Add(2, "two");
            Assert.That(bd.RemoveByKey(2), Is.True);
            Assert.That(bd.ContainsValue("two"), Is.False);
        }

        [Test]
        public void CircularBuffer_OverwritesOldestWhenFull()
        {
            var cb = new CircularBuffer<int>(3);
            foreach (var n in new[] { 1, 2, 3, 4, 5 })
                cb.Add(n);

            Assert.That(cb.Count, Is.EqualTo(3));
            Assert.That(cb.IsFull, Is.True);
            Assert.That(cb[0], Is.EqualTo(3));
            Assert.That(cb[1], Is.EqualTo(4));
            Assert.That(cb[2], Is.EqualTo(5));
            Assert.That(cb.PeekFirst(), Is.EqualTo(3));
            Assert.That(cb.PeekLast(), Is.EqualTo(5));
        }

        [Test]
        public void CircularBuffer_IndexerAndEnumeration()
        {
            var cb = new CircularBuffer<string>(2);
            cb.Add("a");
            cb.Add("b");
            Assert.That(cb.PeekFirst(), Is.EqualTo("a"));
            Assert.That(new[] { cb[0], cb[1] }, Is.EqualTo(new[] { "a", "b" }));

            cb.Clear();
            Assert.That(cb.IsEmpty, Is.True);
            Assert.Throws<InvalidOperationException>(() => cb.PeekFirst());
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = cb[0]);
        }
    }
}
