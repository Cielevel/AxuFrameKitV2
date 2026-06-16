using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Save
{
    /// <summary>
    /// 通用存档管理工具类（注意，json序列化使用Unity.JsonUtility，一些数据结构可能不支持）
    /// 支持字符串存储和对象泛型存储
    /// 支持 PlayerPrefs, JSON文件, 和 加密文件三种模式
    /// </summary>
    public static class SimpleSave
    {
        // 定义存储模式
        public enum SaveMode
        {
            PlayerPrefs,      // Unity自带的键值对存储 (适合小数据)
            JsonFile,         // 本地明文JSON文件 (适合调试，大数据)
            EncryptedJsonFile // 本地加密文件 (适合正式发布，防篡改)
        }

        // 加密密钥 (实际项目中请修改这个Key)
        // private const string EncryptionKey = "MyGameSecretKey_2024";

        // 稍微增加一点反编译难度的写法（不是绝对安全，但比直接写死好）
        private static string EncryptionKey() => "kX9#" + "mP2$" + "L5vR";

        private const string JsonExt = ".json";
        private const string SavExt = ".sav";

        #region 1. 基础字符串存储 (String Storage)

        /// <summary>
        /// 存储字符串
        /// </summary>
        /// <param name="key">存储的键名或文件名(不带后缀)</param>
        /// <param name="value">要存储的字符串内容</param>
        /// <param name="mode">存储模式</param>
        public static void SaveString(string key, string value, SaveMode mode = SaveMode.JsonFile)
        {
            try
            {
                switch (mode)
                {
                    case SaveMode.PlayerPrefs:
                        PlayerPrefs.SetString(key, value);
                        PlayerPrefs.Save();
                        break;

                    case SaveMode.JsonFile:
                        string path = GetFilePath(key, JsonExt);
                        File.WriteAllText(path, value);
                        break;

                    case SaveMode.EncryptedJsonFile:
                        string encryptedPath = GetFilePath(key, SavExt);
                        string encryptedData = Encrypt(value);
                        File.WriteAllText(encryptedPath, encryptedData);
                        break;
                }
                Debug.Log($"[SimpleSave] Saved string successfully via {mode}: {key}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleSave] Save String Error: {e.Message}");
            }
        }

        /// <summary>
        /// 加载字符串
        /// </summary>
        public static string LoadString(string key, SaveMode mode = SaveMode.JsonFile)
        {
            try
            {
                switch (mode)
                {
                    case SaveMode.PlayerPrefs:
                        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

                    case SaveMode.JsonFile:
                        string path = GetFilePath(key, JsonExt);
                        if (File.Exists(path))
                        {
                            return File.ReadAllText(path);
                        }
                        break;

                    case SaveMode.EncryptedJsonFile:
                        string encryptedPath = GetFilePath(key, SavExt);
                        if (File.Exists(encryptedPath))
                        {
                            string content = File.ReadAllText(encryptedPath);
                            return Decrypt(content);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleSave] Load String Error: {e.Message}");
            }
            return null; // 未找到或出错返回null
        }

        #endregion

        #region 2. 泛型对象存储 (Generic Class Storage)

        /// <summary>
        /// 存储对象 (泛型)
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储的键名或文件名</param>
        /// <param name="data">数据对象</param>
        /// <param name="mode">存储模式</param>
        public static void SaveData<T>(string key, T data, SaveMode mode = SaveMode.JsonFile)
        {
            if (data is string strData)
            {
                SaveString(key, strData, mode);
                return;
            }

            // 将对象序列化为JSON字符串
            string json = JsonUtility.ToJson(data, true);

            // 复用字符串存储逻辑
            SaveString(key, json, mode);
        }

        /// <summary>
        /// 加载对象 (泛型)
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">存储的键名或文件名</param>
        /// <param name="mode">存储模式</param>
        /// <returns>对象实例，如果不存在则返回默认值</returns>
        public static T LoadData<T>(string key, SaveMode mode = SaveMode.JsonFile) where T : new()
        {
            string json = LoadString(key, mode);

            if (string.IsNullOrEmpty(json))
            {
                return new T(); // 返回一个新的默认对象，避免空引用
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleSave] Json Parse Error: {e.Message}");
                return new T();
            }
        }

        #endregion

        #region 3. 辅助功能 (Helpers)

        // 获取文件完整路径
        private static string GetFilePath(string fileName, string extension)
        {
            return Path.Combine(Application.persistentDataPath, fileName + extension);
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public static void DeleteSave(string key, SaveMode mode = SaveMode.JsonFile)
        {
            switch (mode)
            {
                case SaveMode.PlayerPrefs:
                    PlayerPrefs.DeleteKey(key);
                    break;
                case SaveMode.JsonFile:
                    string path = GetFilePath(key, JsonExt);
                    if (File.Exists(path)) File.Delete(path);
                    break;
                case SaveMode.EncryptedJsonFile:
                    string ePath = GetFilePath(key, SavExt);
                    if (File.Exists(ePath)) File.Delete(ePath);
                    break;
            }
            Debug.Log($"[SimpleSave] Deleted save: {key}");
        }

        // --- 简单的异或+Base64加密算法 ---

        private static string Encrypt(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey());

            for (int i = 0; i < dataBytes.Length; i++)
            {
                dataBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(dataBytes);
        }

        private static string Decrypt(string data)
        {
            try
            {
                byte[] dataBytes = Convert.FromBase64String(data);
                byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey());

                for (int i = 0; i < dataBytes.Length; i++)
                {
                    dataBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                return Encoding.UTF8.GetString(dataBytes);
            }
            catch
            {
                Debug.LogError("[SimpleSave] Decryption failed. Save file might be corrupted.");
                return "";
            }
        }

        #endregion
    }
}