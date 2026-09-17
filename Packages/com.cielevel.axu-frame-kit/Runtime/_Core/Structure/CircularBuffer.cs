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
        /// 查看最新添加的元素，不移除
        /// </summary>
        public T PeekLast()
        {
            if (_count == 0) throw new InvalidOperationException("Buffer is empty.");
            return _buffer[(_start + _count - 1) % Capacity];
        }

        /// <summary>
        /// 查看最早添加的元素，不移除
        /// </summary>
        public T PeekFirst()
        {
            if (_count == 0) throw new InvalidOperationException("Buffer is empty.");
            return _buffer[_start];
        }

        /// <summary>
        /// 尝试移除并返回最早添加的元素（FIFO 消费）；缓冲区为空时不抛异常，返回 false。
        /// 不改变 Add/Peek 的既有语义。
        /// </summary>
        /// <param name="item">出队的元素；缓冲区为空时为 default</param>
        /// <returns>成功移除返回 true</returns>
        public bool TryDequeue(out T item)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_start];
            _buffer[_start] = default; // 释放引用，避免已消费对象滞留缓冲区
            _start = (_start + 1) % Capacity;
            _count--;
            return true;
        }

        /// <summary>
        /// 是否已满
        /// </summary>
        public bool IsFull => _count == Capacity;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => _count == 0;

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

        // 实现 IEnumerable<T>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
                yield return _buffer[(_start + i) % Capacity];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}