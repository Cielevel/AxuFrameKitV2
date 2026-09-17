using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Save
{
    // ==========================================
    // 存储实现 (Storage Implementations)
    // 注意：序列化器在 AxuFrameKit.Common.Utility.Serializer 中实现
    // ==========================================

    // --- 键名安全校验（防路径逃逸与 PlayerPrefs 键注册表损坏） ---
    internal static class StorageKeyValidator
    {
        /// <summary>
        /// 校验存储键名：仅允许字母/数字/下划线/横线/点，且不得为空、不得包含连续点（".."）。
        /// 路径分隔符（/ \）会造成文件存储路径逃逸；";" 会破坏 PlayerPrefs 键注册表——一律 LogError 并拒绝。
        /// </summary>
        /// <param name="key">待校验键名</param>
        /// <param name="storageName">存储实现名（用于日志前缀）</param>
        /// <returns>合法返回 true；非法返回 false 并 LogError</returns>
        public static bool IsValid(string key, string storageName)
        {
            if (string.IsNullOrEmpty(key) || key.Contains(".."))
            {
                Debug.LogError($"[{storageName}] 非法键名（为空或包含 \"..\"）：\"{key}\"，操作已拒绝");
                return false;
            }

            foreach (var c in key)
            {
                // 白名单：字母/数字（含 Unicode，兼容既有中文键名）+ 下划线 + 横线 + 点
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                {
                    Debug.LogError($"[{storageName}] 非法键名（包含不允许的字符 '{c}'）：\"{key}\"，操作已拒绝");
                    return false;
                }
            }

            return true;
        }
    }

    // --- 实现B: 本地文件存储 ---
    public class LocalFileStorage : IStorage, IStorageExtended
    {
        private string _basePath;
        private string _extension;

        public LocalFileStorage(string directory = null, string extension = ".json")
        {
            _basePath = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            _extension = extension.StartsWith(".") ? extension : "." + extension;

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        private string GetPath(string key) => Path.Combine(_basePath, key + _extension);

        public void Write(string key, string data)
        {
            // 键名白名单校验：防止路径分隔符 / ".." 造成路径逃逸
            if (!StorageKeyValidator.IsValid(key, nameof(LocalFileStorage))) return;
            File.WriteAllText(GetPath(key), data);
        }

        public string Read(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(LocalFileStorage))) return null;
            return File.Exists(GetPath(key)) ? File.ReadAllText(GetPath(key)) : null;
        }

        public void Delete(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(LocalFileStorage))) return;
            string path = GetPath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool Exists(string key)
        {
            return StorageKeyValidator.IsValid(key, nameof(LocalFileStorage)) && File.Exists(GetPath(key));
        }

        public string[] GetAllKeys()
        {
            if (!Directory.Exists(_basePath)) return new string[0];
            var files = Directory.GetFiles(_basePath, "*" + _extension);
            var keys = new System.Collections.Generic.List<string>(files.Length);
            foreach (var file in files)
            {
                keys.Add(Path.GetFileNameWithoutExtension(file));
            }
            return keys.ToArray();
        }

        public void Clear()
        {
            var keys = GetAllKeys();
            foreach (var k in keys)
            {
                Delete(k);
            }
        }
    }

    // --- 实现C: JSON 文件存储（别名，便于区分） ---
    public class JsonFileStorage : LocalFileStorage
    {
        public JsonFileStorage(string directory = null, string extension = ".json")
            : base(directory, extension)
        {
        }
    }

    // --- 实现D: PlayerPrefs 存储 (支持键注册) ---
    public class PlayerPrefsStorage : IStorage, IStorageExtended
    {
        private readonly string _keyRegistryKey;

        public PlayerPrefsStorage(string keyRegistryKey = "__save_keys__")
        {
            _keyRegistryKey = keyRegistryKey;
        }

        public void Write(string key, string data)
        {
            // 键名白名单校验：防止 ";" 等字符破坏以分号连接的键注册表
            if (!StorageKeyValidator.IsValid(key, nameof(PlayerPrefsStorage))) return;
            PlayerPrefs.SetString(key, data);
            RegisterKey(key);
            PlayerPrefs.Save();
        }

        public string Read(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(PlayerPrefsStorage))) return null;
            return PlayerPrefs.GetString(key, null);
        }

        public void Delete(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(PlayerPrefsStorage))) return;
            PlayerPrefs.DeleteKey(key);
            UnregisterKey(key);
            PlayerPrefs.Save();
        }

        public bool Exists(string key)
        {
            return StorageKeyValidator.IsValid(key, nameof(PlayerPrefsStorage)) && PlayerPrefs.HasKey(key);
        }

        public string[] GetAllKeys()
        {
            string keysData = PlayerPrefs.GetString(_keyRegistryKey, "");
            if (string.IsNullOrEmpty(keysData)) return new string[0];
            return keysData.Split(';');
        }

        public void Clear()
        {
            var keys = GetAllKeys();
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key))
                    PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.DeleteKey(_keyRegistryKey);
            PlayerPrefs.Save();
        }

        private void RegisterKey(string key)
        {
            var set = new System.Collections.Generic.HashSet<string>(GetAllKeys());
            if (set.Add(key))
            {
                PlayerPrefs.SetString(_keyRegistryKey, string.Join(";", set));
            }
        }

        private void UnregisterKey(string key)
        {
            var list = new System.Collections.Generic.List<string>(GetAllKeys());
            list.Remove(key);
            PlayerPrefs.SetString(_keyRegistryKey, string.Join(";", list));
        }
    }

    // --- 实现F: 二进制文件存储 ---
    public class BinaryFileStorage : IStorage, IStorageExtended
    {
        private readonly string _basePath;
        private readonly string _extension;

        public BinaryFileStorage(string directory = null, string extension = ".dat")
        {
            _basePath = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            _extension = extension.StartsWith(".") ? extension : "." + extension;

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        private string GetFilePath(string key) => Path.Combine(_basePath, key + _extension);

        public void Write(string key, string data)
        {
            // 键名白名单校验：防止路径分隔符 / ".." 造成路径逃逸
            if (!StorageKeyValidator.IsValid(key, nameof(BinaryFileStorage))) return;
            string path = GetFilePath(key);
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(data);
        }

        public string Read(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(BinaryFileStorage))) return null;
            string path = GetFilePath(key);
            if (!File.Exists(path)) return null;
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            return reader.ReadString();
        }

        public void Delete(string key)
        {
            if (!StorageKeyValidator.IsValid(key, nameof(BinaryFileStorage))) return;
            string path = GetFilePath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool Exists(string key)
        {
            return StorageKeyValidator.IsValid(key, nameof(BinaryFileStorage)) && File.Exists(GetFilePath(key));
        }

        public string[] GetAllKeys()
        {
            if (!Directory.Exists(_basePath)) return new string[0];
            var files = Directory.GetFiles(_basePath, "*" + _extension);
            var keys = new System.Collections.Generic.List<string>(files.Length);
            foreach (var file in files)
            {
                keys.Add(Path.GetFileNameWithoutExtension(file));
            }
            return keys.ToArray();
        }

        public void Clear()
        {
            var keys = GetAllKeys();
            foreach (var k in keys)
            {
                Delete(k);
            }
        }
    }
}