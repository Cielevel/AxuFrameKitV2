namespace AxuFrameKit.Common.Utility.Crypto
{
    /// <summary>
    /// 加密接口：负责数据的加密与解密处理
    /// </summary>
    public interface ICrypto
    {
        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="input">原始数据</param>
        /// <returns>加密后的数据</returns>
        string Encrypt(string input);

        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="input">加密的数据</param>
        /// <returns>解密后的数据</returns>
        string Decrypt(string input);
    }
}
