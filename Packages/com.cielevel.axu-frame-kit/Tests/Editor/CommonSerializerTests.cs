using AxuFrameKit.Common.Utility.Serializer;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] 序列化后端往返：UnityJsonSerializer（JsonUtility）与 BinarySerializer（Base64(JSON)）。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class CommonSerializerTests
    {
        [System.Serializable]
        public sealed class SampleData
        {
            public int Level;
            public string Name;
            public bool IsActive;
        }

        private static SampleData CreateSample()
        {
            return new SampleData { Level = 9, Name = "阿修", IsActive = true };
        }

        [Test]
        public void UnityJsonSerializer_对象序列化往返_字段一致()
        {
            // Arrange
            var serializer = new UnityJsonSerializer();
            var sample = CreateSample();

            // Act
            var json = serializer.Serialize(sample);
            var restored = serializer.Deserialize<SampleData>(json);

            // Assert
            Assert.That(json, Is.Not.Empty);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Level, Is.EqualTo(9));
            Assert.That(restored.Name, Is.EqualTo("阿修"));
            Assert.That(restored.IsActive, Is.True);
        }

        [Test]
        public void BinarySerializer_对象序列化往返_字段一致()
        {
            // Arrange
            var serializer = new BinarySerializer();
            var sample = CreateSample();

            // Act
            var payload = serializer.Serialize(sample);
            var restored = serializer.Deserialize<SampleData>(payload);

            // Assert：负载为 Base64(JSON)，解码后与原对象一致
            Assert.That(payload, Is.Not.Empty);
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Level, Is.EqualTo(9));
            Assert.That(restored.Name, Is.EqualTo("阿修"));
            Assert.That(restored.IsActive, Is.True);
            // Base64 合法性：解码不抛异常
            Assert.That(() => System.Convert.FromBase64String(payload), Throws.Nothing);
        }
    }
}
