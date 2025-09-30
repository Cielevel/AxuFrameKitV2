using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Multiton
{
    /// <summary>
    /// 多例模式基类（由于是MonoBehaviour，销毁直接调用Destroy即可）
    /// </summary>
    public class MultitonMono<T> : MonoBehaviour where T : MultitonMono<T>
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
        }

        public static void DestroyInstance(string key)
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
