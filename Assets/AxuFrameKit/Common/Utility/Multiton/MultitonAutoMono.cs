using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Multiton
{
    /// <summary>
    /// 多例模式基类（自动化ID）（由于是MonoBehaviour，销毁直接调用Destroy即可）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MultitonAutoMono<T> : MonoBehaviour where T : MultitonAutoMono<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static ulong _index = 1;
        
        // ReSharper disable once StaticMemberInGenericType
        private static Transform _parent;
        
        [field: SerializeField]
        public ulong Key { get; set; }
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new object();
        
        private static readonly Dictionary<ulong, T> Instances = new(16);

        /// <summary>
        /// 略
        /// </summary>
        /// <param name="key">默认不输入，实用自增key；输入时需要大于等于0</param>
        /// <param name="createWhenKeyInvalid">当输入有效key但没有对应有效实例时，创建实例并占位对应key</param>
        /// <returns></returns>
        public static T GetInstance(ulong key = 0, bool createWhenKeyInvalid = false)
        {
            if (key != 0)
            {
                if (Instances.TryGetValue(key, out var instance1) && instance1 != null)
                    return instance1;
            }

            lock (Lock)
            {
                if (key == 0)
                {
                    key = GetValidAutoIndex();
                    var instance = new GameObject($"[{typeof(T).Name}(key={key})]").AddComponent<T>();
                    instance.Key = key;
                    Instances[key] = instance;

                    OnCreate(instance);
                    
                    return instance;
                }

                if (Instances.TryGetValue(key, out var instance2) && instance2 != null)
                    return instance2;

                if (createWhenKeyInvalid)
                {
                    var instance = new GameObject($"[{typeof(T).Name}(key={key})]").AddComponent<T>();
                    instance.Key = key;
                    Instances[key] = instance;
                    OnCreate(instance);
                    return instance;
                }
                
                return null;
            }
        }

        private static ulong GetValidAutoIndex() // 自增index时的验证
        {
            ulong key = _index++;
            while (Instances.ContainsKey(key))
                key = _index++;
            return key;
        }

        private static void OnCreate(T instance)
        {
            if (_parent == null)
                _parent = new GameObject($"[Parent {typeof(T).Name}]").transform;
            
            if (instance != null)
                instance.transform.SetParent(_parent);
        }
        
        public static void DestroyInstance(ulong key)
        {
            lock (Lock)
            {
                if (Instances.TryGetValue(key, out var instance))
                    Destroy(instance.gameObject);
            }
        }
        
        public static void ClearAllInstances()
        {
            lock (Lock)
            {
                foreach (var instance in Instances.Values)
                {
                    if (instance != null)
                        Destroy(instance.gameObject);
                }
                Instances.Clear();
            }
        }
        
        private void OnDestroy()
        {
            lock (Lock)
                Instances.Remove(Key);
        }
    }
}