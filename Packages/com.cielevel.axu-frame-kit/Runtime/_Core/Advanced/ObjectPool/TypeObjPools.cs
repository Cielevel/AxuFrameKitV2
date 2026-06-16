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
        /// 对象的归属池类型，取出时由池管理器自动烙印，防止多态继承漂移
        /// </summary>
        Type PoolKey { get; set; }

        /// <summary>
        /// 标识对象当前是否已经处于池中缓存状态，用于 O(1) 复杂度防御重复归还
        /// </summary>
        bool IsInPool { get; set; }

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
        Type IPoolable.PoolKey { get; set; }
        bool IPoolable.IsInPool { get; set; }

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
            var pools = (this as IPoolable).Pools;
            if (pools != null)
            {
                pools.Push(this);
            }
            else
            {
                Logger.Warning($"尝试给尚未池化归属的对象 {GetType().Name} 执行 Push，未找到池管理器，直接执行 Dispose() 销毁。");
                // Dispose(); // 这里不做任何操作、只Warning可能更安全，让调用者自己决定怎么处理。
            }
        }
    }

    /// <summary>
    /// 类型对象池实现；可承载多个类型的对象到一个对象池；按照类型进行取用
    /// </summary>
    public class TypeObjPools : IPools
    {
        private const int InitialPoolSize = 8;

        /// <summary>
        /// 单个类型的最大缓存容量，超出此数量归还的对象将被丢弃（Dispose）
        /// </summary>
        public int MaxPoolSize { get; set; } = 128;

        private Dictionary<Type, Func<IPoolable>> _createFuncDict = new(InitialPoolSize);
        private Dictionary<Type, Stack<IPoolable>> _poolDict = new(InitialPoolSize);

        void IPools.AddCreateFunc<T>(Func<T> createFunc)
        {
            var key = typeof(T);
            if (!_createFuncDict.TryAdd(key, () => createFunc?.Invoke()))
            {
                _createFuncDict[key] = () => createFunc?.Invoke();
                Logger.Warning($"重复为 {key.Name} 添加了对象的创建方法，已覆盖原方法。");
            }
        }

        IPoolable IPools.Get<T>()
        {
            var key = typeof(T);

            if (!_poolDict.TryGetValue(key, out var pool))
            {
                pool = new Stack<IPoolable>();
                _poolDict.Add(key, pool);
            }

            IPoolable obj = null;
            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else if (_createFuncDict.TryGetValue(key, out var func))
            {
                obj = func?.Invoke();
            }
            else
            {
                Logger.Warning($"尝试获取 {key.Name} 对象时，对象池没有元素，且没有对应的创建方法。");
                return null;
            }

            if (obj != null)
            {
                obj.PoolKey = key; // 【核心】无论是新建还是取出，统一烙印确切归属
                obj.IsInPool = false; // 出借分配时清空在池状态
                ValidatePoolable(obj);
                obj.OnGet();
            }
            return obj;
        }

        T IPools.GetInstance<T>()
        {
            var result = (this as IPools).Get<T>();
            return result as T;
        }

        void IPools.Push<T>(T obj)
        {
            if (obj == null) return;

            // 【核心】检票：优先读取 PoolKey 判断老家，如果未受池管辖(比如外部直接New出来的)则退化为 GetType()
            var key = obj.PoolKey ?? obj.GetType();

            if (!_poolDict.TryGetValue(key, out var pool))
            {
                pool = new Stack<IPoolable>();
                _poolDict.Add(key, pool);
            }

            // 【核心】高频 O(1) 重复归还过滤，取代 O(N) 的 Stack.Contains
            if (obj.IsInPool)
            {
                Logger.Warning($"对象已经处于闲置化缓存池中，请勿引发逻辑死循环重复归还：{key.Name}");
                return;
            }

            if (pool.Count >= MaxPoolSize)
            {
                obj.Dispose();
                return;
            }

            obj.IsInPool = true; // 登记入池保存状态
            obj.OnPush();
            pool.Push(obj);
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