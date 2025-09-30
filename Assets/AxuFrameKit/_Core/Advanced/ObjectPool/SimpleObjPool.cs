using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.ObjectPool
{
    public interface ISimpleObjPool
    {
        void Clear();
        int CountInactive { get; }
    }
    
    /// <summary>
    /// 单一类型，单一物体类型的简易对象池；只负责管理一种类型
    /// </summary>
    public class SimpleObjPool<T> : ISimpleObjPool where T : class
    {
        public int CountInactive => ObjStack.Count;

        protected Stack<T> ObjStack { get; private set; }
        protected Func<T> Constructor { get; private set; }
        protected Action<T> OnGet { get; private set; }
        protected Action<T> OnRelease { get; private set; }
        
        public SimpleObjPool(Func<T> constructor, Action<T> get = null, Action<T> release = null, int defaultCapacity = 16)
        {
            Constructor = constructor;
            OnGet      = get;
            OnRelease  = release;
            ObjStack      = new Stack<T>(defaultCapacity);
        }

        public virtual T Get()
        {
            if (ObjStack.Count > 0)
            {
                var obj = ObjStack.Pop();
                OnGet?.Invoke(obj);
                return obj;
            }
            else
            {
                var obj = Constructor();
                OnGet?.Invoke(obj);
                return obj;
            }
        }
        
        public void Release(T obj)
        {
            OnRelease?.Invoke(obj);
            ObjStack.Push(obj);
        }

        public void Clear() => ObjStack?.Clear();
    }
}