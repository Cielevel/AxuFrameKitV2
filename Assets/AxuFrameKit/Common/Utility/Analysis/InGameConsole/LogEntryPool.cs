using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Utility.Analysis.InGameConsole
{
    /// <summary>
    /// UI元素对象池，用于高性能虚拟化滚动
    /// </summary>
    public class LogEntryElementPool
    {
        private readonly Stack<VisualElement> _pool = new();
        private readonly System.Func<VisualElement> _createFunc;
        
        public LogEntryElementPool(System.Func<VisualElement> createFunc, int initialCapacity = 50)
        {
            _createFunc = createFunc;
            
            // 预先创建一批元素
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_createFunc());
            }
        }
        
        /// <summary>
        /// 从池中获取元素
        /// </summary>
        public VisualElement Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : _createFunc();
        }
        
        /// <summary>
        /// 将元素归还到池中
        /// </summary>
        public void Return(VisualElement element)
        {
            if (element != null)
            {
                element.RemoveFromHierarchy();
                _pool.Push(element);
            }
        }
        
        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
        
        /// <summary>
        /// 获取当前池中的元素数量
        /// </summary>
        public int Count => _pool.Count;
    }
}
