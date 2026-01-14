using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.ObjectPool
{
    public interface ISimpleObjPool
    {
        int CountInactive { get; }

        void Preload(int count);

        void Clear();
    }

    /// <summary>
    /// 单一类型，单一物体类型的简易对象池；只负责管理一种类型
    /// </summary>
    public class SimpleObjPool<T> : ISimpleObjPool where T : class
    {
        // 解决方案有两个：
        // 1.可能需要设立一个HashSet来记录已经被Release的物体，防止重复Release。（但又需要注意性能问题）
        // 2.在外部Release前就自行检查，确保不会重复Release。（需要用户自行负责）

        public int CountInactive => ObjStack.Count;

        protected Stack<T> ObjStack { get; private set; }
        protected Func<T> Constructor { get; private set; }
        protected Action<T> OnGet { get; private set; }
        protected Action<T> OnRelease { get; private set; }

        private const int DefaultCapacity = 16;

        public SimpleObjPool(Func<T> constructor, Action<T> onGet = null, Action<T> onRelease = null, int defaultCapacity = DefaultCapacity)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            OnGet = onGet;
            OnRelease = onRelease;
            ObjStack = new Stack<T>(defaultCapacity > 0 ? defaultCapacity : DefaultCapacity);
        }

        /// <summary>
        /// 预载对象到对象池；此操作将新增（触发Constructor）
        /// </summary>
        public void Preload(int count)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var obj = Constructor();
                OnRelease?.Invoke(obj);
                ObjStack.Push(obj);
            }
        }

        public virtual T Get()
        {
            T obj = ObjStack.Count > 0 ? ObjStack.Pop() : Constructor();
            OnGet?.Invoke(obj);
            return obj;
        }

        public void Release(T obj)
        {
            OnRelease?.Invoke(obj);
            ObjStack.Push(obj);
        }

        public void Clear() => ObjStack.Clear();
    }
}