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

        public void Write(string key, string data) => File.WriteAllText(GetPath(key), data);

        public string Read(string key) => File.Exists(GetPath(key)) ? File.ReadAllText(GetPath(key)) : null;

        public void Delete(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool Exists(string key) => File.Exists(GetPath(key));

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
            PlayerPrefs.SetString(key, data);
            RegisterKey(key);
            PlayerPrefs.Save();
        }

        public string Read(string key)
        {
            return PlayerPrefs.GetString(key, null);
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            UnregisterKey(key);
            PlayerPrefs.Save();
        }

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
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
            string path = GetFilePath(key);
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(data);
        }

        public string Read(string key)
        {
            string path = GetFilePath(key);
            if (!File.Exists(path)) return null;
            using var reader = new BinaryReader(File.Open(path, FileMode.Open));
            return reader.ReadString();
        }

        public void Delete(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool Exists(string key)
        {
            return File.Exists(GetFilePath(key));
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