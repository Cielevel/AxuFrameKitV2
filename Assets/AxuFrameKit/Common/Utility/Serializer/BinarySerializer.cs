using System;
using System.Text;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Serializer
{
    /// <summary>
    /// 二进制序列化器实现
    /// 将对象序列化为Base64编码的JSON字符串，提供更紧凑的存储格式
    /// </summary>
    public class BinarySerializer : ISerializer
    {
        /// <summary>
        /// 将对象序列化为Base64编码的JSON字符串
        /// </summary>
        public string Serialize(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[BinarySerializer] 尝试序列化null对象");
                return string.Empty;
            }

            try
            {
                string json = JsonUtility.ToJson(obj, false);
                byte[] data = Encoding.UTF8.GetBytes(json);
                return Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializer] 序列化失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 反序列化Base64编码的JSON字符串
        /// </summary>
        public T Deserialize<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            try
            {
                byte[] decodedData = Convert.FromBase64String(data);
                string json = Encoding.UTF8.GetString(decodedData);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializer] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 尝试反序列化
        /// </summary>
        public bool TryDeserialize<T>(string data, out T result)
        {
            try
            {
                result = Deserialize<T>(data);
                return result != null;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
