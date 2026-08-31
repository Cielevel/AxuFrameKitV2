using System;
using System.Collections.Generic;

namespace AxuTestFramework.Core.Metrics
{
    /// <summary>
    /// 固定容量环形缓冲：满后覆盖最旧（FIFO 淘汰），Count 恒不超容量——长跑采样防内存无界增长。
    /// </summary>
    public class RingBuffer<T>
    {
        readonly T[] _items;
        int _head; // 最旧元素在 _items 中的下标
        int _count;

        public RingBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须为正数");
            _items = new T[capacity];
        }

        /// <summary>固定容量（构造后不变）。</summary>
        public int Capacity => _items.Length;

        /// <summary>当前元素数（恒 ≤ Capacity）。</summary>
        public int Count => _count;

        /// <summary>写入元素；满后覆盖最旧。</summary>
        public void Add(T item)
        {
            var tail = (_head + _count) % _items.Length;
            _items[tail] = item;
            if (_count < _items.Length) _count++;
            else _head = (_head + 1) % _items.Length;
        }

        /// <summary>按序读取：0=最旧，Count-1=最新。</summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
                return _items[(_head + index) % _items.Length];
            }
        }

        /// <summary>按最旧 → 最新顺序枚举。</summary>
        public IEnumerable<T> Items
        {
            get
            {
                for (var i = 0; i < _count; i++) yield return this[i];
            }
        }
    }
}
