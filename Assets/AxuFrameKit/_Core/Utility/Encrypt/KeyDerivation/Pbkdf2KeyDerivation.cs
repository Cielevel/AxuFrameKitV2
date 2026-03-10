using System;
using System.Security.Cryptography;
using AxuFrameKit._Core.Utility.Encrypt.Core;

namespace AxuFrameKit._Core.Utility.Encrypt.KeyDerivation
{
    /// <summary>
    /// PBKDF2 密钥派生实现
    /// 用于从用户密码安全地派生加密密钥
    /// </summary>
    public class Pbkdf2KeyDerivation : IKeyDerivation
    {
        private const int DefaultIterations = 10000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// 从密码派生密钥
        /// </summary>
        /// <param name="password">用户密码</param>
        /// <param name="salt">盐值</param>
        /// <param name="keyLength">派生密钥长度（字节）</param>
        /// <param name="iterations">迭代次数，默认10000（建议至少10000次）</param>
        /// <returns>派生的密钥</returns>
        public byte[] DeriveKey(string password, byte[] salt, int keyLength, int iterations = DefaultIterations)
        {
            if (string.IsNullOrEmpty(password))
                throw new EncryptException(EncryptOperation.KeyDerivation, "密码不能为空");
            if (salt == null || salt.Length == 0)
                throw new EncryptException(EncryptOperation.KeyDerivation, "盐值不能为空");
            if (keyLength <= 0)
                throw new EncryptException(EncryptOperation.KeyDerivation, "密钥长度必须大于 0");
            if (iterations < 1000)
                throw new EncryptException(EncryptOperation.KeyDerivation, "迭代次数建议至少 1000 次");

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithm);

            return pbkdf2.GetBytes(keyLength);
        }

        /// <summary>
        /// 生成随机盐值
        /// </summary>
        /// <param name="saltLength">盐值长度（字节），默认16</param>
        /// <returns>随机盐值</returns>
        public byte[] GenerateSalt(int saltLength = 16)
        {
            if (saltLength < 8)
                throw new EncryptException(EncryptOperation.KeyDerivation, "盐值长度建议至少 8 字节");

            var salt = new byte[saltLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// 从密码派生密钥并生成新盐值（便捷方法）
        /// </summary>
        /// <param name="password">用户密码</param>
        /// <param name="keyLength">派生密钥长度（字节），默认32</param>
        /// <param name="saltLength">盐值长度（字节），默认16</param>
        /// <param name="iterations">迭代次数，默认10000</param>
        /// <returns>派生的密钥和生成的盐值</returns>
        public (byte[] key, byte[] salt) DeriveKeyWithNewSalt(
            string password,
            int keyLength = 32,
            int saltLength = 16,
            int iterations = DefaultIterations)
        {
            var salt = GenerateSalt(saltLength);
            var key = DeriveKey(password, salt, keyLength, iterations);
            return (key, salt);
        }
    }
}
