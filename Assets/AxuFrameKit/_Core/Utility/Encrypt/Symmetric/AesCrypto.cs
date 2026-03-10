using System;
using System.Security.Cryptography;
using System.Text;
using AxuFrameKit._Core.Utility.Encrypt.Core;

namespace AxuFrameKit._Core.Utility.Encrypt.Symmetric
{
    /// <summary>
    /// AES-CBC 模式加密实现
    /// 使用 CBC 模式 + 随机 IV，比 ECB 更安全
    /// 线程安全：每次操作创建新的 AES 实例
    /// </summary>
    public class AesCrypto : ISymmetricCrypto
    {
        private const int KeySize = 256;       // AES-256
        private const int BlockSize = 128;     // AES 块大小
        private const int IvLength = 16;       // IV 长度

        /// <summary>
        /// 加密字节数组
        /// </summary>
        public SymmetricCryptoResult Encrypt(byte[] plainData, byte[] key, byte[] iv = null)
        {
            ValidateInput(plainData, key);

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 调整密钥长度
            aes.Key = NormalizeKey(key);

            // 生成或使用提供的 IV
            if (iv != null)
            {
                if (iv.Length != IvLength)
                    throw new EncryptException(EncryptOperation.Encrypt, $"IV 长度必须为 {IvLength} 字节");
                aes.IV = iv;
            }
            else
            {
                aes.GenerateIV();
            }

            using var encryptor = aes.CreateEncryptor();
            var cipherData = encryptor.TransformFinalBlock(plainData, 0, plainData.Length);

            return new SymmetricCryptoResult
            {
                CipherData = cipherData,
                IV = aes.IV
            };
        }

        /// <summary>
        /// 解密字节数组
        /// </summary>
        public byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv)
        {
            ValidateInput(cipherData, key);
            if (iv == null || iv.Length != IvLength)
                throw new EncryptException(EncryptOperation.Decrypt, $"IV 长度必须为 {IvLength} 字节");

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = NormalizeKey(key);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
        }

        /// <summary>
        /// 加密字符串，返回 Base64 编码结果（格式：IV(16字节) + 密文）
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

            // 将 IV 附加到密文前面
            var combined = new byte[IvLength + result.CipherData.Length];
            Buffer.BlockCopy(result.IV, 0, combined, 0, IvLength);
            Buffer.BlockCopy(result.CipherData, 0, combined, IvLength, result.CipherData.Length);

            return Convert.ToBase64String(combined);
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
                var combined = Convert.FromBase64String(cipherText);
                if (combined.Length < IvLength)
                    throw new EncryptException(EncryptOperation.Decrypt, "密文格式无效");

                // 提取 IV 和密文
                var iv = new byte[IvLength];
                var cipherData = new byte[combined.Length - IvLength];
                Buffer.BlockCopy(combined, 0, iv, 0, IvLength);
                Buffer.BlockCopy(combined, IvLength, cipherData, 0, cipherData.Length);

                var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
                var plainBytes = Decrypt(cipherData, keyBytes, iv);

                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "Base64 解码失败", ex);
            }
        }

        /// <summary>
        /// 生成随机密钥
        /// </summary>
        public byte[] GenerateKey()
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = KeySize;
            aes.GenerateKey();
            return aes.Key;
        }

        /// <summary>
        /// 生成随机 IV
        /// </summary>
        public byte[] GenerateIV()
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }

        private void ValidateInput(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "数据不能为空");
            if (key == null || key.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "密钥不能为空");
        }

        private byte[] NormalizeKey(byte[] key)
        {
            // 将密钥调整为 32 字节（AES-256）
            var normalizedKey = new byte[32];
            if (key.Length >= 32)
                Buffer.BlockCopy(key, 0, normalizedKey, 0, 32);
            else
                Buffer.BlockCopy(key, 0, normalizedKey, 0, key.Length);
            return normalizedKey;
        }
    }
}
