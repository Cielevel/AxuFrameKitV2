namespace AxuFrameKit.Modules.Config.Interface
{
    /// <summary>
    /// 配置加密策略接口
    /// 负责数据的加密与解密
    /// </summary>
    public interface IConfigCrypto
    {
        /// <summary>
        /// 加密数据
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns>密文</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// 解密数据
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <returns>明文</returns>
        string Decrypt(string cipherText);
    }
}
