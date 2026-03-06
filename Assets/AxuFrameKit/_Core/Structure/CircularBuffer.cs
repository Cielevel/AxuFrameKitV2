using System;
using System.Collections;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 循环缓冲
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private int _start;
        private int _count;

        /// <summary>
        /// 缓冲区的总容量
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// 当前缓冲区中存储的有效元素数量
        /// </summary>
        public int Count => _count;

        public CircularBuffer(int capacity = 8)
        {
            if (capacity <= 0)
            {
                Logger.Error("CircularBuffer: 容量不能小于等于0");
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }

            _buffer = new T[capacity];
            _start = 0;
            _count = 0;
        }

        /// <summary>
        /// 向缓冲区添加一个元素。若缓冲区已满，则覆盖最早的元素。
        /// </summary>
        /// <param name="item">要添加的元素</param>
        public void Add(T item)
        {
            int index = (_start + _count) % Capacity;
            _buffer[index] = item;

            if (_count == Capacity)
            {
                // 覆盖旧值
                _start = (_start + 1) % Capacity;
            }
            else
            {
                _count++;
            }
        }

        /// <summary>
        /// 按索引获取元素
        /// </summary>
        /// <param name="index">相对于起始位置的索引</param>
        /// <returns>找到的元素</returns>
        /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _buffer[(_start + index) % Capacity];
            }
        }

        /// <summary>
        /// 清空缓冲区所有元素
        /// </summary>
        public void Clear()
        {
            _start = 0;
            _count = 0;
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        // 🔹 实现 IEnumerable<T>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
                yield return _buffer[(_start + i) % Capacity];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}