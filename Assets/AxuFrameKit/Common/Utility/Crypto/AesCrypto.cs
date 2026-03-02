using System;
using System.Text;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Crypto
{
    /// <summary>
    /// AES 加密实现
    /// 比异或更安全，适合实际项目使用
    /// 注意：此实现仅作演示，生产环境建议使用更严谨的 IV 处理
    /// </summary>
    public class AesCrypto : ICrypto
    {
        private readonly string _key;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">加密密钥（会被自动调整至32字节）</param>
        public AesCrypto(string key)
        {
            // 简单处理，确保key长度符合AES要求
            _key = key.PadRight(32)[..32];
        }

        /// <summary>
        /// 使用AES加密
        /// </summary>
        public string Encrypt(string input)
        {
            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(_key);
                byte[] toEncryptArray = Encoding.UTF8.GetBytes(input);

                var rDel = new System.Security.Cryptography.RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = System.Security.Cryptography.CipherMode.ECB;
                rDel.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                var cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AesCrypto] 加密失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 使用AES解密
        /// </summary>
        public string Decrypt(string input)
        {
            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(_key);
                byte[] toEncryptArray = Convert.FromBase64String(input);

                var rDel = new System.Security.Cryptography.RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = System.Security.Cryptography.CipherMode.ECB;
                rDel.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                var cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Encoding.UTF8.GetString(resultArray);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AesCrypto] 解密失败: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
