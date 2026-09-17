using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using AxuFrameKit._Core.Utility.Multiton;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Utility.UseMono
{
    // 每个继承IUseMonoBehaviour的类"实例"，都会有一个GameObject挂载一个MonoBehaviour脚本
    // 非MonoBehaviour脚本，也可以使用协程，Update等Unity特性

    /// <summary>
    /// 每一个实例都是独立生命周期
    /// </summary>
    public interface IUseMonoBehaviourSeparate { }

    /// <summary>
    /// 每一个实例都是独立生命周期，并且可以使用Update
    /// </summary>
    public interface IUseMonoBehaviourUpdate : IUseMonoBehaviourSeparate { }

    public static class IUseMonoBehaviourSeparateExtension
    {
        // 与 Common 对齐，使用 Lazy<T> 保证线程安全的延迟初始化
        private static Lazy<IMonoHandler> _monoHandler = new(() => new MonoHandler());
        private static IMonoHandler MonoHandler => _monoHandler.Value;

        static IUseMonoBehaviourSeparateExtension()
        {
            // 域重载关闭时重建 handler，避免上一轮 Play 已销毁的挂载器残留
            MultitonRegistry.Register(typeof(IUseMonoBehaviourSeparate), ResetHandler);
        }

        internal static void ResetHandler() => _monoHandler = new Lazy<IMonoHandler>(() => new MonoHandler());

        public static Coroutine StartCoroutine(this IUseMonoBehaviourSeparate self, IEnumerator enumerator)
        {
            return MonoHandler.StartCoroutine(enumerator, self);
        }

        public static void StopAllCoroutines(this IUseMonoBehaviourSeparate self)
        {
            MonoHandler.StopAllCoroutines(self);
        }

        public static void StopCoroutine(this IUseMonoBehaviourSeparate self, Coroutine coroutine)
        {
            MonoHandler.StopCoroutine(coroutine, self);
        }

        public static void RegisterUpdate(this IUseMonoBehaviourUpdate self, Action onUpdate)
        {
            MonoHandler.RegisterUpdate(onUpdate, self);
        }

        public static void RegisterUpdate(this IUseMonoBehaviourUpdate self, Action<float> onUpdate)
        {
            MonoHandler.RegisterUpdate(onUpdate, self);
        }

        public static void ClearUpdate(this IUseMonoBehaviourUpdate self)
        {
            MonoHandler.ClearUpdate(self);
        }

        public static void Destroy(this IUseMonoBehaviourSeparate self)
        {
            MonoHandler.Destroy(self);
        }
    }

    public interface IMonoHandler
    {
        Coroutine StartCoroutine(IEnumerator enumerator, object obj);

        void StopAllCoroutines(object obj);

        void StopCoroutine(Coroutine coroutine, object obj);

        void RegisterUpdate(Action onUpdate, object obj);

        void RegisterUpdate(Action<float> onUpdate, object obj);

        void ClearUpdate(object obj);

        void Destroy(object obj);
    }

    /// <summary>
    /// 控制器，非Mono的一个管理类；提供生成，生命周期功能，销毁等功能
    /// </summary>
    public class MonoHandler : IMonoHandler
    {
        private const int DefaultCapacity = 8;

        private readonly Dictionary<object, MonoSingle> _memberDict = new(DefaultCapacity);

        // Destroy 是延迟的，记录已销毁对象，防止同帧内重新注册。
        // 用 ConditionalWeakTable 弱引用标记：HashSet 强引用会导致已销毁对象永远无法被 GC
        private static readonly ConditionalWeakTable<object, object> _destroyedObjects = new();
        private static readonly object _destroyedMarker = new();

        private static bool IsMarkedDestroyed(object obj) => _destroyedObjects.TryGetValue(obj, out _);

        /// <summary>
        /// 确保对象对应的 MonoSingle 已存在，不存在则按类型创建
        /// </summary>
        private MonoSingle EnsureMonoSingle(object obj)
        {
            if (_memberDict.TryGetValue(obj, out var existing))
                return existing;

            MonoSingle mono;
            if (obj is IUseMonoBehaviourUpdate)
                mono = new GameObject($"[MonoSingle_{obj.GetType().Name}_{_memberDict.Count}]").AddComponent<MonoSingleUpdate>();
            else
                mono = new GameObject($"[MonoSingle_{obj.GetType().Name}_{_memberDict.Count}]").AddComponent<MonoSingle>();

            // 与 Common 对齐，场景切换时不销毁
            Object.DontDestroyOnLoad(mono.gameObject);

            _memberDict.Add(obj, mono);
            return mono;
        }

        public Coroutine StartCoroutine(IEnumerator enumerator, object obj)
        {
            if (enumerator == null)
            {
                Debug.LogError("StartCoroutine: enumerator cannot be null.");
                return null;
            }

            if (IsMarkedDestroyed(obj))
            {
                Debug.LogWarning($"StartCoroutine: 对象 {obj.GetType().Name} 已被销毁，无法启动协程。");
                return null;
            }

            return EnsureMonoSingle(obj).StartCoroutine(enumerator);
        }

        public void StopAllCoroutines(object obj)
        {
            if (_memberDict.TryGetValue(obj, out var mono) == false)
                return;

            mono.StopAllCoroutines();
        }

        public void StopCoroutine(Coroutine coroutine, object obj)
        {
            if (coroutine == null)
                return;

            if (_memberDict.TryGetValue(obj, out var mono) == false)
                return;

            mono.StopCoroutine(coroutine);
        }

        public void RegisterUpdate(Action onUpdate, object obj)
        {
            if (onUpdate == null)
            {
                Debug.LogError("RegisterUpdate: onUpdate cannot be null.");
                return;
            }

            // 前置接口检查，与实际类型问题解耦，避免误导性错误信息
            if (obj is not IUseMonoBehaviourUpdate)
            {
                Debug.LogWarning($"RegisterUpdate: 对象 {obj.GetType().Name} 未实现 IUseMonoBehaviourUpdate 接口，注册失败。");
                return;
            }

            if (IsMarkedDestroyed(obj))
            {
                Debug.LogWarning($"RegisterUpdate: 对象 {obj.GetType().Name} 已被销毁，无法注册 Update。");
                return;
            }

            var mono = EnsureMonoSingle(obj);

            // 经过接口检查和 EnsureMonoSingle，此处必然是 MonoSingleUpdate
            if (mono is MonoSingleUpdate monoSingleUpdate)
                monoSingleUpdate.OnUpdate += onUpdate;
        }

        public void RegisterUpdate(Action<float> onUpdate, object obj)
        {
            if (onUpdate == null)
            {
                Debug.LogError("RegisterUpdate: onUpdate cannot be null.");
                return;
            }

            if (obj is not IUseMonoBehaviourUpdate)
            {
                Debug.LogWarning($"RegisterUpdate: 对象 {obj.GetType().Name} 未实现 IUseMonoBehaviourUpdate 接口，注册失败。");
                return;
            }

            if (IsMarkedDestroyed(obj))
            {
                Debug.LogWarning($"RegisterUpdate: 对象 {obj.GetType().Name} 已被销毁，无法注册 Update。");
                return;
            }

            var mono = EnsureMonoSingle(obj);

            if (mono is MonoSingleUpdate monoSingleUpdate)
                monoSingleUpdate.OnUpdateTime += onUpdate;
        }

        public void ClearUpdate(object obj)
        {
            if (_memberDict.TryGetValue(obj, out var mono) == false)
                return;

            if (mono is MonoSingleUpdate monoSingleUpdate)
                monoSingleUpdate.ClearUpdate();
        }

        public void Destroy(object obj)
        {
            if (_memberDict.TryGetValue(obj, out var mono) == false)
                return;

            // 标记为已销毁，防止 Object.Destroy 延迟期间同帧重新注册（弱标记，不阻碍 GC）
            _destroyedObjects.Remove(obj);
            _destroyedObjects.Add(obj, _destroyedMarker);

            Object.Destroy(mono.gameObject);
            _memberDict.Remove(obj);

            // Object.Destroy 延迟一帧；标记为弱引用（ConditionalWeakTable），
            // 对象被 GC 后标记自动消失，不会阻碍回收
        }
    }

    /// <summary>
    /// 挂载器，提供生命周期
    /// </summary>
    public class MonoSingle : MonoBehaviour { }

    /// <summary>
    /// 挂载器，提供生命周期，同时提供 Update 事件
    /// </summary>
    public class MonoSingleUpdate : MonoSingle
    {
        public event Action OnUpdate;
        public event Action<float> OnUpdateTime;

        private void Update()
        {
            OnUpdate?.Invoke();
            OnUpdateTime?.Invoke(Time.deltaTime);
        }

        public void ClearUpdate()
        {
            OnUpdate     = null;
            OnUpdateTime = null;
        }
    }
}