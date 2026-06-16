using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Advanced.ObjectPool;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    public class GameObjectPools<T> : ScriptableObject
    {
        [SerializeField]
        private List<ObjectUnit<T>> _objectUnitList;

        private Dictionary<T, SimpleObjPool<Poolable<T>>> _poolDict;

        public void Initialize(Action<Poolable<T>> onGet, Action<Poolable<T>> onRelease, Transform defaultRoot, int defaultCapacity = 16)
        {
            _poolDict = new Dictionary<T, SimpleObjPool<Poolable<T>>>(_objectUnitList.Count);

            var idSet = new HashSet<T>(_objectUnitList.Count);

            foreach (var unit in _objectUnitList)
            {
                var newPool = new SimpleObjPool<Poolable<T>>(() => Instantiate(unit.Prefab, defaultRoot), onGet, onRelease, defaultCapacity);

                if (idSet.Add(unit.ObjID) == false)
                    Debug.LogError("GameObjectIntPools初始化失败，包含重复的ObjID：" + unit.ObjID);

                _poolDict.Add(unit.ObjID, newPool);
            }
        }

        /// <summary>
        /// 获取GameObject实例
        /// </summary>
        public Poolable<T> Get(T objId)
        {
            if (_poolDict == null)
            {
                Debug.LogError("GameObjectIntPools 尚未初始化，请先调用 Initialize()");
                return null;
            }

            var poolable = _poolDict.TryGetValue(objId, out var pool) ? pool.Get() : null;
            poolable?.Set(objId, Release);
            return poolable;
        }

        /// <summary>
        /// 预加载指定数量的GameObject实例到缓存池中
        /// </summary>
        /// <param name="objId">GameObject的ID</param>
        /// <param name="count">需要预加载的数量</param>
        public void Prewarm(T objId, int count)
        {
            if (_poolDict == null)
            {
                Debug.LogError("GameObjectPools 尚未初始化，请先调用 Initialize()");
                return;
            }

            if (_poolDict.TryGetValue(objId, out var pool))
                pool.Preload(count);
            else
                Debug.LogError("GameObjectPools 中不存在 ObjID：" + objId);
        }

        private void Release(Poolable<T> poolable)
        {
            if (_poolDict.TryGetValue(poolable.ObjId, out var pool))
                pool.Release(poolable);
        }
    }

    [Serializable]
    public class ObjectUnit<T>
    {
        [field: SerializeField]
        public T ObjID { get; set; }

        [field: SerializeField]
        public Poolable<T> Prefab { get; set; }
    }
}