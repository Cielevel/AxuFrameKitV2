using System;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// Base64 编码器
    /// </summary>
    public static class Base64Encoder
    {
        /// <summary>
        /// 编码为 Base64 字符串
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>Base64 编码字符串</returns>
        public static string Encode(byte[] data)
        {
            if (data == null)
                throw new EncryptException(EncryptOperation.Encode, "输入数据不能为空");
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// 从 Base64 字符串解码
        /// </summary>
        /// <param name="encoded">Base64 编码字符串</param>
        /// <returns>原始字节数组</returns>
        public static byte[] Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                throw new EncryptException(EncryptOperation.Decode, "输入字符串不能为空");

            try
            {
                return Convert.FromBase64String(encoded);
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decode, "无效的 Base64 字符串", ex);
            }
        }

        /// <summary>
        /// URL 安全的 Base64 编码
        /// 将 + 替换为 -，/ 替换为 _，并移除 = 填充
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>URL 安全的 Base64 字符串</returns>
        public static string EncodeUrlSafe(byte[] data)
        {
            return Encode(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// URL 安全的 Base64 解码
        /// 自动还原字符和填充
        /// </summary>
        /// <param name="encoded">URL 安全的 Base64 字符串</param>
        /// <returns>原始字节数组</returns>
        public static byte[] DecodeUrlSafe(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                throw new EncryptException(EncryptOperation.Decode, "输入字符串不能为空");

            var padded = encoded
                .Replace('-', '+')
                .Replace('_', '/');

            // 添加填充
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Decode(padded);
        }
    }
}
