using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Multiton
{
    /// <summary>
    /// 多例模式基类（由于是MonoBehaviour，销毁直接调用Destroy即可）
    /// </summary>
    public abstract class MultitonMono<T> : MonoBehaviour where T : MultitonMono<T>
    {
        [field: SerializeField]
        public string Key { get; set; }

        // ReSharper disable once StaticMemberInGenericType
        private static Transform _parent;
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new();

        private static readonly Dictionary<string, T> Instances = new(8);

        public static T GetInstance(string key)
        {
            lock (Lock)
            {
                if (Instances.ContainsKey(key) == false)
                {
                    var instance = new GameObject($"[{typeof(T).Name}(key={key})]").AddComponent<T>();
                    instance.Key = key;
                    Instances.Add(key, instance);
                    
                    OnCreate(instance);
                }

                return Instances[key];
            }
        }
        
        private static void OnCreate(T instance)
        {
            if (_parent == null)
                _parent = new GameObject($"[Parent {typeof(T).Name}]").transform;
            instance.transform.SetParent(_parent);
            instance.OnThisCreated();
        }

        public static void DestroyInstance(string key)
        {
            lock (Lock)
            {
                if (Instances.TryGetValue(key, out var instance))
                {
                    instance.OnThisDestroyed();
                    Destroy(instance.gameObject);
                }
            }
        }
        
        public static void ClearAllInstances()
        {
            lock (Lock)
            {
                foreach (var instance in Instances.Values.Where(instance => instance != null))
                {
                    instance.OnThisDestroyed();
                    Destroy(instance.gameObject);
                }

                Instances.Clear();
            }
        }
        
        private void OnDestroy()
        {
            // 理论上此处是不需要锁的，但是Instances在其他同步块内会使用，所以依旧保持锁。
            lock (Lock)
                Instances.Remove(Key);
        }
        
        protected virtual void OnThisCreated() { }
        protected virtual void OnThisDestroyed() { }
    }
}
