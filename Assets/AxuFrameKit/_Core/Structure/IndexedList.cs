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
            _list      = new List<T>(capacity);
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
                _indexDict.Remove(oldItem);
                _list[index]      = value;
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
        /// 注意此操作后数组顺序可能会改变。
        /// </summary>
        public bool Remove(T item)
        {
            if (!_indexDict.TryGetValue(item, out int index))
                return false;

            // 为了避免 O(n) 删除，这里将最后一个元素换到当前位置
            int lastIndex = _list.Count - 1;
            T   lastItem  = _list[lastIndex];
            _list.RemoveAt(lastIndex);
            _indexDict.Remove(item);

            if (index < lastIndex)
            {
                _list[index]         = lastItem;
                _indexDict[lastItem] = index;
            }

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