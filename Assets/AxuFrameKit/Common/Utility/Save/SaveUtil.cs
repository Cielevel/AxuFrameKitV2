using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using AxuFrameKit.Common.Utility.Serializer;
using AxuFrameKit._Core.Utility.Encrypt.Core;
using AxuFrameKit._Core.Utility.Encrypt.Symmetric;

namespace AxuFrameKit.Common.Utility.Save
{
    // ==========================================
    // 1. 核心接口定义 (The Abstractions)
    // ==========================================

    /// <summary>
    /// 存储介质接口：负责将数据写入具体位置
    /// 外部可以实现为 本地文件, PlayerPrefs, 数据库, 或 云端API
    /// </summary>
    public interface IStorage
    {
        void Write(string key, string data);
        string Read(string key);
        void Delete(string key);
        bool Exists(string key);
    }

    /// <summary>
    /// 可选的扩展接口，包含枚举与清空能力。
    /// 现有实现如果不支持可选择不实现；SaveUtil 会在运行时检测并安全降级。
    /// </summary>
    public interface IStorageExtended
    {
        /// <summary>
        /// 获取当前存储所有键名（不包含前缀）。
        /// </summary>
        string[] GetAllKeys();

        /// <summary>
        /// 清空所有存储的数据（不包含缓存/回调）。
        /// </summary>
        void Clear();
    }

    // ==========================================
    // 2. 核心工具类 (The Core Class)
    // ==========================================

    /// <summary>
    /// SaveUtil (非静态)
    /// 这是一个纯粹的逻辑协调者，它不关心具体怎么存、怎么转Json，全靠注入的模块工作。
    /// 支持缓存、变更监听、延迟保存、批量操作等高级功能。
    /// </summary>
    public class SaveUtil
    {
        private readonly ISerializer _serializer;
        private readonly IStorage _storage;
        private readonly ISymmetricCrypto _crypto; // 可以为空
        private readonly Dictionary<string, object> _cache;
        private readonly Dictionary<string, Delegate> _callbacks;
        private readonly string _keyPrefix;
        private readonly string _cryptoKey;

        // 构造函数注入所有依赖
        public SaveUtil(ISerializer serializer, IStorage storage, ISymmetricCrypto crypto = null, string cryptoKey = "", string keyPrefix = "")
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _crypto = crypto;
            _cryptoKey = cryptoKey ?? string.Empty;
            _keyPrefix = keyPrefix ?? string.Empty;
            _cache = new Dictionary<string, object>();
            _callbacks = new Dictionary<string, Delegate>();
        }

        private string GetFullKey(string key)
        {
            return string.IsNullOrEmpty(_keyPrefix) ? key : $"{_keyPrefix}.{key}";
        }

        #region 基础读写操作

        /// <summary>
        /// 加载数据（带缓存）
        /// 流程：检查缓存 -> 读取 -> (解密) -> 反序列化 -> 缓存 -> 对象
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            string fullKey = GetFullKey(key);

            // 1. 检查缓存
            if (_cache.TryGetValue(fullKey, out var cachedValue))
            {
                return (T)cachedValue;
            }

            // 2. 检查存储是否存在
            if (!_storage.Exists(fullKey))
            {
                return defaultValue;
            }

            // 3. 读取
            string content = _storage.Read(fullKey);

            // 4. 解密 (如果有配置)
            if (_crypto != null && !string.IsNullOrEmpty(_cryptoKey))
            {
                try
                {
                    content = _crypto.DecryptString(content, _cryptoKey);
                }
                catch (Exception)
                {
                    Debug.LogError($"[SaveUtil] Decrypt failed for key: {key}");
                    return defaultValue;
                }
            }

            // 5. 反序列化
            T result;
            try
            {
                result = _serializer.Deserialize<T>(content);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveUtil] Deserialize failed: {e.Message}");
                return defaultValue;
            }

            // 6. 缓存并返回
            _cache[fullKey] = result;
            return result;
        }

        /// <summary>
        /// 存储数据（立即保存，带回调和缓存）
        /// 流程：对象 -> 序列化 -> (加密) -> 存储 -> 触发回调
        /// </summary>
        public void Save<T>(string key, T data)
        {
            string fullKey = GetFullKey(key);

            // 1. 获取旧值用于回调
            T oldValue = _cache.TryGetValue(fullKey, out var cached) ? (T)cached : default;

            // 2. 更新缓存
            _cache[fullKey] = data;

            // 3. 序列化
            string content = _serializer.Serialize(data);

            // 4. 加密 (如果有配置)
            if (_crypto != null && !string.IsNullOrEmpty(_cryptoKey))
            {
                content = _crypto.EncryptString(content, _cryptoKey);
            }

            // 5. 写入存储介质
            _storage.Write(fullKey, content);

            // 6. 触发回调
            TriggerCallback(fullKey, oldValue, data);
        }

        /// <summary>
        /// 延迟保存（仅更新缓存，不立即写入存储）
        /// </summary>
        public void SaveDelayed<T>(string key, T data)
        {
            string fullKey = GetFullKey(key);
            T oldValue = _cache.TryGetValue(fullKey, out var cached) ? (T)cached : default;
            _cache[fullKey] = data;
            TriggerCallback(fullKey, oldValue, data);
        }

        /// <summary>
        /// 保存所有缓存的配置到存储
        /// </summary>
        public void SaveAll()
        {
            foreach (var kvp in _cache)
            {
                string content = _serializer.Serialize(kvp.Value);
                if (_crypto != null && !string.IsNullOrEmpty(_cryptoKey))
                {
                    content = _crypto.EncryptString(content, _cryptoKey);
                }
                _storage.Write(kvp.Key, content);
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        public void Delete(string key)
        {
            string fullKey = GetFullKey(key);
            _storage.Delete(fullKey);
            _cache.Remove(fullKey);
            _callbacks.Remove(fullKey);
        }

        /// <summary>
        /// 检查数据是否存在（优先检查缓存）
        /// </summary>
        public bool Exists(string key)
        {
            string fullKey = GetFullKey(key);
            return _cache.ContainsKey(fullKey) || _storage.Exists(fullKey);
        }

        /// <summary>
        /// 清空缓存（不删除存储数据）
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 清空所有（缓存 + 回调）
        /// </summary>
        public void ClearAll()
        {
            _cache.Clear();
            _callbacks.Clear();
        }

        #endregion

        #region 变更监听

        /// <summary>
        /// 注册值变更回调
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="callback">回调函数 (oldValue, newValue)</param>
        public void RegisterCallback<T>(string key, Action<T, T> callback)
        {
            string fullKey = GetFullKey(key);
            if (_callbacks.TryGetValue(fullKey, out var existing))
            {
                _callbacks[fullKey] = Delegate.Combine(existing, callback);
            }
            else
            {
                _callbacks[fullKey] = callback;
            }
        }

        /// <summary>
        /// 取消注册值变更回调
        /// </summary>
        public void UnregisterCallback<T>(string key, Action<T, T> callback)
        {
            string fullKey = GetFullKey(key);
            if (_callbacks.TryGetValue(fullKey, out var existing))
            {
                _callbacks[fullKey] = Delegate.Remove(existing, callback);
            }
        }

        private void TriggerCallback<T>(string fullKey, T oldValue, T newValue)
        {
            if (_callbacks.TryGetValue(fullKey, out var callback))
            {
                (callback as Action<T, T>)?.Invoke(oldValue, newValue);
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量设置数据（延迟保存后统一写入）
        /// </summary>
        public void SaveBatch<T>(Dictionary<string, T> data)
        {
            if (data == null) return;

            foreach (var kvp in data)
            {
                SaveDelayed(kvp.Key, kvp.Value);
            }
            SaveAll();
        }

        #endregion

        #region 额外功能扩展

        /// <summary>
        /// 如果底层存储支持，返回所有键名。
        /// 否则返回空数组。
        /// </summary>
        public string[] GetAllKeys()
        {
            if (_storage is IStorageExtended ext)
                return ext.GetAllKeys();
            return new string[0];
        }

        /// <summary>
        /// 清空整个存储（调用底层 Clear）并清理缓存/回调。
        /// </summary>
        public void Clear()
        {
            if (_storage is IStorageExtended ext)
            {
                try
                {
                    ext.Clear();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveUtil] Clear storage failed: {e.Message}");
                }
            }

            // 同步清除本地缓存和回调
            ClearAll();
        }

        #endregion
    }

    // ==========================================
    // 3. 构造器 (Builder Pattern)
    // ==========================================

    /// <summary>
    /// 用于方便地创建 SaveUtil 实例
    /// </summary>
    public class SaveUtilBuilder
    {
        private ISerializer _serializer;
        private IStorage _storage;
        private ISymmetricCrypto _crypto;
        private string _cryptoKey;
        private string _keyPrefix;

        #region 核心设定

        public SaveUtilBuilder UseSerializer(ISerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        public SaveUtilBuilder UseStorage(IStorage storage)
        {
            _storage = storage;
            return this;
        }

        public SaveUtilBuilder UseEncryption(ISymmetricCrypto crypto, string cryptoKey)
        {
            _crypto = crypto;
            _cryptoKey = cryptoKey;
            return this;
        }

        public SaveUtilBuilder UseKeyPrefix(string prefix)
        {
            _keyPrefix = prefix;
            return this;
        }

        #endregion

        #region 便捷策略 (与 ConfigManagerBuilder 保持一致)

        public SaveUtilBuilder UsePlayerPrefs(string keyRegistryKey = "__save_keys__")
        {
            _storage = new PlayerPrefsStorage(keyRegistryKey);
            return this;
        }

        public SaveUtilBuilder UseJsonFile(string directory = null, string extension = ".json")
        {
            _storage = new JsonFileStorage(directory, extension);
            return this;
        }

        public SaveUtilBuilder UseBinaryFile(string directory = null, string extension = ".dat")
        {
            _storage = new BinaryFileStorage(directory, extension);
            return this;
        }

        public SaveUtilBuilder UseUnityJsonSerializer(bool prettyPrint = false)
        {
            _serializer = new UnityJsonSerializer(prettyPrint);
            return this;
        }

        public SaveUtilBuilder UseBinarySerializer()
        {
            _serializer = new BinarySerializer();
            return this;
        }

        public SaveUtilBuilder UseXorCrypto(string key)
        {
            _crypto = new XorCrypto();
            _cryptoKey = key;
            return this;
        }

        public SaveUtilBuilder NoCrypto()
        {
            _crypto = null;
            _cryptoKey = null;
            return this;
        }

        #endregion

        public SaveUtil Build()
        {
            // 默认检查
            if (_serializer == null) throw new Exception("Serializer not set!");
            if (_storage == null) throw new Exception("Storage mechanism not set!");

            return new SaveUtil(_serializer, _storage, _crypto, _cryptoKey, _keyPrefix);
        }

        /// <summary>
        /// 创建默认的 SaveUtil（PlayerPrefs + Unity JSON）
        /// </summary>
        public static SaveUtil CreateDefault(string keyPrefix = "")
        {
            return new SaveUtilBuilder()
                .UsePlayerPrefs()
                .UseUnityJsonSerializer()
                .UseKeyPrefix(keyPrefix)
                .Build();
        }
    }
}