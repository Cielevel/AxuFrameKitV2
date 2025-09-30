using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Advanced.ObjectPool
{
    public interface IPools : IDisposable
    {
        void AddCreateFunc<T>(Func<T> createFunc) where T : class, IPoolable;
        IPoolable Get<T>() where T : class, IPoolable;
        T GetInstance<T>() where T : class, IPoolable;
        void Push<T>(T obj) where T : class, IPoolable;
    }

    public interface IPoolable : IDisposable
    {
        public IPools Pools { get; protected set; }

        public void Initialize(IPools pools) => Pools = pools;
        
        void OnGet();
        void OnPush();

        void Push();
    }
    
    public abstract class Poolable : IPoolable
    {
        IPools IPoolable.Pools { get; set; }

        public abstract void OnGet();

        public abstract void OnPush();

        public abstract void Dispose();

        public void Push()
        {
            OnPush();
            (this as IPoolable).Pools.Push(this);
        }
    }
    
    /// <summary>
    /// 类型对象池；可承载多个类型的对象到一个对象池；按照类型进行取用
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