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
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// 队列中的元素数量
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// 初始化空优先级队列（使用默认比较器 Comparer&lt;T&gt;.Default，行为与原先一致）
        /// </summary>
        public PriorityQueue() : this(Comparer<T>.Default) { }

        /// <summary>
        /// 使用自定义比较器初始化空优先级队列（比较结果小者先出队）
        /// </summary>
        /// <param name="comparer">元素比较器，传 null 将抛出异常</param>
        public PriorityQueue(IComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
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
                if (_comparer.Compare(_data[childIndex], _data[parentIndex]) >= 0)
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
                if (rightChild <= lastIndex && _comparer.Compare(_data[rightChild], _data[childIndex]) < 0)
                    childIndex = rightChild;

                if (_comparer.Compare(_data[parentIndex], _data[childIndex]) <= 0)
                    break;

                (_data[parentIndex], _data[childIndex]) = (_data[childIndex], _data[parentIndex]);
                parentIndex = childIndex;
            }

            return frontItem;
        }

        /// <summary>
        /// 尝试出队优先级最高（值最小）的元素；队列为空时不抛异常，返回 false。
        /// </summary>
        /// <param name="item">出队的元素；队列为空时为 default</param>
        /// <returns>成功出队返回 true</returns>
        public bool TryDequeue(out T item)
        {
            if (_data.Count == 0)
            {
                item = default;
                return false;
            }

            item = Dequeue();
            return true;
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
