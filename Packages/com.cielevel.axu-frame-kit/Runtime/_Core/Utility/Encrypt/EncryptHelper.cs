using System;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 加密工具统一入口
    /// 提供便捷的静态方法访问各种加密功能
    /// </summary>
    public static class EncryptHelper
    {
        // 延迟初始化的实例（线程安全）
        private static readonly Lazy<SHA256Hasher> _sha256Hasher = new(() => new SHA256Hasher());
        private static readonly Lazy<SHA512Hasher> _sha512Hasher = new(() => new SHA512Hasher());
        private static readonly Lazy<MD5Hasher> _md5Hasher = new(() => new MD5Hasher());
        private static readonly Lazy<AesCrypto> _aesCrypto = new(() => new AesCrypto());
        private static readonly Lazy<RsaCrypto> _rsaCrypto = new(() => new RsaCrypto());
        private static readonly Lazy<Pbkdf2KeyDerivation> _pbkdf2 = new(() => new Pbkdf2KeyDerivation());
        private static readonly Lazy<XorCrypto> _xorCrypto = new(() => new XorCrypto());

        #region 哈希实例

        /// <summary>SHA256 哈希实例</summary>
        public static SHA256Hasher SHA256 => _sha256Hasher.Value;

        /// <summary>SHA512 哈希实例</summary>
        public static SHA512Hasher SHA512 => _sha512Hasher.Value;

        /// <summary>MD5 哈希实例（仅用于校验，不用于安全场景）</summary>
        public static MD5Hasher MD5 => _md5Hasher.Value;

        #endregion

        #region 哈希便捷方法

        /// <summary>
        /// 计算字符串的 SHA256 哈希值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>十六进制哈希值（小写）</returns>
        public static string HashSHA256(string input) => SHA256.ComputeHash(input);

        /// <summary>
        /// 计算字符串的 SHA512 哈希值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>十六进制哈希值（小写）</returns>
        public static string HashSHA512(string input) => SHA512.ComputeHash(input);

        /// <summary>
        /// 计算字符串的 MD5 哈希值（仅用于校验，不用于安全场景）
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>十六进制哈希值（小写）</returns>
        public static string HashMD5(string input) => MD5.ComputeHash(input);

        #endregion

        #region AES 对称加密

        /// <summary>AES 加密实例</summary>
        public static AesCrypto AES => _aesCrypto.Value;

        /// <summary>
        /// AES 加密字符串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="key">密钥</param>
        /// <returns>Base64 编码的密文（包含IV）</returns>
        public static string AesEncrypt(string plainText, string key) => AES.EncryptString(plainText, key);

        /// <summary>
        /// AES 解密字符串
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文</param>
        /// <param name="key">密钥</param>
        /// <returns>解密后的明文</returns>
        public static string AesDecrypt(string cipherText, string key) => AES.DecryptString(cipherText, key);

        /// <summary>
        /// 生成随机 AES 密钥
        /// </summary>
        /// <returns>32字节密钥</returns>
        public static byte[] GenerateAesKey() => AES.GenerateKey();

        /// <summary>
        /// 生成随机 AES IV
        /// </summary>
        /// <returns>16字节 IV</returns>
        public static byte[] GenerateAesIV() => AES.GenerateIV();

        #endregion

        #region XOR 对称加密

        /// <summary>XOR 加密实例</summary>
        public static XorCrypto XOR => _xorCrypto.Value;

        /// <summary>
        /// XOR 加密字符串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="key">密钥</param>
        /// <returns>Base64 编码的密文</returns>
        public static string XorEncrypt(string plainText, string key) => XOR.EncryptString(plainText, key);

        /// <summary>
        /// XOR 解密字符串
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文</param>
        /// <param name="key">密钥</param>
        /// <returns>解密后的明文</returns>
        public static string XorDecrypt(string cipherText, string key) => XOR.DecryptString(cipherText, key);

        #endregion

        #region RSA 非对称加密

        /// <summary>RSA 加密实例</summary>
        public static RsaCrypto RSA => _rsaCrypto.Value;

        /// <summary>
        /// 生成 RSA 密钥对
        /// </summary>
        /// <param name="keySize">密钥大小（位），默认2048</param>
        /// <returns>公钥和私钥元组</returns>
        public static (string publicKey, string privateKey) GenerateRsaKeyPair(int keySize = 2048) => RSA.GenerateKeyPair(keySize);

        /// <summary>
        /// RSA 公钥加密字符串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="publicKey">公钥</param>
        /// <returns>Base64 编码的密文</returns>
        public static string RsaEncrypt(string plainText, string publicKey) => RSA.EncryptString(plainText, publicKey);

        /// <summary>
        /// RSA 私钥解密字符串
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>解密后的明文</returns>
        public static string RsaDecrypt(string cipherText, string privateKey) => RSA.DecryptString(cipherText, privateKey);

        #endregion

        #region 编码转换

        /// <summary>
        /// Base64 编码
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>Base64 字符串</returns>
        public static string ToBase64(byte[] data) => Base64Encoder.Encode(data);

        /// <summary>
        /// Base64 解码
        /// </summary>
        /// <param name="encoded">Base64 字符串</param>
        /// <returns>原始字节数组</returns>
        public static byte[] FromBase64(string encoded) => Base64Encoder.Decode(encoded);

        /// <summary>
        /// URL 安全的 Base64 编码
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>URL 安全的 Base64 字符串</returns>
        public static string ToBase64UrlSafe(byte[] data) => Base64Encoder.EncodeUrlSafe(data);

        /// <summary>
        /// URL 安全的 Base64 解码
        /// </summary>
        /// <param name="encoded">URL 安全的 Base64 字符串</param>
        /// <returns>原始字节数组</returns>
        public static byte[] FromBase64UrlSafe(string encoded) => Base64Encoder.DecodeUrlSafe(encoded);

        /// <summary>
        /// 十六进制编码
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>十六进制字符串（小写）</returns>
        public static string ToHex(byte[] data) => HexEncoder.Encode(data);

        /// <summary>
        /// 十六进制解码
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>原始字节数组</returns>
        public static byte[] FromHex(string hex) => HexEncoder.Decode(hex);

        #endregion

        #region 密钥派生

        /// <summary>PBKDF2 密钥派生实例</summary>
        public static Pbkdf2KeyDerivation PBKDF2 => _pbkdf2.Value;

        /// <summary>
        /// 从密码派生密钥
        /// </summary>
        /// <param name="password">用户密码</param>
        /// <param name="salt">盐值</param>
        /// <param name="keyLength">密钥长度（字节），默认32</param>
        /// <param name="iterations">迭代次数，默认10000</param>
        /// <returns>派生的密钥</returns>
        public static byte[] DeriveKey(string password, byte[] salt, int keyLength = 32, int iterations = 10000)
            => PBKDF2.DeriveKey(password, salt, keyLength, iterations);

        /// <summary>
        /// 生成随机盐值
        /// </summary>
        /// <param name="saltLength">盐值长度（字节），默认16</param>
        /// <returns>随机盐值</returns>
        public static byte[] GenerateSalt(int saltLength = 16) => PBKDF2.GenerateSalt(saltLength);

        /// <summary>
        /// 从密码派生密钥并生成新盐值
        /// </summary>
        /// <param name="password">用户密码</param>
        /// <param name="keyLength">密钥长度（字节），默认32</param>
        /// <param name="saltLength">盐值长度（字节），默认16</param>
        /// <returns>派生的密钥和生成的盐值</returns>
        public static (byte[] key, byte[] salt) DeriveKeyWithSalt(
            string password,
            int keyLength = 32,
            int saltLength = 16)
            => PBKDF2.DeriveKeyWithNewSalt(password, keyLength, saltLength);

        #endregion

        #region HMAC 消息认证

        /// <summary>
        /// 计算 HMAC-SHA256
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="key">密钥</param>
        /// <returns>十六进制 HMAC 值（小写）</returns>
        public static string ComputeHMAC(string input, string key) => HmacSha256.ComputeHash(input, key);

        /// <summary>
        /// 验证 HMAC-SHA256
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="key">密钥</param>
        /// <param name="expectedHash">期望的 HMAC 值</param>
        /// <returns>验证是否通过</returns>
        public static bool VerifyHMAC(string input, string key, string expectedHash)
            => HmacSha256.Verify(input, key, expectedHash);

        #endregion
    }
}
