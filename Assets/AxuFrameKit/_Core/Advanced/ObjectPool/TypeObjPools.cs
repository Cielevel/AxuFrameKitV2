using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Advanced.ObjectPool
{
    /// <summary>
    /// 类型对象池接口，支持管理多种不同类型的 IPoolable 对象
    /// </summary>
    public interface IPools : IDisposable
    {
        /// <summary>
        /// 为特定类型添加自定义的创建方法
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="createFunc">创建实例的委托</param>
        void AddCreateFunc<T>(Func<T> createFunc) where T : class, IPoolable;

        /// <summary>
        /// 获取一个对象，返回接口类型
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>池化的对象实例</returns>
        IPoolable Get<T>() where T : class, IPoolable;

        /// <summary>
        /// 获取一个对象，并转换为具体类型
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>池化的对象实例</returns>
        T GetInstance<T>() where T : class, IPoolable;

        /// <summary>
        /// 将对象推入（归还）到池中
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要归还的对象</param>
        void Push<T>(T obj) where T : class, IPoolable;
    }

    /// <summary>
    /// 可池化对象接口，定义对象在池中的生命周期行为
    /// </summary>
    public interface IPoolable : IDisposable
    {
        /// <summary>
        /// 该对象所属的池管理器
        /// </summary>
        public IPools Pools { get; protected set; }

        /// <summary>
        /// 初始化对象所属的池管理器
        /// </summary>
        /// <param name="pools">关联的管理器实例</param>
        public void Initialize(IPools pools) => Pools = pools;

        /// <summary>
        /// 当对象从池中取出时调用
        /// </summary>
        void OnGet();

        /// <summary>
        /// 当对象推入池中时（归还前）调用
        /// </summary>
        void OnPush();

        /// <summary>
        /// 快捷方法：将自身归还到池中
        /// </summary>
        void Push();
    }

    /// <summary>
    /// 可池化对象的抽象基类
    /// </summary>
    public abstract class Poolable : IPoolable
    {
        IPools IPoolable.Pools { get; set; }

        /// <summary>
        /// 获取时的处理逻辑
        /// </summary>
        public abstract void OnGet();

        /// <summary>
        /// 归还前的处理逻辑
        /// </summary>
        public abstract void OnPush();

        /// <summary>
        /// 销毁逻辑
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// 将自身推入管理池中
        /// </summary>
        public void Push()
        {
            OnPush();
            (this as IPoolable).Pools.Push(this);
        }
    }

    /// <summary>
    /// 类型对象池实现；可承载多个类型的对象到一个对象池；按照类型进行取用
    /// </summary>
    public class TypeObjPools : IPools
    {
        private const int InitialPoolSize = 8;

        private Dictionary<Type, Func<IPoolable>> _createFuncDict = new(InitialPoolSize);
        private Dictionary<Type, Stack<IPoolable>> _poolDict = new(InitialPoolSize);

        void IPools.AddCreateFunc<T>(Func<T> createFunc)
        {
            Validate();

            _createFuncDict.Add(typeof(T), () => createFunc?.Invoke());
        }

        IPoolable IPools.Get<T>()
        {
            Validate();

            var key = typeof(T);

            if (_poolDict.TryGetValue(key, out var pool))
            {
                if (pool.Count <= 0)
                {
                    if (_createFuncDict.TryGetValue(key, out var func))
                    {
                        var result = func?.Invoke();
                        ValidatePoolable(result);
                        result?.OnGet();
                        return result;
                    }

                    Logger.Warning($"尝试获取 {typeof(T).Name} 对象时，对象池没有元素，且没有对应的创建方法。");
                    return null;
                }

                var obj = pool.Pop();
                ValidatePoolable(obj);
                obj?.OnGet();
                return obj;
            }

            _poolDict.Add(typeof(T), new Stack<IPoolable>());
            if (_createFuncDict.TryGetValue(key, out var create))
            {
                var result = create?.Invoke();
                ValidatePoolable(result);
                result?.OnGet();
                return result;
            }

            Logger.Warning($"尝试获取 {typeof(T).Name} 对象时，对象池为空，且没有对应的创建方法。");

            return null;
        }

        T IPools.GetInstance<T>()
        {
            Validate();

            var result = (this as IPools).Get<T>();

            return result as T;
        }

        void IPools.Push<T>(T obj)
        {
            Validate();

            var key = typeof(T);

            if (_poolDict.ContainsKey(key) == false)
                _poolDict.Add(key, new Stack<IPoolable>());

            obj.OnPush();
            _poolDict[key].Push(obj);
        }

        private void Validate()
        {
            _createFuncDict ??= new Dictionary<Type, Func<IPoolable>>(InitialPoolSize);

            _poolDict ??= new Dictionary<Type, Stack<IPoolable>>(InitialPoolSize);
        }

        private void ValidatePoolable(IPoolable poolable)
        {
            if (poolable == null)
                return;

            if (poolable.Pools == null)
                poolable.Initialize(this);
        }

        public void Dispose()
        {
            foreach (var poolable in _poolDict.SelectMany(kv => kv.Value))
                poolable.Dispose();

            _poolDict.Clear();
            _createFuncDict.Clear();
        }
    }
}