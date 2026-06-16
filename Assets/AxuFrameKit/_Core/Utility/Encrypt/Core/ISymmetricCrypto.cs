namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 对称加密结果
    /// </summary>
    public class SymmetricCryptoResult
    {
        /// <summary>加密后的数据</summary>
        public byte[] CipherData { get; set; }

        /// <summary>使用的初始化向量</summary>
        public byte[] IV { get; set; }
    }

    /// <summary>
    /// 对称加密接口
    /// </summary>
    public interface ISymmetricCrypto
    {
        /// <summary>
        /// 加密字节数组
        /// </summary>
        /// <param name="plainData">原始数据</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始化向量（可选，不提供则自动生成）</param>
        /// <returns>加密结果（包含密文和IV）</returns>
        SymmetricCryptoResult Encrypt(byte[] plainData, byte[] key, byte[] iv = null);

        /// <summary>
        /// 解密字节数组
        /// </summary>
        /// <param name="cipherData">加密数据</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns>解密后的原始数据</returns>
        byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv);

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="plainText">原始字符串</param>
        /// <param name="key">密钥字符串</param>
        /// <returns>Base64编码的加密结果（IV + 密文）</returns>
        string EncryptString(string plainText, string key);

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="cipherText">Base64编码的加密数据</param>
        /// <param name="key">密钥字符串</param>
        /// <returns>解密后的原始字符串</returns>
        string DecryptString(string cipherText, string key);

        /// <summary>
        /// 生成随机密钥
        /// </summary>
        byte[] GenerateKey();

        /// <summary>
        /// 生成随机IV
        /// </summary>
        byte[] GenerateIV();
    }
}
