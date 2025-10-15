using AxuFrameKit._Core.Advanced.ObjectPool;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Multiton
{
    /// <summary>
    /// 对象池多例模式，用于需要运行在MonoBehaviour下的纯组件（对于快捷实现，使用IUseMonoBehaviour接口以实现对应特性）
    /// 该多例仅支持无子类的类进行创建构造
    /// </summary>
    public class MultitonAutoMonoR<T> : MonoBehaviour where T : MultitonAutoMonoR<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Transform _parent;
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new object();

        private static readonly SimpleObjPool<T> Instances = new(Construct, OnGet, OnRelease);

        public static T Get() => Instances.Get();

        public static T1 Get<T1>() where T1 : T => Instances.Get() as T1;
        
        public static void Release(T obj) => Instances.Release(obj);
        
        private static T Construct()
        {
            lock (Lock)
            {
                var component = new GameObject($"[Child-{typeof(T).Name}]").AddComponent<T>();
                if (_parent == null)
                    _parent = new GameObject($"[PoolParent-{typeof(T).Name}]").transform;
                component.transform.SetParent(_parent);
                return component;
            }
        }

        private static void OnGet(T obj) => obj.gameObject.SetActive(true);

        private static void OnRelease(T obj) => obj.gameObject.SetActive(false);
    }
}