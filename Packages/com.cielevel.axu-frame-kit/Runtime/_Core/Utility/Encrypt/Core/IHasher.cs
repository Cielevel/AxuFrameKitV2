namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 哈希算法接口
    /// </summary>
    public interface IHasher
    {
        /// <summary>
        /// 计算字节数组的哈希值
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>哈希值字节数组</returns>
        byte[] ComputeHash(byte[] data);

        /// <summary>
        /// 计算字符串的哈希值
        /// </summary>
        /// <param name="input">原始字符串</param>
        /// <returns>哈希值十六进制字符串（小写）</returns>
        string ComputeHash(string input);

        /// <summary>
        /// 计算文件的哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>哈希值字节数组</returns>
        byte[] ComputeFileHash(string filePath);
    }
}
