using System;
using UnityEngine;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Implementation.Serializer
{
    /// <summary>
    /// Unity JsonUtility 序列化器实现
    /// 使用 Unity 内置的 JsonUtility 进行序列化
    /// </summary>
    public class UnityJsonSerializer : IConfigSerializer
    {
        private readonly bool _prettyPrint;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prettyPrint">是否美化输出（便于阅读）</param>
        public UnityJsonSerializer(bool prettyPrint = false)
        {
            _prettyPrint = prettyPrint;
        }

        /// <inheritdoc/>
        public string Serialize<T>(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[UnityJsonSerializer] 尝试序列化 null 对象");
                return string.Empty;
            }

            try
            {
                return JsonUtility.ToJson(obj, _prettyPrint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityJsonSerializer] 序列化失败: {ex.Message}");
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
                return JsonUtility.FromJson<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityJsonSerializer] 反序列化失败: {ex.Message}");
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
                result = JsonUtility.FromJson<T>(data);
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
