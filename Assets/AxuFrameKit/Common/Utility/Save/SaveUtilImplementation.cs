using System;
using System.IO;
using System.Text;
using UnityEngine;
using AxuFrameKit.Common.Utility.Serializer;

namespace AxuFrameKit.Common.Utility.Save
{
    // ==========================================
    // 默认实现 (Concrete Implementations) --> SaveUtil可以直接使用这些实现
    // 注意：UnityJsonSerializer 已移至 AxuFrameKit.Common.Utility.Serializer 命名空间
    // ==========================================

    // --- 实现B: 本地文件存储 ---
    public class LocalFileStorage : IStorage
    {
        private string _basePath;
        private string _extension;

        public LocalFileStorage(string extension = ".json")
        {
            _basePath = Application.persistentDataPath;
            _extension = extension;
        }

        private string GetPath(string key) => Path.Combine(_basePath, key + _extension);

        public void Write(string key, string data) => File.WriteAllText(GetPath(key), data);

        public string Read(string key) => File.ReadAllText(GetPath(key));

        public void Delete(string key)
        {
            if (Exists(key)) File.Delete(GetPath(key));
        }

        public bool Exists(string key) => File.Exists(GetPath(key));
    }

    // --- 实现C: 简单的 AES 加密 (比异或更安全) ---
    public class AesCrypto : ICrypto
    {
        private string _key; // 必须是16, 24, 或 32 字节

        public AesCrypto(string key)
        {
            // 简单处理，确保key长度符合AES要求
            _key = key.PadRight(32)[..32];
        }

        public string Encrypt(string input)
        {
            // 这里仅作演示，实际生产环境建议使用更严谨的 AES 实现 (IV处理等)
            byte[] keyArray = Encoding.UTF8.GetBytes(_key);
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(input);

            var rDel = new System.Security.Cryptography.RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = System.Security.Cryptography.CipherMode.ECB; // ECB简单但安全性略低，建议用CBC
            rDel.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

            var cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string Decrypt(string input)
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
    }
}