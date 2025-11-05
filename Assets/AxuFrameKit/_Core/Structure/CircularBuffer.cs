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

        public int Capacity => _buffer.Length;
        public int Count => _count;
        
        public CircularBuffer(int capacity = 8)
        {
            if (capacity <= 0)
            {
                Logger.Error("CircularBuffer: 容量不能小于等于0");
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            }

            _buffer = new T[capacity];
            _start  = 0;
            _count  = 0;
        }

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

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _buffer[(_start + index) % Capacity];
            }
        }

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