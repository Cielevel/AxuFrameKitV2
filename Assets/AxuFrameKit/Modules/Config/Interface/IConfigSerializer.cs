namespace AxuFrameKit.Modules.Config.Interface
{
    /// <summary>
    /// 配置序列化策略接口
    /// 负责对象与字符串之间的转换
    /// </summary>
    public interface IConfigSerializer
    {
        /// <summary>
        /// 序列化对象为字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>序列化后的字符串</returns>
        string Serialize<T>(T obj);

        /// <summary>
        /// 反序列化字符串为对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">序列化字符串</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(string data);

        /// <summary>
        /// 尝试反序列化
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">序列化字符串</param>
        /// <param name="result">反序列化结果</param>
        /// <returns>是否成功</returns>
        bool TryDeserialize<T>(string data, out T result);
    }
}
