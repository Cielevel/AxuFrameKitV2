using System;
using System.Text;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Implementation.Crypto
{
    /// <summary>
    /// 简单异或加密实现
    /// 适用于防简单篡改场景，不适用于高安全需求
    /// </summary>
    public class XorCrypto : IConfigCrypto
    {
        private readonly byte[] _keyBytes;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">加密密钥（不能为空）</param>
        /// <exception cref="ArgumentNullException">密钥为空时抛出</exception>
        public XorCrypto(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "加密密钥不能为空");
            }
            _keyBytes = Encoding.UTF8.GetBytes(key);
        }

        /// <inheritdoc/>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);

            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = (byte)(dataBytes[i] ^ _keyBytes[i % _keyBytes.Length]);
            }

            return Convert.ToBase64String(dataBytes);
        }

        /// <inheritdoc/>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            try
            {
                byte[] dataBytes = Convert.FromBase64String(cipherText);

                for (int i = 0; i < dataBytes.Length; i++)
                {
                    dataBytes[i] = (byte)(dataBytes[i] ^ _keyBytes[i % _keyBytes.Length]);
                }

                return Encoding.UTF8.GetString(dataBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解密失败，数据可能已损坏", ex);
            }
        }
    }
}
