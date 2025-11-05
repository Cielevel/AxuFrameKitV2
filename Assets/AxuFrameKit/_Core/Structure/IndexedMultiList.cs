using System;
using System.Collections;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 支持重复元素的索引化列表。
    /// 结合 List 和 Dictionary，实现 O(1) 添加与删除。
    /// </summary>
    public class IndexedMultiList<T> : IEnumerable<T>
    {
        private readonly List<T> _list;
        private readonly Dictionary<T, HashSet<int>> _indexDict;

        /// <summary>
        /// 元素数量。
        /// </summary>
        public int Count => _list.Count;

        public IndexedMultiList(int capacity = 8)
        {
            _list      = new List<T>(capacity);
            _indexDict = new Dictionary<T, HashSet<int>>();
        }
        
        /// <summary>
        /// 按索引访问元素。
        /// </summary>
        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (index < 0 || index >= _list.Count)
                {
                    Logger.Error("IndexedMultiList: Index out of range");
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                T oldItem = _list[index];
                if (EqualityComparer<T>.Default.Equals(oldItem, value))
                    return; // 同值无需更新

                // 更新映射关系
                _indexDict[oldItem].Remove(index);
                if (_indexDict[oldItem].Count == 0)
                    _indexDict.Remove(oldItem);

                _list[index] = value;
                if (!_indexDict.TryGetValue(value, out var set))
                {
                    set              = new HashSet<int>();
                    _indexDict[value] = set;
                }
                set.Add(index);
            }
        }

        /// <summary>
        /// 添加一个元素。
        /// </summary>
        public void Add(T item)
        {
            int index = _list.Count;
            _list.Add(item);

            if (!_indexDict.TryGetValue(item, out var set))
            {
                set = new HashSet<int>(4);

                _indexDict[item] = set;
            }
            set.Add(index);
        }

        /// <summary>
        /// 移除一个元素（只移除第一个找到的）。
        /// </summary>
        public bool Remove(T item)
        {
            if (!_indexDict.TryGetValue(item, out var indices) || indices.Count == 0)
                return false;

            // 取一个索引（HashSet无序，这里取第一个）
            int indexToRemove = -1;
            foreach (var idx in indices)
            {
                indexToRemove = idx;
                break;
            }

            RemoveAt(indexToRemove);
            return true;
        }

        /// <summary>
        /// 按索引移除。
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count)
            {
                Logger.Error("IndexedMultiList: Index out of range");
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            T   item      = _list[index];
            int lastIndex = _list.Count - 1;
            T   lastItem  = _list[lastIndex];

            _list.RemoveAt(lastIndex);
            _indexDict[item].Remove(index);
            if (_indexDict[item].Count == 0)
                _indexDict.Remove(item);

            if (index < lastIndex)
            {
                _list[index] = lastItem;

                // 更新 lastItem 的索引
                _indexDict[lastItem].Remove(lastIndex);
                _indexDict[lastItem].Add(index);
            }
        }

        /// <summary>
        /// 清空列表。
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            _indexDict.Clear();
        }

        /// <summary>
        /// 是否包含某个元素。
        /// </summary>
        public bool Contains(T item) => _indexDict.ContainsKey(item);

        /// <summary>
        /// 返回某个元素的所有索引。
        /// </summary>
        public IEnumerable<int> IndicesOf(T item)
        {
            if (_indexDict.TryGetValue(item, out var set))
                return set;
            return Array.Empty<int>();
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
