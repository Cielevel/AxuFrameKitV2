using System;
using System.IO;
using System.Text;
using UnityEngine;
using AxuFrameKit.Common.Utility.Serializer;

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
    /// 加密接口：负责数据的加密与解密处理 (可选)
    /// </summary>
    public interface ICrypto
    {
        string Encrypt(string input);
        string Decrypt(string input);
    }

    // ==========================================
    // 2. 核心工具类 (The Core Class)
    // ==========================================

    /// <summary>
    /// SaveUtil (非静态)
    /// 这是一个纯粹的逻辑协调者，它不关心具体怎么存、怎么转Json，全靠注入的模块工作。
    /// </summary>
    public class SaveUtil
    {
        private readonly ISerializer _serializer;
        private readonly IStorage _storage;
        private readonly ICrypto _crypto; // 可以为空

        // 构造函数注入所有依赖
        public SaveUtil(ISerializer serializer, IStorage storage, ICrypto crypto = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _crypto = crypto;
        }

        /// <summary>
        /// 存储数据
        /// 流程：对象 -> 序列化 -> (加密) -> 存储
        /// </summary>
        public void Save<T>(string key, T data)
        {
            // 1. 序列化
            string content = _serializer.Serialize(data);

            // 2. 加密 (如果有配置)
            if (_crypto != null)
            {
                content = _crypto.Encrypt(content);
            }

            // 3. 写入存储介质
            _storage.Write(key, content);
        }

        /// <summary>
        /// 加载数据
        /// 流程：读取 -> (解密) -> 反序列化 -> 对象
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            if (!_storage.Exists(key))
            {
                return defaultValue;
            }

            // 1. 读取
            string content = _storage.Read(key);

            // 2. 解密 (如果有配置)
            if (_crypto != null)
            {
                try
                {
                    content = _crypto.Decrypt(content);
                }
                catch (Exception)
                {
                    Debug.LogError($"[SaveUtil] Decrypt failed for key: {key}");
                    return defaultValue;
                }
            }

            // 3. 反序列化
            try
            {
                return _serializer.Deserialize<T>(content);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveUtil] Deserialize failed: {e.Message}");
                return defaultValue;
            }
        }

        public void Delete(string key)
        {
            _storage.Delete(key);
        }
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
        private ICrypto _crypto;

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

        public SaveUtilBuilder UseEncryption(ICrypto crypto)
        {
            _crypto = crypto;
            return this;
        }

        public SaveUtil Build()
        {
            // 默认检查
            if (_serializer == null) throw new Exception("Serializer not set!");
            if (_storage == null) throw new Exception("Storage mechanism not set!");

            return new SaveUtil(_serializer, _storage, _crypto);
        }
    }
}