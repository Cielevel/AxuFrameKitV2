using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Advanced.ObjectPool
{
    /// <summary>
    /// 简易对象池接口
    /// </summary>
    public interface ISimpleObjPool
    {
        /// <summary>
        /// 池中当前非活跃（空闲）的对象数量
        /// </summary>
        int CountInactive { get; }

        /// <summary>
        /// 预载指定数量的对象到池中
        /// </summary>
        /// <param name="count">要预载的数量</param>
        void Preload(int count);

        /// <summary>
        /// 清空池中所有非活跃对象
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 单个物体类型的简易对象池实现
    /// </summary>
    /// <typeparam name="T">对象类型（类）</typeparam>
    public class SimpleObjPool<T> : ISimpleObjPool where T : class
    {
        /// <summary>
        /// 获取当前非活跃对象数量
        /// </summary>
        public int CountInactive => ObjStack.Count;

        /// <summary>
        /// 内部存储容器
        /// </summary>
        protected Stack<T> ObjStack { get; private set; }

        /// <summary>
        /// 对象构造委托
        /// </summary>
        protected Func<T> Constructor { get; private set; }

        /// <summary>
        /// 对象从池中取出时的回调（如激活、重置位置等）
        /// </summary>
        protected Action<T> OnGet { get; private set; }

        /// <summary>
        /// 对象归还到池中时的回调（如隐藏、重置属性等）
        /// </summary>
        protected Action<T> OnRelease { get; private set; }

        /// <summary>
        /// 可选的对象有效性校验（返回 false 视为已销毁/失效，出池时跳过、入池时拒绝）。
        /// 本类为纯 C# 无法感知 Unity 销毁，由 Unity 侧调用方注入（如 obj => obj != null）。
        /// </summary>
        protected Func<T, bool> AliveChecker { get; private set; }

        // 当前已在池内（空闲）的对象集合，与 ObjStack 内容保持一致，用于守卫重复归还
        private readonly HashSet<T> _pooledObjs;

        private const int DefaultCapacity = 16;

        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="constructor">创建新实例的方法</param>
        /// <param name="onGet">获取对象时的初始化动作</param>
        /// <param name="onRelease">释放对象时的重置动作</param>
        /// <param name="defaultCapacity">初始分配容量</param>
        /// <param name="aliveChecker">可选的有效性校验，用于跳过已销毁对象（如 Unity 对象被外部 Destroy）</param>
        public SimpleObjPool(Func<T> constructor, Action<T> onGet = null, Action<T> onRelease = null,
            int defaultCapacity = DefaultCapacity, Func<T, bool> aliveChecker = null)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            OnGet = onGet;
            OnRelease = onRelease;
            AliveChecker = aliveChecker;
            ObjStack = new Stack<T>(defaultCapacity > 0 ? defaultCapacity : DefaultCapacity);
            _pooledObjs = new HashSet<T>(defaultCapacity > 0 ? defaultCapacity : DefaultCapacity);
        }

        /// <summary>
        /// 预载对象到对象池
        /// </summary>
        /// <param name="count">要预创建的数量</param>
        public void Preload(int count)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var obj = Constructor();
                OnRelease?.Invoke(obj);
                _pooledObjs.Add(obj);
                ObjStack.Push(obj);
            }
        }

        /// <summary>
        /// 从池中获取一个对象。若池为空，则调用 Constructor 创建。
        /// 池内滞留的已销毁/失效对象会被跳过（不下发）。
        /// </summary>
        /// <returns>获取的对象实例</returns>
        public virtual T Get()
        {
            T obj = null;

            while (ObjStack.Count > 0)
            {
                var candidate = ObjStack.Pop();
                _pooledObjs.Remove(candidate);

                if (IsAlive(candidate))
                {
                    obj = candidate;
                    break;
                }
                // 已销毁/失效的滞留对象直接丢弃
            }

            obj ??= Constructor();
            OnGet?.Invoke(obj);
            return obj;
        }

        /// <summary>
        /// 将对象归还（释放）到池中。已销毁/失效对象不入池；重复归还会被忽略并告警。
        /// </summary>
        /// <param name="obj">要归还的对象</param>
        public void Release(T obj)
        {
            if (IsAlive(obj) == false)
                return; // 已销毁/失效的对象不回池

            if (_pooledObjs.Add(obj) == false)
            {
                Logger.Warning("SimpleObjPool: 重复归还同一对象，已忽略。Type=" + typeof(T).Name);
                return;
            }

            OnRelease?.Invoke(obj);
            ObjStack.Push(obj);
        }

        /// <summary>
        /// 清空池中所有由于不用的对象
        /// </summary>
        public void Clear()
        {
            ObjStack.Clear();
            _pooledObjs.Clear();
        }

        /// <summary>
        /// 对象是否有效：非 null 且未通过注入的校验判定为销毁
        /// </summary>
        private bool IsAlive(T obj)
        {
            return obj != null && (AliveChecker == null || AliveChecker(obj));
        }
    }
}