using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Implementation.Storage
{
    /// <summary>
    /// PlayerPrefs 存储实现
    /// 适合小型配置数据，使用 Unity 内置的 PlayerPrefs
    /// </summary>
    public class PlayerPrefsStorage : IConfigStorage
    {
        private readonly string _keyRegistryKey;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="keyRegistryKey">用于记录所有键的特殊键名</param>
        public PlayerPrefsStorage(string keyRegistryKey = "__config_keys__")
        {
            _keyRegistryKey = keyRegistryKey;
        }

        /// <inheritdoc/>
        public void Write(string key, string data)
        {
            PlayerPrefs.SetString(key, data);
            RegisterKey(key);
            PlayerPrefs.Save();
        }

        /// <inheritdoc/>
        public string Read(string key)
        {
            return PlayerPrefs.GetString(key, null);
        }

        /// <inheritdoc/>
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            UnregisterKey(key);
            PlayerPrefs.Save();
        }

        /// <inheritdoc/>
        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <inheritdoc/>
        public string[] GetAllKeys()
        {
            string keysData = PlayerPrefs.GetString(_keyRegistryKey, "");
            if (string.IsNullOrEmpty(keysData))
            {
                return new string[0];
            }
            return keysData.Split(';');
        }

        /// <inheritdoc/>
        public void Clear()
        {
            var keys = GetAllKeys();
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.DeleteKey(_keyRegistryKey);
            PlayerPrefs.Save();
        }

        private void RegisterKey(string key)
        {
            var keys = new HashSet<string>(GetAllKeys());
            if (keys.Add(key))
            {
                PlayerPrefs.SetString(_keyRegistryKey, string.Join(";", keys));
            }
        }

        private void UnregisterKey(string key)
        {
            var keys = new List<string>(GetAllKeys());
            keys.Remove(key);
            PlayerPrefs.SetString(_keyRegistryKey, string.Join(";", keys));
        }
    }
}
