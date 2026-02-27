using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Core
{
    /// <summary>
    /// 配置管理器 - 核心协调者
    /// 负责配置的读取、写入、缓存和变更通知
    /// </summary>
    public class ConfigManager
    {
        private readonly IConfigStorage _storage;
        private readonly IConfigSerializer _serializer;
        private readonly IConfigCrypto _crypto;
        private readonly Dictionary<string, object> _cache;
        private readonly Dictionary<string, Delegate> _callbacks;
        private readonly string _keyPrefix;

        /// <summary>
        /// 构造函数（依赖注入）
        /// </summary>
        /// <param name="storage">存储策略</param>
        /// <param name="serializer">序列化策略</param>
        /// <param name="crypto">加密策略（可选）</param>
        /// <param name="keyPrefix">键名前缀（可选）</param>
        public ConfigManager(
            IConfigStorage storage,
            IConfigSerializer serializer,
            IConfigCrypto crypto = null,
            string keyPrefix = "")
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _crypto = crypto;
            _keyPrefix = keyPrefix ?? string.Empty;
            _cache = new Dictionary<string, object>();
            _callbacks = new Dictionary<string, Delegate>();
        }

        #region 基础读写操作

        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            string fullKey = GetFullKey(key);

            // 1. 检查缓存
            if (_cache.TryGetValue(fullKey, out var cachedValue))
            {
                return (T)cachedValue;
            }

            // 2. 从存储读取
            if (!_storage.Exists(fullKey))
            {
                return defaultValue;
            }

            // 3. 读取并解密
            string data = _storage.Read(fullKey);
            if (string.IsNullOrEmpty(data))
            {
                return defaultValue;
            }

            if (_crypto != null)
            {
                try
                {
                    data = _crypto.Decrypt(data);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ConfigManager] 解密失败: {key}, {ex.Message}");
                    return defaultValue;
                }
            }

            // 4. 反序列化
            if (!_serializer.TryDeserialize<T>(data, out var result))
            {
                Debug.LogWarning($"[ConfigManager] 反序列化失败: {key}");
                return defaultValue;
            }

            // 5. 缓存并返回
            _cache[fullKey] = result;
            return result;
        }

        /// <summary>
        /// 设置配置值（立即保存）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键名</param>
        /// <param name="value">配置值</param>
        public void Set<T>(string key, T value)
        {
            string fullKey = GetFullKey(key);

            // 1. 获取旧值用于回调
            T oldValue = _cache.TryGetValue(fullKey, out var cached) ? (T)cached : default;

            // 2. 更新缓存
            _cache[fullKey] = value;

            // 3. 序列化
            string data = _serializer.Serialize(value);

            // 4. 加密（如果配置了）
            if (_crypto != null)
            {
                data = _crypto.Encrypt(data);
            }

            // 5. 写入存储
            _storage.Write(fullKey, data);

            // 6. 触发回调
            TriggerCallback(fullKey, oldValue, value);
        }

        /// <summary>
        /// 设置配置值（延迟保存，仅更新缓存）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键名</param>
        /// <param name="value">配置值</param>
        public void SetDelayed<T>(string key, T value)
        {
            string fullKey = GetFullKey(key);
            T oldValue = _cache.TryGetValue(fullKey, out var cached) ? (T)cached : default;
            _cache[fullKey] = value;
            TriggerCallback(fullKey, oldValue, value);
        }

        /// <summary>
        /// 保存所有缓存的配置
        /// </summary>
        public void SaveAll()
        {
            foreach (var kvp in _cache)
            {
                string data = _serializer.Serialize(kvp.Value);
                if (_crypto != null)
                {
                    data = _crypto.Encrypt(data);
                }
                _storage.Write(kvp.Key, data);
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        /// <param name="key">配置键名</param>
        public void Delete(string key)
        {
            string fullKey = GetFullKey(key);
            _storage.Delete(fullKey);
            _cache.Remove(fullKey);
            _callbacks.Remove(fullKey);
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <returns>是否存在</returns>
        public bool Exists(string key)
        {
            string fullKey = GetFullKey(key);
            return _cache.ContainsKey(fullKey) || _storage.Exists(fullKey);
        }

        /// <summary>
        /// 清空所有配置
        /// </summary>
        public void Clear()
        {
            _storage.Clear();
            _cache.Clear();
            _callbacks.Clear();
        }

        #endregion

        #region 变更监听

        /// <summary>
        /// 注册值变更回调
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键名</param>
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
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键名</param>
        /// <param name="callback">回调函数</param>
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
        /// 获取所有配置键
        /// </summary>
        /// <returns>键名集合</returns>
        public string[] GetAllKeys()
        {
            return _storage.GetAllKeys();
        }

        /// <summary>
        /// 批量设置配置（延迟保存后统一写入）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="configs">配置字典</param>
        public void SetBatch<T>(Dictionary<string, T> configs)
        {
            if (configs == null) return;

            foreach (var kvp in configs)
            {
                SetDelayed(kvp.Key, kvp.Value);
            }
            SaveAll();
        }

        #endregion

        private string GetFullKey(string key)
        {
            return string.IsNullOrEmpty(_keyPrefix) ? key : $"{_keyPrefix}.{key}";
        }
    }
}
