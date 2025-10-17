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
        // TODO 在GameObjectPool示例中，存在一些问题：在频繁获取和释放时，会存在丢失情况（按下5次按键，并未产生5个物体；在构造好场景的对象池后）
        // ObjPool本身可能没问题，问题在于多次对一个物体调用Release？如果一个物体被多次调用了Release，会导致对象池中存在重复的物体引用，从而在Get时获取到同一个物体多次，进而导致丢失其他物体。
        // 可能需要设立一个HashSet来记录已经被Release的物体，防止重复Release。（但又需要注意性能问题）
        
        public int CountInactive => ObjStack.Count;

        protected Stack<T> ObjStack { get; private set; }
        protected Func<T> Constructor { get; private set; }
        protected Action<T> OnGet { get; private set; }
        protected Action<T> OnRelease { get; private set; }
        
        public SimpleObjPool(Func<T> constructor, Action<T> onGet = null, Action<T> onRelease = null, int defaultCapacity = 16)
        {
            Constructor = constructor;
            OnGet       = onGet;
            OnRelease   = onRelease;
            ObjStack    = new Stack<T>(defaultCapacity);
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