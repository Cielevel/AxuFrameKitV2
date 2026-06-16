using System;
using System.IO;
using System.Security.Cryptography;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// SHA512 哈希算法实现
    /// 线程安全：每次调用创建新的 SHA512 实例
    /// </summary>
    public class SHA512Hasher : IHasher
    {
        /// <summary>
        /// 计算字节数组的 SHA512 哈希值
        /// </summary>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new EncryptException(EncryptOperation.Hash, "输入数据不能为空");

            using var sha512 = System.Security.Cryptography.SHA512.Create();
            return sha512.ComputeHash(data);
        }

        /// <summary>
        /// 计算字符串的 SHA512 哈希值，返回十六进制字符串（小写）
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
        /// 计算文件的 SHA512 哈希值
        /// </summary>
        public byte[] ComputeFileHash(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new EncryptException(EncryptOperation.Hash, "文件路径不能为空");

            if (!File.Exists(filePath))
                throw new EncryptException(EncryptOperation.Hash, $"文件不存在: {filePath}");

            using var sha512 = System.Security.Cryptography.SHA512.Create();
            using var stream = File.OpenRead(filePath);
            return sha512.ComputeHash(stream);
        }
    }
}
