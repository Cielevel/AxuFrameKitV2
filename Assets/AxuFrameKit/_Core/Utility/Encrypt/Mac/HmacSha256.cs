using System;
using System.Security.Cryptography;
using System.Text;
using AxuFrameKit._Core.Utility.Encrypt.Core;

namespace AxuFrameKit._Core.Utility.Encrypt.Mac
{
    /// <summary>
    /// HMAC-SHA256 消息认证码实现
    /// 用于验证消息的完整性和真实性
    /// </summary>
    public static class HmacSha256
    {
        /// <summary>
        /// 计算 HMAC-SHA256
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="key">密钥</param>
        /// <returns>HMAC 值（32字节）</returns>
        public static byte[] ComputeHash(byte[] data, byte[] key)
        {
            if (data == null)
                throw new EncryptException(EncryptOperation.Hash, "数据不能为空");
            if (key == null || key.Length == 0)
                throw new EncryptException(EncryptOperation.Hash, "密钥不能为空");

            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        /// <summary>
        /// 计算字符串的 HMAC-SHA256
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="key">密钥字符串</param>
        /// <returns>十六进制格式的 HMAC 值（小写）</returns>
        public static string ComputeHash(string input, string key)
        {
            if (input == null)
                throw new EncryptException(EncryptOperation.Hash, "输入不能为空");
            if (key == null)
                throw new EncryptException(EncryptOperation.Hash, "密钥不能为空");

            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var hash = ComputeHash(inputBytes, keyBytes);

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 验证 HMAC（使用恒定时间比较防止时序攻击）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="key">密钥</param>
        /// <param name="expectedHash">期望的 HMAC 值</param>
        /// <returns>验证是否通过</returns>
        public static bool Verify(byte[] data, byte[] key, byte[] expectedHash)
        {
            if (expectedHash == null)
                throw new EncryptException(EncryptOperation.Verify, "期望的哈希值不能为空");

            var actualHash = ComputeHash(data, key);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        /// <summary>
        /// 验证字符串的 HMAC
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="key">密钥字符串</param>
        /// <param name="expectedHash">期望的十六进制 HMAC 值</param>
        /// <returns>验证是否通过</returns>
        public static bool Verify(string input, string key, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
                throw new EncryptException(EncryptOperation.Verify, "期望的哈希值不能为空");

            var actualHash = ComputeHash(input, key);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
