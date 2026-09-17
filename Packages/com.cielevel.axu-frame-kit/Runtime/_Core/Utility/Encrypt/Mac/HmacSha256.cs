using System;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
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

            if (input == null)
                throw new EncryptException(EncryptOperation.Hash, "输入不能为空");
            if (key == null)
                throw new EncryptException(EncryptOperation.Hash, "密钥不能为空");

            // 与 byte 重载一致：取字节级 HMAC 做常量时间比较，避免 string.Equals 的时序泄漏
            // （字符串版 ComputeHash 返回十六进制文本，此处需要原始字节才能走 FixedTimeEquals）
            var actualHash = ComputeHash(System.Text.Encoding.UTF8.GetBytes(input),
                System.Text.Encoding.UTF8.GetBytes(key));

            if (expectedHash.Length != actualHash.Length * 2)
                return false;

            // 十六进制解码；出现非法字符按不匹配处理（与旧版字符串比较的容错语义一致，不抛异常）
            var expectedBytes = new byte[actualHash.Length];
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (!TryParseHexNibble(expectedHash[i * 2], out var high) ||
                    !TryParseHexNibble(expectedHash[i * 2 + 1], out var low))
                    return false;
                expectedBytes[i] = (byte)((high << 4) | low);
            }

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedBytes);
        }

        /// <summary>
        /// 解析单个十六进制字符（0-9 / a-f / A-F，大小写不敏感）
        /// </summary>
        private static bool TryParseHexNibble(char c, out int value)
        {
            if (c >= '0' && c <= '9') { value = c - '0'; return true; }
            if (c >= 'a' && c <= 'f') { value = c - 'a' + 10; return true; }
            if (c >= 'A' && c <= 'F') { value = c - 'A' + 10; return true; }
            value = 0;
            return false;
        }
    }
}
