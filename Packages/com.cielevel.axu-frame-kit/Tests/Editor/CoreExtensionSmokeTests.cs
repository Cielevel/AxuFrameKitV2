using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Extension;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Smoke] _Core 扩展方法族冒烟：StringExtension / CollectionExtension。
    /// </summary>
    [Category("Axu/Smoke")]
    public class CoreExtensionSmokeTests
    {
        [Test]
        public void StringExtension_Truncate_超过MaxLength_截断并追加后缀()
        {
            // Arrange & Act & Assert：超长 → 截断 + 默认后缀
            Assert.That("abcdefghij".Truncate(5), Is.EqualTo("abcde..."));
            // 未超长 → 原样返回
            Assert.That("abc".Truncate(5), Is.EqualTo("abc"));
            // 自定义后缀
            Assert.That("abcdefghij".Truncate(5, "…"), Is.EqualTo("abcde…"));
            // 空串安全
            Assert.That(string.Empty.Truncate(5), Is.EqualTo(string.Empty));
        }

        [Test]
        public void CollectionExtension_Chunk_按大小分块_余数成组()
        {
            // Arrange
            var source = Enumerable.Range(1, 7).ToList();

            // Act
            var chunks = source.Chunk(3).Select(c => c.ToList()).ToList();

            // Assert：完整块 + 余数组
            Assert.That(chunks, Has.Count.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, chunks[0]);
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, chunks[1]);
            CollectionAssert.AreEqual(new[] { 7 }, chunks[2]);
        }
    }
}
