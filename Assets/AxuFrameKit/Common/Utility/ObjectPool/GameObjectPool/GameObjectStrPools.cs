using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreateAssetMenu(fileName = "GameObjectPool-Str", menuName = "AxuFrameKit/Utility/GameObjectPools-Str")]
    public class GameObjectStrPools : ScriptableObject
    {
        [SerializeField]
        private List<ObjectUnitStr> _objectUnitIntList;
        
        private Dictionary<string, SimpleObjPool<PoolableStr>> _strPoolDict;
        
        public void Initialize(Action<PoolableStr> onGet, Action<PoolableStr> onRelease, Transform defaultRoot, int defaultCapacity = 16)
        {
            _strPoolDict = new Dictionary<string, SimpleObjPool<PoolableStr>>(_objectUnitIntList.Count);

            var strSet = new HashSet<string>(_objectUnitIntList.Count);
            
            foreach (var unit in _objectUnitIntList)
            {
                var newPool = new SimpleObjPool<PoolableStr>(() => Instantiate(unit.Prefab, defaultRoot), onGet, onRelease, defaultCapacity);
                _strPoolDict.Add(unit.ObjName, newPool);
                
                if (strSet.Add(unit.ObjName) == false)
                    Debug.LogError("GameObjectStrPools初始化失败，包含重复的ObjName：" + unit.ObjName);
            }
        }

        /// <summary>
        /// 获取GameObject实例
        /// </summary>
        public PoolableStr GetObj(string objName)
        {
            MakeSure();

            var poolable = _strPoolDict.TryGetValue(objName, out var pool) ? pool.Get() : null;
            poolable?.Set(this, objName);
            return poolable;

            void MakeSure()
            {
                if (_strPoolDict != null && _strPoolDict.Count == _objectUnitIntList.Count)
                    return;

                _strPoolDict = new Dictionary<string, SimpleObjPool<PoolableStr>>(_objectUnitIntList.Count);

                var strSet = new HashSet<string>(_objectUnitIntList.Count);
                
                foreach (var unit in _objectUnitIntList)
                {
                    var newPool = new SimpleObjPool<PoolableStr>(() => Instantiate(unit.Prefab), OnGetDefault, OnReleaseDefault);
                    _strPoolDict.Add(unit.ObjName, newPool);
                    
                    if (strSet.Add(unit.ObjName) == false)
                        Debug.LogError("GameObjectStrPools初始化失败，包含重复的ObjName：" + unit.ObjName);
                }
            }
        }

        /// <summary>
        /// 默认的释放行为
        /// </summary>
        private static void OnReleaseDefault(PoolableStr poolable) => poolable.gameObject.SetActive(false);

        /// <summary>
        /// 默认的获取行为
        /// </summary>
        private static void OnGetDefault(PoolableStr poolable) => poolable.gameObject.SetActive(true);
        
        public void Release(PoolableStr poolable)
        {
            if (_strPoolDict.TryGetValue(poolable.ObjName, out var pool))
                pool.Release(poolable);
        }
    }

    [Serializable]
    public class ObjectUnitStr
    {
        [field: SerializeField]
        public string ObjName { get; set; }

        [field: SerializeField]
        public PoolableStr Prefab { get; set; }
    }
}