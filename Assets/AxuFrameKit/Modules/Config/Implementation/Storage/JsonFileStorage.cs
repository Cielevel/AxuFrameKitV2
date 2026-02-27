using System.IO;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Modules.Config.Interface;

namespace AxuFrameKit.Modules.Config.Implementation.Storage
{
    /// <summary>
    /// JSON 文件存储实现
    /// 适合需要可读性的配置，文件以明文形式存储
    /// </summary>
    public class JsonFileStorage : IConfigStorage
    {
        private readonly string _basePath;
        private readonly string _extension;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="directory">存储目录，null 则使用 persistentDataPath</param>
        /// <param name="extension">文件扩展名</param>
        public JsonFileStorage(string directory = null, string extension = ".json")
        {
            _basePath = directory ?? Application.persistentDataPath;
            _extension = extension.StartsWith(".") ? extension : "." + extension;

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        private string GetFilePath(string key) => Path.Combine(_basePath, key + _extension);

        /// <inheritdoc/>
        public void Write(string key, string data)
        {
            string path = GetFilePath(key);
            File.WriteAllText(path, data);
        }

        /// <inheritdoc/>
        public string Read(string key)
        {
            string path = GetFilePath(key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <inheritdoc/>
        public void Delete(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <inheritdoc/>
        public bool Exists(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        /// <inheritdoc/>
        public string[] GetAllKeys()
        {
            if (!Directory.Exists(_basePath))
            {
                return new string[0];
            }

            var files = Directory.GetFiles(_basePath, "*" + _extension);
            var keys = new List<string>(files.Length);
            foreach (var file in files)
            {
                keys.Add(Path.GetFileNameWithoutExtension(file));
            }
            return keys.ToArray();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            var keys = GetAllKeys();
            foreach (var key in keys)
            {
                Delete(key);
            }
        }
    }
}
