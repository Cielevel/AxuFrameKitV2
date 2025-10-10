using System;
using AxuFrameKit.Common.CustomAttribute;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Singleton
{
    public interface ISingleton
    {
        
    }
    
    public abstract class SingletonLazy<T> : ISingleton where T : SingletonLazy<T>, new()
    {
        public static T Instance => _instance.Value;
        private static Lazy<T> _instance = new(() => new T());

#if UNITY_EDITOR
        static SingletonLazy()
        {
            // 静态构造函数，只在类型首次使用时调用一次
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            // 退出Play模式时清理
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                if (_instance.IsValueCreated)
                    _instance.Value?.OnDestroy();
                _instance = new Lazy<T>(() => new T());
            }
        }
#endif
        
        protected virtual void OnDestroy() { }
    }

    // TODO 分离singletonLazy，切换场景不销毁，切换场景销毁；但editor下，停止运行都会销毁

    // TODO MonoBehaviour的单例还可以继续补全
    public abstract class SingletonMono<T> : MonoBehaviour, ISingleton where T : SingletonMono<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            // ReSharper disable once Unity.UnityObjectNullComparison
            if (Instance == null) // 不能使用if(Instance)，否则会出错
                Instance = GetComponent<T>();
            else
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// 单例Mono类基类，可使用自定义Attribute
    /// </summary>
    public abstract class SingletonCustomMono<T> : CustomMonoBehaviour, ISingleton where T : SingletonCustomMono<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            // ReSharper disable once Unity.UnityObjectNullComparison
            if (Instance == null) // 不能使用if(Instance)，否则会出错
                Instance = GetComponent<T>();
            else
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Cr-->Create GameObject，可直接获取单例，如果单例不存在则创建一个物体挂载该单例脚本，该物体会随切换场景而销毁
    /// </summary>
    public abstract class SingletonMonoCr<T> : MonoBehaviour, ISingleton where T : SingletonMonoCr<T>
    {
        private static object _lock = new();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    // ReSharper disable once Unity.UnityObjectNullComparison
                    if (_instance == null) // 不能使用if(_instance)，否则会出错
                    {
                        var go = new GameObject($"[{typeof(T).Name}]");
                        _instance = go.AddComponent<T>();
                    }

                    return _instance;
                }
            }
        }

        private static T _instance;

        protected virtual void Awake()
        {
            // ReSharper disable once Unity.UnityObjectNullComparison
            if (_instance == null) // 不能使用if(_instance)，否则会出错
            {
                _instance = this as T;
            }
        }
    }

    /// <summary>
    /// Cr-->Create GameObject，Ds-->DontDestroyOnLoad，可直接获取单例，如果单例不存在则创建一个物体挂载该单例脚本，该单例不会随切换场景而销毁
    /// </summary>
    public abstract class SingletonMonoCrDs<T> : SingletonMonoCr<T> where T : SingletonMonoCrDs<T>
    {
        protected override void Awake()
        {
            base.Awake();
            
            // ReSharper disable once Unity.UnityObjectNullComparison
            if (transform.parent != null)
                transform.SetParent(null);
            
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 自动创建到场景的不销毁的单例类，该单例类不建议在其他monoBehaviour中的awake中调用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    // public class SingletonAutoCrDs<T> : MonoBehaviour where T : SingletonAutoCrDs<T>
    // {
    //     public static T Instance { get; private set; }
    //
    //     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    //     private static void AutoCreate()
    //     {
    //         DebugEx.Log("将创建单例到场景：" + typeof(T).Name);
    //         if (Instance == null)
    //         {
    //             var go = new GameObject($"[{typeof(T).Name}]");
    //             Instance = go.AddComponent<T>();
    //             DontDestroyOnLoad(go);
    //         }
    //     }
    // }
}