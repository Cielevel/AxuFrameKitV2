using System;

namespace AxuFrameKit._Core.Utility.Encrypt
{
    /// <summary>
    /// 十六进制编码器
    /// </summary>
    public static class HexEncoder
    {
        /// <summary>
        /// 编码为十六进制字符串（小写）
        /// </summary>
        /// <param name="data">原始字节数组</param>
        /// <returns>十六进制字符串</returns>
        public static string Encode(byte[] data)
        {
            if (data == null)
                throw new EncryptException(EncryptOperation.Encode, "输入数据不能为空");
            return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 从十六进制字符串解码
        /// </summary>
        /// <param name="hex">十六进制字符串（大小写不敏感）</param>
        /// <returns>原始字节数组</returns>
        public static byte[] Decode(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new EncryptException(EncryptOperation.Decode, "输入字符串不能为空");
            if (hex.Length % 2 != 0)
                throw new EncryptException(EncryptOperation.Decode, "十六进制字符串长度必须为偶数");

            try
            {
                var bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                return bytes;
            }
            catch (FormatException ex)
            {
                throw new EncryptException(EncryptOperation.Decode, "无效的十六进制字符串", ex);
            }
        }

        /// <summary>
        /// 检查字符串是否为有效的十六进制格式
        /// </summary>
        /// <param name="hex">待检查的字符串</param>
        /// <returns>是否为有效十六进制</returns>
        public static bool IsValidHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return false;

            foreach (char c in hex)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }
            return true;
        }
    }
}
