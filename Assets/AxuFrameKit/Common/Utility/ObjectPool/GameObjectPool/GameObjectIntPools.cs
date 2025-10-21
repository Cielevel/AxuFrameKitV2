using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreateAssetMenu(fileName = "GameObjectPool-Int", menuName = "AxuFrameKit/Utility/GameObjectPools-Int")]
    public class GameObjectIntPools : ScriptableObject
    {
        [SerializeField] 
        private List<ObjectUnitInt> _objectUnitIntList;

        private Dictionary<int, SimpleObjPool<PoolableInt>> _intPoolDict;
        
        public void Initialize(Action<PoolableInt> onGet, Action<PoolableInt> onRelease, Transform defaultRoot, int defaultCapacity = 16)
        {
            _intPoolDict = new Dictionary<int, SimpleObjPool<PoolableInt>>(_objectUnitIntList.Count);

            var idSet = new HashSet<int>(_objectUnitIntList.Count);
            
            foreach (var unit in _objectUnitIntList)
            {
                var newPool = new SimpleObjPool<PoolableInt>(() => Instantiate(unit.Prefab, defaultRoot), onGet, onRelease, defaultCapacity);
                _intPoolDict.Add(unit.ObjID, newPool);

                if (idSet.Add(unit.ObjID) == false)
                    Debug.LogError("GameObjectIntPools初始化失败，包含重复的ObjID：" + unit.ObjID);
            }
        }
        
        /// <summary>
        /// 获取GameObject实例
        /// </summary>
        public PoolableInt GetObj(int objID)
        {
            MakeSure();

            var poolable = _intPoolDict.TryGetValue(objID, out var pool) ? pool.Get() : null;
            poolable?.Set(this, objID);
            return poolable;

            void MakeSure()
            {
                if (_intPoolDict != null && _intPoolDict.Count == _objectUnitIntList.Count)
                    return;

                _intPoolDict = new Dictionary<int, SimpleObjPool<PoolableInt>>(_objectUnitIntList.Count);

                var idSet = new HashSet<int>(_objectUnitIntList.Count);
                
                foreach (var unit in _objectUnitIntList)
                {
                    var newPool = new SimpleObjPool<PoolableInt>(() => Instantiate(unit.Prefab), OnGetDefault, OnReleaseDefault);
                    _intPoolDict.Add(unit.ObjID, newPool);
                    
                    if (idSet.Add(unit.ObjID) == false)
                        Debug.LogError("GameObjectIntPools初始化失败，包含重复的ObjID：" + unit.ObjID);
                }
            }
        }

        /// <summary>
        /// 默认的释放行为
        /// </summary>
        private static void OnReleaseDefault(PoolableInt poolable) => poolable.gameObject.SetActive(false);
        
        /// <summary>
        /// 默认的获取行为
        /// </summary>
        private static void OnGetDefault(PoolableInt poolable) => poolable.gameObject.SetActive(true);

        public void Release(PoolableInt poolable)
        {
            if (_intPoolDict.TryGetValue(poolable.ObjID, out var pool))
                pool.Release(poolable);
        }
    }
    
    [Serializable]
    public class ObjectUnitInt
    {
        [field: SerializeField]
        public int ObjID { get; set; }

        [field: SerializeField]
        public PoolableInt Prefab { get; set; }
    }
}