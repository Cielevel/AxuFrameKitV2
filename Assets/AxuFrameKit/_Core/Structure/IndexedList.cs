using System;
using System.Collections;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 一个结合 List 和 Dictionary 的容器，具有去重效果，不允许List中有重复的元素。
    /// 支持 O(1) 的按元素查找与按索引访问。
    /// </summary>
    public class IndexedList<T> : IEnumerable<T>
    {
        private readonly List<T> _list;
        private readonly Dictionary<T, int> _indexDict;

        /// <summary>
        /// 获取列表中元素数量。
        /// </summary>
        public int Count => _list.Count;

        public IndexedList(int capacity = 8)
        {
            _list = new List<T>(capacity);
            _indexDict = new Dictionary<T, int>(capacity);
        }

        /// <summary>
        /// 按索引访问元素。
        /// </summary>
        public T this[int index]
        {
            get => _list[index];
            set
            {
                T oldItem = _list[index];
                if (EqualityComparer<T>.Default.Equals(oldItem, value)) return; // 相同元素不处理
                if (_indexDict.ContainsKey(value))
                {
                    Logger.Error("IndexedList: 重复元素");
                    throw new ArgumentException("Duplicate item.", nameof(value));
                }
                _indexDict.Remove(oldItem);
                _list[index] = value;
                _indexDict[value] = index;
            }
        }

        /// <summary>
        /// 添加元素（如果不存在）。
        /// </summary>
        public bool Add(T item)
        {
            if (_indexDict.ContainsKey(item))
                return false;

            _list.Add(item);
            _indexDict[item] = _list.Count - 1;
            return true;
        }

        /// <summary>
        /// 将元素插入到指定位置
        /// </summary>
        /// <param name="index">插入的索引位置</param>
        /// <param name="item">要插入的元素</param>
        /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > _list.Count)
            {
                Logger.Error("IndexedList: Index out of range");
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_indexDict.ContainsKey(item))
                return;

            _list.Insert(index, item);

            for (int i = index; i < _list.Count; i++)
                _indexDict[_list[i]] = i;
        }

        /// <summary>
        /// 移除指定元素。
        /// 注意：为保证O(1)性能，此操作会将最后一个元素移到被删除的位置，顺序会改变。
        /// 若需要保持顺序，请使用 RemoveAt 的有序版本（性能为O(n)）。
        /// </summary>
        public bool Remove(T item)
        {
            if (!_indexDict.TryGetValue(item, out int index))
                return false;

            // 为了避免 O(n) 删除，这里将最后一个元素换到当前位置
            int lastIndex = _list.Count - 1;
            T lastItem = _list[lastIndex];
            _list.RemoveAt(lastIndex);
            _indexDict.Remove(item);

            if (index < lastIndex)
            {
                _list[index] = lastItem;
                _indexDict[lastItem] = index;
            }

            return true;
        }

        /// <summary>
        /// 有序移除O(n)，保证顺序
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>如果成功移除则返回 true</returns>
        public bool RemoveOrdered(T item)
        {
            if (!_indexDict.TryGetValue(item, out int index))
                return false;

            _list.RemoveAt(index);
            _indexDict.Remove(item);
            for (int i = index; i < _list.Count; i++)
                _indexDict[_list[i]] = i;
            return true;
        }

        /// <summary>
        /// 根据索引移除元素。
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count)
            {
                Logger.Error("IndexedList: Index out of range");
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Remove(_list[index]);
        }

        /// <summary>
        /// 清空所有元素。
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            _indexDict.Clear();
        }

        /// <summary>
        /// 检查是否包含某个元素。
        /// </summary>
        public bool Contains(T item) => _indexDict.ContainsKey(item);

        /// <summary>
        /// 获取某个元素的索引。若没有，返回-1。
        /// </summary>
        public int IndexOf(T item)
        {
            return _indexDict.GetValueOrDefault(item, -1);
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}