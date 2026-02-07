namespace AxuFrameKit.Common.Utility.Serializer
{
    /// <summary>
    /// 序列化器接口，用于对象的序列化和反序列化
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 将对象序列化为字符串
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        string Serialize(object obj);

        /// <summary>
        /// 将字符串反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">要反序列化的字符串</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(string data);

        /// <summary>
        /// 尝试将字符串反序列化为指定类型的对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">要反序列化的字符串</param>
        /// <param name="result">反序列化后的对象</param>
        /// <returns>是否成功反序列化</returns>
        bool TryDeserialize<T>(string data, out T result);
    }
}
