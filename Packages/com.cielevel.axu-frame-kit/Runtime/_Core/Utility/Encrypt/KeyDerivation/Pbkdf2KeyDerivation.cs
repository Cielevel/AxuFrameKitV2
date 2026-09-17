using System;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// PBKDF2 密钥派生实现
    /// 用于从用户密码安全地派生加密密钥
    /// </summary>
    public class Pbkdf2KeyDerivation : IKeyDerivation
    {
        /// <summary>
        /// 默认迭代次数 10000。
        /// 注意：保持该默认值是为了不破坏旧数据（迭代次数参与派生结果）；
        /// OWASP 当前建议新项目至少 100000 次，安全敏感场景请显式传入更高值。
        /// </summary>
        public const int DefaultIterations = 10000;

        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// 从密码派生密钥
        /// </summary>
        /// <param name="password">用户密码</param>
        /// <param name="salt">盐值</param>
        /// <param name="keyLength">派生密钥长度（字节）</param>
        /// <param name="iterations">
        /// 迭代次数，默认 10000（兼容旧数据）。OWASP 建议新项目 ≥ 100000。
        /// 迁移说明：迭代次数参与密钥派生——已存数据必须用当初的迭代次数派生才能解密；
        /// 提高该值后旧密文需先用旧值解出再以新值重新加密，不可直接混用。
        /// </param>
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
        /// <param name="iterations">
        /// 迭代次数，默认 10000（兼容旧数据）。OWASP 建议新项目 ≥ 100000；
        /// 迁移注意事项见 <see cref="DeriveKey(string, byte[], int, int)"/>。
        /// </param>
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
