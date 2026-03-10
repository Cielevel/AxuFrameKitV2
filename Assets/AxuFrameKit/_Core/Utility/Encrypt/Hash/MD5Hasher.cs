using System;
using System.IO;
using System.Security.Cryptography;
using AxuFrameKit._Core.Utility.Encrypt.Core;

namespace AxuFrameKit._Core.Utility.Encrypt.Hash
{
    /// <summary>
    /// MD5 哈希算法实现
    /// 注意：MD5 仅适用于数据校验，不应用于安全敏感场景
    /// </summary>
    public class MD5Hasher : IHasher
    {
        /// <summary>
        /// 计算字节数组的 MD5 哈希值
        /// </summary>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new EncryptException(EncryptOperation.Hash, "输入数据不能为空");

            using var md5 = System.Security.Cryptography.MD5.Create();
            return md5.ComputeHash(data);
        }

        /// <summary>
        /// 计算字符串的 MD5 哈希值，返回十六进制字符串（小写）
        /// </summary>
        public string ComputeHash(string input)
        {
            if (input == null)
                throw new EncryptException(EncryptOperation.Hash, "输入字符串不能为空");

            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 计算文件的 MD5 哈希值
        /// </summary>
        public byte[] ComputeFileHash(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new EncryptException(EncryptOperation.Hash, "文件路径不能为空");

            if (!File.Exists(filePath))
                throw new EncryptException(EncryptOperation.Hash, $"文件不存在: {filePath}");

            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);
            return md5.ComputeHash(stream);
        }
    }
}
