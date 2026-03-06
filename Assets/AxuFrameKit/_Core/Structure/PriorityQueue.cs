using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Structure
{
    /// <summary>
    /// 优先级队列（最小堆实现）
    /// 优先级越低（CompareTo返回值越小），越先出队
    /// </summary>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private readonly List<T> _data;

        /// <summary>
        /// 队列中的元素数量
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// 初始化空优先级队列
        /// </summary>
        public PriorityQueue()
        {
            _data = new List<T>();
        }

        /// <summary>
        /// 向队列中入队一个新元素
        /// </summary>
        /// <param name="item">要添加的元素</param>
        public void Enqueue(T item)
        {
            _data.Add(item);
            int childIndex = _data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (_data[childIndex].CompareTo(_data[parentIndex]) >= 0)
                    break;

                (_data[childIndex], _data[parentIndex]) = (_data[parentIndex], _data[childIndex]);
                childIndex = parentIndex;
            }
        }

        /// <summary>
        /// 出队优先级最高（值最小）的元素
        /// </summary>
        /// <returns>出队的元素</returns>
        /// <exception cref="InvalidOperationException">队列为空时抛出</exception>
        public T Dequeue()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            int lastIndex = _data.Count - 1;
            T frontItem = _data[0];
            _data[0] = _data[lastIndex];
            _data.RemoveAt(lastIndex);

            lastIndex--;

            int parentIndex = 0;
            while (true)
            {
                int childIndex = parentIndex * 2 + 1;
                if (childIndex > lastIndex)
                    break;

                int rightChild = childIndex + 1;
                if (rightChild <= lastIndex && _data[rightChild].CompareTo(_data[childIndex]) < 0)
                    childIndex = rightChild;

                if (_data[parentIndex].CompareTo(_data[childIndex]) <= 0)
                    break;

                (_data[parentIndex], _data[childIndex]) = (_data[childIndex], _data[parentIndex]);
                parentIndex = childIndex;
            }

            return frontItem;
        }

        /// <summary>
        /// 查看但不移除优先级最高的元素
        /// </summary>
        /// <returns>优先级最高的元素</returns>
        /// <exception cref="InvalidOperationException">队列为空时抛出</exception>
        public T Peek()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            return _data[0];
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// 检查队列是否包含指定元素
        /// </summary>
        /// <param name="item">要检查的元素</param>
        /// <returns>若存在则返回 true</returns>
        public bool Contains(T item)
        {
            return _data.Contains(item);
        }
    }
}
