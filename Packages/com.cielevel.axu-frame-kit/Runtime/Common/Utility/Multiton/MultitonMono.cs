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

        private bool _isDestroyed = false;

        public static T GetInstance(string key)
        {
            if (Instances.TryGetValue(key, out var instance) && instance != null)
                return instance;

            lock (Lock)
            {
                if (Instances.TryGetValue(key, out instance) == false || instance == null)
                {
                    instance = new GameObject($"[{typeof(T).Name}(key={key})]").AddComponent<T>();
                    instance.Key = key;
                    Instances[key] = instance;

                    OnCreate(instance);
                }

                return instance;
            }
        }

        private static void OnCreate(T instance)
        {
            if (_parent == null)
                _parent = new GameObject($"[Parent {typeof(T).Name}]").transform;

            if (instance != null)
            {
                instance.transform.SetParent(_parent);
                instance.OnThisCreated();
            }
        }

        public static void DestroyInstance(string key)
        {
            lock (Lock)
            {
                if (Instances.TryGetValue(key, out var instance))
                {
                    if (instance != null)
                    {
                        if (!instance._isDestroyed)
                        {
                            instance._isDestroyed = true;
                            instance.OnThisDestroyed();
                        }
                        Destroy(instance.gameObject);
                    }
                }
            }
        }

        public static void ClearAllInstances()
        {
            lock (Lock)
            {
                foreach (var instance in Instances.Values.Where(instance => instance != null))
                {
                    if (!instance._isDestroyed)
                    {
                        instance._isDestroyed = true;
                        instance.OnThisDestroyed();
                    }
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

            if (_isDestroyed)
                return;
            
            _isDestroyed = true;
            OnThisDestroyed();
        }

        protected virtual void OnThisCreated() { }
        protected virtual void OnThisDestroyed() { }
    }
}
