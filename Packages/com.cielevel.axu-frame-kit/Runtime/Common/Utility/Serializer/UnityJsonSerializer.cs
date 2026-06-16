using System;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Serializer
{
    /// <summary>
    /// Unity内置JSON序列化器实现
    /// 使用Unity的JsonUtility进行序列化和反序列化
    /// </summary>
    public class UnityJsonSerializer : ISerializer
    {
        /// <summary>
        /// 是否使用美化格式（带缩进）
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prettyPrint">是否使用美化格式，默认为false</param>
        public UnityJsonSerializer(bool prettyPrint = false)
        {
            PrettyPrint = prettyPrint;
        }

        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的JSON字符串</returns>
        public string Serialize(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[UnityJsonSerializer] 尝试序列化null对象");
                return string.Empty;
            }

            try
            {
                return JsonUtility.ToJson(obj, PrettyPrint);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityJsonSerializer] 序列化失败: {ex.Message}\n类型: {obj.GetType().Name}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 将JSON字符串反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">要反序列化的JSON字符串</param>
        /// <returns>反序列化后的对象</returns>
        public T Deserialize<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[UnityJsonSerializer] 尝试反序列化空字符串");
                return default;
            }

            try
            {
                return JsonUtility.FromJson<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityJsonSerializer] 反序列化失败: {ex.Message}\n目标类型: {typeof(T).Name}\n数据: {data}");
                return default;
            }
        }

        /// <summary>
        /// 尝试将JSON字符串反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">要反序列化的JSON字符串</param>
        /// <param name="result">反序列化后的对象</param>
        /// <returns>是否成功反序列化</returns>
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
