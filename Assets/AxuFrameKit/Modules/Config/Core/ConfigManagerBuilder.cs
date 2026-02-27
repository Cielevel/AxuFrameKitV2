using System;
using AxuFrameKit.Modules.Config.Interface;
using AxuFrameKit.Modules.Config.Implementation.Storage;
using AxuFrameKit.Modules.Config.Implementation.Serializer;
using AxuFrameKit.Modules.Config.Implementation.Crypto;

namespace AxuFrameKit.Modules.Config.Core
{
    /// <summary>
    /// ConfigManager 构建器
    /// 使用 Builder 模式灵活组合配置策略
    /// </summary>
    public class ConfigManagerBuilder
    {
        private IConfigStorage _storage;
        private IConfigSerializer _serializer;
        private IConfigCrypto _crypto;
        private string _keyPrefix;

        #region 存储策略

        /// <summary>
        /// 使用自定义存储策略
        /// </summary>
        /// <param name="storage">存储实现</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseStorage(IConfigStorage storage)
        {
            _storage = storage;
            return this;
        }

        /// <summary>
        /// 使用 PlayerPrefs 存储
        /// 适合小型配置数据
        /// </summary>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UsePlayerPrefs()
        {
            _storage = new PlayerPrefsStorage();
            return this;
        }

        /// <summary>
        /// 使用 JSON 文件存储
        /// 适合需要可读性的配置
        /// </summary>
        /// <param name="directory">存储目录，null 则使用 persistentDataPath</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseJsonFile(string directory = null, string extension = ".json")
        {
            _storage = new JsonFileStorage(directory, extension);
            return this;
        }

        /// <summary>
        /// 使用二进制文件存储
        /// 适合需要紧凑存储的配置
        /// </summary>
        /// <param name="directory">存储目录，null 则使用 persistentDataPath</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseBinaryFile(string directory = null, string extension = ".dat")
        {
            _storage = new BinaryFileStorage(directory, extension);
            return this;
        }

        #endregion

        #region 序列化策略

        /// <summary>
        /// 使用自定义序列化策略
        /// </summary>
        /// <param name="serializer">序列化实现</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseSerializer(IConfigSerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        /// <summary>
        /// 使用 Unity JsonUtility 序列化器
        /// </summary>
        /// <param name="prettyPrint">是否美化输出</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseUnityJsonSerializer(bool prettyPrint = false)
        {
            _serializer = new UnityJsonSerializer(prettyPrint);
            return this;
        }

        /// <summary>
        /// 使用二进制序列化器
        /// </summary>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseBinarySerializer()
        {
            _serializer = new BinarySerializer();
            return this;
        }

        #endregion

        #region 加密策略

        /// <summary>
        /// 使用自定义加密策略
        /// </summary>
        /// <param name="crypto">加密实现</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseCrypto(IConfigCrypto crypto)
        {
            _crypto = crypto;
            return this;
        }

        /// <summary>
        /// 使用简单异或加密
        /// 适用于防简单篡改场景
        /// </summary>
        /// <param name="key">加密密钥</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseXorCrypto(string key)
        {
            _crypto = new XorCrypto(key);
            return this;
        }

        /// <summary>
        /// 不使用加密
        /// </summary>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder NoCrypto()
        {
            _crypto = null;
            return this;
        }

        #endregion

        #region 其他配置

        /// <summary>
        /// 设置键名前缀
        /// 用于区分不同配置管理器的数据
        /// </summary>
        /// <param name="prefix">前缀字符串</param>
        /// <returns>Builder 实例</returns>
        public ConfigManagerBuilder UseKeyPrefix(string prefix)
        {
            _keyPrefix = prefix;
            return this;
        }

        #endregion

        #region 构建

        /// <summary>
        /// 构建 ConfigManager 实例
        /// </summary>
        /// <returns>ConfigManager 实例</returns>
        /// <exception cref="InvalidOperationException">未配置必要的策略时抛出</exception>
        public ConfigManager Build()
        {
            if (_storage == null)
            {
                throw new InvalidOperationException(
                    "Storage strategy is required. Call UseStorage() or UsePlayerPrefs() etc.");
            }

            if (_serializer == null)
            {
                throw new InvalidOperationException(
                    "Serializer strategy is required. Call UseSerializer() or UseUnityJsonSerializer() etc.");
            }

            return new ConfigManager(_storage, _serializer, _crypto, _keyPrefix);
        }

        /// <summary>
        /// 创建默认配置管理器（PlayerPrefs + Unity JSON）
        /// </summary>
        /// <param name="keyPrefix">键名前缀（可选）</param>
        /// <returns>ConfigManager 实例</returns>
        public static ConfigManager CreateDefault(string keyPrefix = "")
        {
            return new ConfigManagerBuilder()
                .UsePlayerPrefs()
                .UseUnityJsonSerializer()
                .UseKeyPrefix(keyPrefix)
                .Build();
        }

        #endregion
    }
}
