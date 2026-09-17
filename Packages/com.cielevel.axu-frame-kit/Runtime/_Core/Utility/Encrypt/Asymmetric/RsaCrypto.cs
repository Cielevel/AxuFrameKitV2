using System;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// RSA 加密填充方式选择。
    /// 不显式指定时维持历史行为：按编译目标自动选择（Unity=Pkcs1，非 Unity=OaepSha256），
    /// 以保证跨端旧密文仍可解；需要跨端互操作的场景请显式传入同一填充方式。
    /// </summary>
    public enum RsaPaddingChoice
    {
        /// <summary>PKCS#1 v1.5 填充（Unity 编译目标默认值；单次明文上限为 keySize/8-11 字节）</summary>
        Pkcs1,

        /// <summary>OAEP-SHA256 填充（非 Unity 编译目标默认值，安全性更高；单次明文上限为 keySize/8-66 字节）</summary>
        OaepSha256
    }

    /// <summary>
    /// RSA 非对称加密实现
    /// 线程安全：每次操作创建新的 RSA 实例
    /// </summary>
    public class RsaCrypto : IAsymmetricCrypto
    {
        /// <summary>
        /// 生成 RSA 密钥对
        /// </summary>
        /// <param name="keySize">密钥大小（位），默认2048</param>
        /// <returns>公钥和私钥的 XML 格式字符串</returns>
        public (string publicKey, string privateKey) GenerateKeyPair(int keySize = 2048)
        {
            if (keySize < 512)
                throw new EncryptException(EncryptOperation.KeyDerivation, "密钥大小必须至少 512 位");

            using var rsa = RSA.Create(keySize);
            var publicKey = rsa.ToXmlString(false);
            var privateKey = rsa.ToXmlString(true);
            return (publicKey, privateKey);
        }

        /// <summary>
        /// 使用公钥加密
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="publicKey">公钥</param>
        /// <param name="padding">填充方式；不传时按编译目标选择（Unity=Pkcs1，非 Unity=OaepSha256），维持历史行为</param>
        public byte[] Encrypt(byte[] data, string publicKey, RsaPaddingChoice? padding = null)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "数据不能为空");
            if (string.IsNullOrEmpty(publicKey))
                throw new EncryptException(EncryptOperation.Encrypt, "公钥不能为空");

            var paddingChoice = ResolvePadding(padding);

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);

            // PKCS#1 v1.5 单次明文上限为 keySize/8-11 字节：超长提前抛带说明的异常，而非裸 CryptographicException
            if (paddingChoice == RsaPaddingChoice.Pkcs1)
            {
                int maxPlainLength = rsa.KeySize / 8 - 11;
                if (data.Length > maxPlainLength)
                    throw new EncryptException(EncryptOperation.Encrypt,
                        $"PKCS1 填充下单次加密明文过长：{data.Length} 字节，超过上限 {maxPlainLength} 字节（密钥 {rsa.KeySize} 位）。" +
                        "请分块加密，或显式改用 RsaPaddingChoice.OaepSha256 / 对称加密");
            }

            return rsa.Encrypt(data, ToRsaPadding(paddingChoice));
        }

        /// <summary>
        /// 使用私钥解密
        /// </summary>
        /// <param name="cipherData">加密数据</param>
        /// <param name="privateKey">私钥</param>
        /// <param name="padding">填充方式；不传时按编译目标选择（Unity=Pkcs1，非 Unity=OaepSha256），维持历史行为</param>
        public byte[] Decrypt(byte[] cipherData, string privateKey, RsaPaddingChoice? padding = null)
        {
            if (cipherData == null || cipherData.Length == 0)
                throw new EncryptException(EncryptOperation.Decrypt, "密文不能为空");
            if (string.IsNullOrEmpty(privateKey))
                throw new EncryptException(EncryptOperation.Decrypt, "私钥不能为空");

            try
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(cipherData, ToRsaPadding(ResolvePadding(padding)));
            }
            catch (CryptographicException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "RSA 解密失败", ex);
            }
        }

        // 接口显式实现：带可选参数的公共签名不满足 IAsymmetricCrypto 契约，这里补齐不带 padding 的旧签名（走默认填充）
        byte[] IAsymmetricCrypto.Encrypt(byte[] data, string publicKey) => Encrypt(data, publicKey, null);

        byte[] IAsymmetricCrypto.Decrypt(byte[] cipherData, string privateKey) => Decrypt(cipherData, privateKey, null);

        /// <summary>
        /// 解析填充方式：显式传入优先；未传入时按编译目标选择以兼容既有密文。
        /// </summary>
        private static RsaPaddingChoice ResolvePadding(RsaPaddingChoice? padding)
        {
            if (padding.HasValue)
                return padding.Value;

#if UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
            return RsaPaddingChoice.Pkcs1;
#else
            return RsaPaddingChoice.OaepSha256;
#endif
        }

        /// <summary>
        /// 枚举转 .NET 填充实例
        /// </summary>
        private static RSAEncryptionPadding ToRsaPadding(RsaPaddingChoice choice)
        {
            return choice == RsaPaddingChoice.OaepSha256
                ? RSAEncryptionPadding.OaepSHA256
                : RSAEncryptionPadding.Pkcs1;
        }

        /// <summary>
        /// 使用私钥签名
        /// </summary>
        public byte[] Sign(byte[] data, string privateKey)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Sign, "数据不能为空");
            if (string.IsNullOrEmpty(privateKey))
                throw new EncryptException(EncryptOperation.Sign, "私钥不能为空");

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// 使用公钥验证签名
        /// </summary>
        public bool Verify(byte[] data, byte[] signature, string publicKey)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Verify, "数据不能为空");
            if (signature == null || signature.Length == 0)
                throw new EncryptException(EncryptOperation.Verify, "签名不能为空");
            if (string.IsNullOrEmpty(publicKey))
                throw new EncryptException(EncryptOperation.Verify, "公钥不能为空");

            try
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(publicKey);
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// 使用公钥加密字符串
        /// </summary>
        /// <param name="plainText">明文字符串</param>
        /// <param name="publicKey">公钥</param>
        /// <returns>Base64 编码的密文</returns>
        public string EncryptString(string plainText, string publicKey)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new EncryptException(EncryptOperation.Encrypt, "明文不能为空");

            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var encrypted = Encrypt(bytes, publicKey);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// 使用私钥解密字符串
        /// </summary>
        /// <param name="cipherText">Base64 编码的密文</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>解密后的明文</returns>
        public string DecryptString(string cipherText, string privateKey)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new EncryptException(EncryptOperation.Decrypt, "密文不能为空");

            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                var decrypted = Decrypt(bytes, privateKey);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "Base64 解码失败", ex);
            }
        }
    }
}
