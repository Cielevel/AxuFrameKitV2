using System;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
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

        // PBKDF2 派生用：随机盐长度（字节）
        private const int Pbkdf2SaltLength = 16;

        // PBKDF2 派生器（无状态，线程安全，可静态复用）
        private static readonly Pbkdf2KeyDerivation Pbkdf2 = new Pbkdf2KeyDerivation();

        /// <summary>
        /// 加密字节数组
        /// </summary>
        public SymmetricCryptoResult Encrypt(byte[] plainData, byte[] key, byte[] iv = null)
        {
            ValidateInput(plainData, key, EncryptOperation.Encrypt);

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 校验密钥长度（不再静默截断/补零，避免弱密钥）
            ValidateKey(key, EncryptOperation.Encrypt);
            aes.Key = key;

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
            ValidateInput(cipherData, key, EncryptOperation.Decrypt);
            if (iv == null || iv.Length != IvLength)
                throw new EncryptException(EncryptOperation.Decrypt, $"IV 长度必须为 {IvLength} 字节");

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = KeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            ValidateKey(key, EncryptOperation.Decrypt);
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
        }

        /// <summary>
        /// 加密字符串，返回 Base64 编码结果（格式：IV(16字节) + 密文）
        /// </summary>
        /// <remarks>
        /// 已过时：该方法直接把口令的 UTF-8 字节当作 AES 密钥（且旧版会静默补零/截断），安全性弱。
        /// 保留是为兼容既有密文（密钥本身为 16/24/32 字节时旧密文仍可解）。
        /// 新代码请使用 <see cref="EncryptStringWithPbkdf2"/>。
        /// </remarks>
        [Obsolete("口令直接作 AES 密钥存在弱密钥风险，请改用 EncryptStringWithPbkdf2；本方法保留仅为兼容旧密文")]
        public string EncryptString(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new EncryptException(EncryptOperation.Encrypt, "明文不能为空");
            if (string.IsNullOrEmpty(key))
                throw new EncryptException(EncryptOperation.Encrypt, "密钥不能为空");

            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            // 旧版行为：口令 UTF-8 字节按密钥长度截断/补零，保证既有密文仍可解（新代码请用 PBKDF2 接口）
            var keyBytes = NormalizeLegacyKey(key);

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
        /// <remarks>
        /// 已过时：与 <see cref="EncryptString"/> 配套的旧口令直传接口，保留仅为兼容旧密文。
        /// 新代码请使用 <see cref="DecryptStringWithPbkdf2"/>。
        /// </remarks>
        [Obsolete("口令直接作 AES 密钥存在弱密钥风险，请改用 DecryptStringWithPbkdf2；本方法保留仅为兼容旧密文")]
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

                // 与 EncryptString 对称：沿用旧版口令归一化，保证既有密文可解
                var keyBytes = NormalizeLegacyKey(key);
                var plainBytes = Decrypt(cipherData, keyBytes, iv);

                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "Base64 解码失败", ex);
            }
        }

        /// <summary>
        /// 使用 PBKDF2 从口令派生密钥后加密字符串（口令场景推荐）。
        /// 输出格式：Base64( salt(16字节) + IV(16字节) + 迭代次数(4字节大端) + 密文 )，
        /// 盐与迭代次数自包含在密文中，解密侧无需额外记录。
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="password">用户口令（任意长度，内部经 PBKDF2 派生 32 字节 AES 密钥）</param>
        /// <param name="iterations">PBKDF2 迭代次数，默认 10000（安全敏感场景建议 ≥ 100000，见 Pbkdf2KeyDerivation 注释）</param>
        public string EncryptStringWithPbkdf2(string plainText, string password,
            int iterations = Pbkdf2KeyDerivation.DefaultIterations)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new EncryptException(EncryptOperation.Encrypt, "明文不能为空");
            if (string.IsNullOrEmpty(password))
                throw new EncryptException(EncryptOperation.Encrypt, "口令不能为空");

            // 每次加密生成随机盐：相同口令不会派生出相同密钥
            var salt = Pbkdf2.GenerateSalt(Pbkdf2SaltLength);
            var key = Pbkdf2.DeriveKey(password, salt, 32, iterations);

            var result = Encrypt(System.Text.Encoding.UTF8.GetBytes(plainText), key);

            // 布局：salt(16) + IV(16) + 迭代次数(4, 大端) + 密文
            var combined = new byte[Pbkdf2SaltLength + IvLength + 4 + result.CipherData.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, Pbkdf2SaltLength);
            Buffer.BlockCopy(result.IV, 0, combined, Pbkdf2SaltLength, IvLength);
            combined[Pbkdf2SaltLength + IvLength] = (byte)(iterations >> 24);
            combined[Pbkdf2SaltLength + IvLength + 1] = (byte)(iterations >> 16);
            combined[Pbkdf2SaltLength + IvLength + 2] = (byte)(iterations >> 8);
            combined[Pbkdf2SaltLength + IvLength + 3] = (byte)iterations;
            Buffer.BlockCopy(result.CipherData, 0, combined, Pbkdf2SaltLength + IvLength + 4, result.CipherData.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// 解密由 <see cref="EncryptStringWithPbkdf2"/> 生成的密文。
        /// 迭代次数从密文头自描述读取，无需调用方传入。
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文（salt + IV + 迭代次数 + 密文）</param>
        /// <param name="password">加密时使用的口令</param>
        public string DecryptStringWithPbkdf2(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new EncryptException(EncryptOperation.Decrypt, "密文不能为空");
            if (string.IsNullOrEmpty(password))
                throw new EncryptException(EncryptOperation.Decrypt, "口令不能为空");

            byte[] combined;
            try
            {
                combined = Convert.FromBase64String(cipherText);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "Base64 解码失败", ex);
            }

            // 头部 = salt(16) + IV(16) + 迭代次数(4)，其后必须还有密文
            const int headerLength = Pbkdf2SaltLength + IvLength + 4;
            if (combined.Length <= headerLength)
                throw new EncryptException(EncryptOperation.Decrypt, "密文格式无效（缺少 salt/IV/迭代次数头）");

            var salt = new byte[Pbkdf2SaltLength];
            var iv = new byte[IvLength];
            Buffer.BlockCopy(combined, 0, salt, 0, Pbkdf2SaltLength);
            Buffer.BlockCopy(combined, Pbkdf2SaltLength, iv, 0, IvLength);

            int iterations = (combined[Pbkdf2SaltLength + IvLength] << 24)
                           | (combined[Pbkdf2SaltLength + IvLength + 1] << 16)
                           | (combined[Pbkdf2SaltLength + IvLength + 2] << 8)
                           | combined[Pbkdf2SaltLength + IvLength + 3];

            var cipherData = new byte[combined.Length - headerLength];
            Buffer.BlockCopy(combined, headerLength, cipherData, 0, cipherData.Length);

            // 用密文中记录的盐与迭代次数重新派生密钥后解密
            var key = Pbkdf2.DeriveKey(password, salt, 32, iterations);
            var plainBytes = Decrypt(cipherData, key, iv);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
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

        /// <summary>
        /// 旧版口令归一化：UTF-8 字节超长截断、不足补零到密钥长度。
        /// 仅为兼容历史密文保留（旧版 EncryptString/DecryptString 即此行为，产物格式不变）；
        /// 字节级接口已改为严格校验密钥长度、不再静默归一化，新代码请走 PBKDF2 接口。
        /// </summary>
        private static byte[] NormalizeLegacyKey(string key)
        {
            int keyByteLength = KeySize / 8;
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

            if (keyBytes.Length == keyByteLength)
                return keyBytes;

            var normalized = new byte[keyByteLength];
            // 注意：本命名空间下存在 AxuFrameKit._Core.Utility.Math，裸写 Math 会被其遮蔽
            Buffer.BlockCopy(keyBytes, 0, normalized, 0, System.Math.Min(keyBytes.Length, keyByteLength));
            return normalized;
        }

        private void ValidateInput(byte[] data, byte[] key, EncryptOperation operation)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(operation, "数据不能为空");
            if (key == null || key.Length == 0)
                throw new EncryptException(operation, "密钥不能为空");
        }

        /// <summary>
        /// 校验密钥长度必须为 AES 合法长度（16/24/32 字节）。
        /// 旧版会把任意长度密钥静默截断/补零到 32 字节（弱密钥且语义不明），现直接拒绝。
        /// </summary>
        private void ValidateKey(byte[] key, EncryptOperation operation)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new EncryptException(operation,
                    $"密钥长度非法：{key.Length} 字节，必须为 16/24/32 字节（AES-128/192/256）；" +
                    "口令请先用 Pbkdf2KeyDerivation 派生密钥，或改用 EncryptStringWithPbkdf2/DecryptStringWithPbkdf2");
        }
    }
}
