using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Implementation.Serializer
{
    /// <summary>
    /// 二进制序列化器实现
    /// 使用 .NET BinaryFormatter 进行序列化，并通过 Base64 编码转为字符串
    /// </summary>
    public class BinarySerializer : IConfigSerializer
    {
        /// <inheritdoc/>
        public string Serialize<T>(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[BinarySerializer] 尝试序列化 null 对象");
                return string.Empty;
            }

            try
            {
                var formatter = new BinaryFormatter();
                using var stream = new MemoryStream();
                formatter.Serialize(stream, obj);
                return Convert.ToBase64String(stream.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializer] 序列化失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public T Deserialize<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            try
            {
                var bytes = Convert.FromBase64String(data);
                var formatter = new BinaryFormatter();
                using var stream = new MemoryStream(bytes);
                return (T)formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializer] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        /// <inheritdoc/>
        public bool TryDeserialize<T>(string data, out T result)
        {
            result = default;

            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(data);
                var formatter = new BinaryFormatter();
                using var stream = new MemoryStream(bytes);
                result = (T)formatter.Deserialize(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
