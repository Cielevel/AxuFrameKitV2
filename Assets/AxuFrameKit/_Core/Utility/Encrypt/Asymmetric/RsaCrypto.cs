using System;
using System.Security.Cryptography;
using System.Text;
using AxuFrameKit._Core.Utility.Encrypt.Core;

namespace AxuFrameKit._Core.Utility.Encrypt.Asymmetric
{
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
        public byte[] Encrypt(byte[] data, string publicKey)
        {
            if (data == null || data.Length == 0)
                throw new EncryptException(EncryptOperation.Encrypt, "数据不能为空");
            if (string.IsNullOrEmpty(publicKey))
                throw new EncryptException(EncryptOperation.Encrypt, "公钥不能为空");

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);
#if UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
            return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
#else
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
#endif
        }

        /// <summary>
        /// 使用私钥解密
        /// </summary>
        public byte[] Decrypt(byte[] cipherData, string privateKey)
        {
            if (cipherData == null || cipherData.Length == 0)
                throw new EncryptException(EncryptOperation.Decrypt, "密文不能为空");
            if (string.IsNullOrEmpty(privateKey))
                throw new EncryptException(EncryptOperation.Decrypt, "私钥不能为空");

            try
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(privateKey);
#if UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
                return rsa.Decrypt(cipherData, RSAEncryptionPadding.Pkcs1);
#else
                return rsa.Decrypt(cipherData, RSAEncryptionPadding.OaepSHA256);
#endif
            }
            catch (CryptographicException ex)
            {
                throw new EncryptException(EncryptOperation.Decrypt, "RSA 解密失败", ex);
            }
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
