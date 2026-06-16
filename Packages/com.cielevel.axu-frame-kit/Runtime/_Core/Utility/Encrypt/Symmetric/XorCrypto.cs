using System;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 简单的 XOR 异或加密实现
    /// 适用于防简单篡改场景或轻量化混淆，安全性相对较低
    /// </summary>
    public class XorCrypto : ISymmetricCrypto
    {
        /// <summary>
        /// 加密字节数组
        /// </summary>
        public SymmetricCryptoResult Encrypt(byte[] plainData, byte[] key, byte[] iv = null)
        {
            ValidateInput(plainData, key);

            byte[] cipherData = new byte[plainData.Length];
            for (int i = 0; i < plainData.Length; i++)
            {
                cipherData[i] = (byte)(plainData[i] ^ key[i % key.Length]);
            }

            return new SymmetricCryptoResult
            {
                CipherData = cipherData,
                IV = Array.Empty<byte>() // XOR 加解密不需要特殊的 IV，返回空数组
            };
        }

        /// <summary>
        /// 解密字节数组
        /// 对于 XOR，解密与加密操作完全一致
        /// </summary>
        public byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv = null)
        {
            ValidateInput(cipherData, key);

            byte[] plainData = new byte[cipherData.Length];
            for (int i = 0; i < cipherData.Length; i++)
            {
                plainData[i] = (byte)(cipherData[i] ^ key[i % key.Length]);
            }

            return plainData;
        }

        /// <summary>
        /// 加密字符串，返回 Base64 编码结果
        /// </summary>
        public string EncryptString(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new EncryptException(EncryptOperation.Encrypt, "明文不能为空");
            if (string.IsNullOrEmpty(key))
                throw new EncryptException(EncryptOperation.Encrypt, "密钥不能为空");

            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

            var result = Encrypt(plainBytes, keyBytes);
            return Convert.ToBase64String(result.CipherData);
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public string DecryptString(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new EncryptException(EncryptOperation.Decrypt, "密文不能为空");
            if (string.IsNullOrEmpty(key))
                throw new EncryptException(EncryptOperation.Decrypt, "密钥不能为空");

            try
            {
                var cipherData = Convert.FromBase64String(cipherText);
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

                var plainBytes = Decrypt(cipherData, keyBytes, null);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "Base64 解码失败", ex);
            }
        }

        /// <summary>
        /// 生成随机密钥（XOR可以使用任意长度的密钥，这里默认生成16字节）
        /// </summary>
        public byte[] GenerateKey()
        {
            var key = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>
        /// 生成空 IV（XOR加解密不使用 IV 参数）
        /// </summary>
        public byte[] GenerateIV()
        {
            return Array.Empty<byte>();
        }

        private void ValidateInput(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "数据不能为空");
            if (key == null || key.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "密钥不能为空");
        }
    }
}
