namespace AxuFrameKit._Core.Utility.Encrypt.Core
{
    /// <summary>
    /// 非对称加密接口
    /// </summary>
    public interface IAsymmetricCrypto
    {
        /// <summary>
        /// 生成密钥对
        /// </summary>
        /// <param name="keySize">密钥大小（位），默认2048</param>
        /// <returns>包含公钥和私钥的元组</returns>
        (string publicKey, string privateKey) GenerateKeyPair(int keySize = 2048);

        /// <summary>
        /// 使用公钥加密
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="publicKey">公钥</param>
        /// <returns>加密后的数据</returns>
        byte[] Encrypt(byte[] data, string publicKey);

        /// <summary>
        /// 使用私钥解密
        /// </summary>
        /// <param name="cipherData">加密数据</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>解密后的原始数据</returns>
        byte[] Decrypt(byte[] cipherData, string privateKey);

        /// <summary>
        /// 使用私钥签名
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>签名数据</returns>
        byte[] Sign(byte[] data, string privateKey);

        /// <summary>
        /// 使用公钥验证签名
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="signature">签名数据</param>
        /// <param name="publicKey">公钥</param>
        /// <returns>验证是否通过</returns>
        bool Verify(byte[] data, byte[] signature, string publicKey);
    }
}
