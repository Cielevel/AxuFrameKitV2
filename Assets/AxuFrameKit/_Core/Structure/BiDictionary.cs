using System;
using System.Collections;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 双向字典：可通过键查值，也可通过值查键。
    /// 内部维护两字典，保证映射一致。
    /// </summary>
    public class BiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _forward = new();
        private readonly Dictionary<TValue, TKey> _backward = new();

        public int Count => _forward.Count;

        public IEnumerable<TKey> Keys => _forward.Keys;
        public IEnumerable<TValue> Values => _forward.Values;

        public BiDictionary(int capacity = 8)
        {
            _forward  = new Dictionary<TKey, TValue>(capacity);
            _backward = new Dictionary<TValue, TKey>(capacity);
        }
        
        /// <summary>
        /// 添加一对映射（唯一键与值）。
        /// 若键或值已存在，会抛出异常。
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            if (_forward.ContainsKey(key))
            {
                Logger.Error("BiDictionary: 重复键值->" + key);
                throw new ArgumentException("Duplicate key.", nameof(key));
            }
            if (_backward.ContainsKey(value))
            {
                Logger.Error("BiDictionary: 重复键值->" + value);
                throw new ArgumentException("Duplicate key.", nameof(key));
            }

            _forward.Add(key, value);
            _backward.Add(value, key);
        }

        /// <summary>
        /// 更新或添加键值对。
        /// 若键或值已存在，将自动覆盖旧映射。
        /// </summary>
        public void Set(TKey key, TValue value)
        {
            if (_forward.TryGetValue(key, out var oldValue))
                _backward.Remove(oldValue);

            if (_backward.TryGetValue(value, out var oldKey))
                _forward.Remove(oldKey);

            _forward[key]    = value;
            _backward[value] = key;
        }

        /// <summary>
        /// 根据键查值。
        /// </summary>
        public bool TryGetByKey(TKey key, out TValue value)
            => _forward.TryGetValue(key, out value);

        /// <summary>
        /// 根据值查键。
        /// </summary>
        public bool TryGetByValue(TValue value, out TKey key)
            => _backward.TryGetValue(value, out key);

        /// <summary>
        /// 移除指定键。
        /// </summary>
        public bool RemoveByKey(TKey key)
        {
            if (_forward.Remove(key, out var value) == false)
                return false;

            _backward.Remove(value);
            return true;
        }

        /// <summary>
        /// 移除指定值。
        /// </summary>
        public bool RemoveByValue(TValue value)
        {
            if (_backward.Remove(value, out var key) == false)
                return false;

            _forward.Remove(key);
            return true;
        }

        /// <summary>
        /// 清空所有映射。
        /// </summary>
        public void Clear()
        {
            _forward.Clear();
            _backward.Clear();
        }

        /// <summary>
        /// 检查是否存在某个键。
        /// </summary>
        public bool ContainsKey(TKey key) => _forward.ContainsKey(key);

        /// <summary>
        /// 检查是否存在某个值。
        /// </summary>
        public bool ContainsValue(TValue value) => _backward.ContainsKey(value);

        /// <summary>
        /// 通过索引器读取（仅支持 key→value 方向）。
        /// </summary>
        public TValue this[TKey key]
        {
            get => _forward[key];
            set => Set(key, value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
