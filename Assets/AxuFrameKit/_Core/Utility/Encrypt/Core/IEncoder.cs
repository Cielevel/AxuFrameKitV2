namespace AxuFrameKit._Core.Utility.Encrypt.Core
{
    /// <summary>
    /// 编码器接口
    /// </summary>
    public interface IEncoder
    {
        /// <summary>
        /// 编码字节数组为字符串
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>编码后的字符串</returns>
        string Encode(byte[] data);

        /// <summary>
        /// 解码字符串为字节数组
        /// </summary>
        /// <param name="encoded">编码后的字符串</param>
        /// <returns>原始字节数组</returns>
        byte[] Decode(string encoded);
    }
}
