using System;
using System.Text;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Crypto
{
    /// <summary>
    /// 简单的异或加密实现
    /// 适用于防简单篡改场景，安全性一般
    /// </summary>
    public class XorCrypto : ICrypto
    {
        private readonly byte[] _keyBytes;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">加密密钥</param>
        public XorCrypto(string key)
        {
            _keyBytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
        }

        /// <summary>
        /// 使用异或加密
        /// </summary>
        public string Encrypt(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            byte[] data = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= _keyBytes[i % _keyBytes.Length];
            }
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// 使用异或解密
        /// </summary>
        public string Decrypt(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            try
            {
                byte[] data = Convert.FromBase64String(input);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= _keyBytes[i % _keyBytes.Length];
                }
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                Debug.LogError("[XorCrypto] 解密失败");
                return string.Empty;
            }
        }
    }
}
