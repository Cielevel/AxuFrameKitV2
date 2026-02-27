namespace AxuFrameKit.Modules.Config.Interface
{
    /// <summary>
    /// 配置存储策略接口
    /// 负责将数据写入具体存储介质
    /// </summary>
    public interface IConfigStorage
    {
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <param name="data">序列化后的字符串数据</param>
        void Write(string key, string data);

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <returns>存储的字符串数据，不存在返回 null</returns>
        string Read(string key);

        /// <summary>
        /// 删除配置
        /// </summary>
        /// <param name="key">配置键名</param>
        void Delete(string key);

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        /// <param name="key">配置键名</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);

        /// <summary>
        /// 获取所有配置键名
        /// </summary>
        /// <returns>键名集合</returns>
        string[] GetAllKeys();

        /// <summary>
        /// 清空所有配置
        /// </summary>
        void Clear();
    }
}
