using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Save;
using AxuFrameKit.Common.Utility.Serializer;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] SaveUtil 策略化存档：内存 IStorage 后端下的存取、变更回调、删除与回退值。
    /// 使用测试内 MemoryStorage，不触碰 PlayerPrefs / 磁盘。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class CommonSaveUtilTests
    {
        [System.Serializable]
        public sealed class PlayerData
        {
            public int Level;
            public string Name;
        }

        /// <summary>纯内存存储后端（实现 IStorage + IStorageExtended）。</summary>
        private sealed class MemoryStorage : IStorage, IStorageExtended
        {
            private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

            public void Write(string key, string data) => _data[key] = data;
            public string Read(string key) => _data.TryGetValue(key, out var v) ? v : null;
            public void Delete(string key) => _data.Remove(key);
            public bool Exists(string key) => _data.ContainsKey(key);
            public string[] GetAllKeys() => new List<string>(_data.Keys).ToArray();
            public void Clear() => _data.Clear();
        }

        [Test]
        public void SaveUtil_内存后端_存取回调与删除()
        {
            // Arrange
            var storage = new MemoryStorage();
            var save = new SaveUtilBuilder()
                .UseSerializer(new UnityJsonSerializer())
                .UseStorage(storage)
                .UseKeyPrefix("axutest")
                .Build();

            int callbackCount = 0;
            save.RegisterCallback<PlayerData>("player", (oldValue, newValue) => callbackCount++);

            // Act & Assert：Save 触发回调并落库（含键前缀）
            save.Save("player", new PlayerData { Level = 7, Name = "阿修" });
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(storage.Exists("axutest.player"), Is.True, "键前缀应作用于存储键");

            // Load 往返
            var loaded = save.Load<PlayerData>("player");
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Level, Is.EqualTo(7));
            Assert.That(loaded.Name, Is.EqualTo("阿修"));

            // Delete：缓存与存储同步清理，Load 回退到默认值
            save.Delete("player");
            Assert.That(save.Exists("player"), Is.False);
            Assert.That(storage.Exists("axutest.player"), Is.False);
            var fallback = save.Load("player", new PlayerData { Level = 1, Name = "默认" });
            Assert.That(fallback.Level, Is.EqualTo(1));
            Assert.That(fallback.Name, Is.EqualTo("默认"));
        }
    }
}
