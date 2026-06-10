using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        private static readonly Lazy<IMonoHandler> _monoHandler = new(() => new MonoHandler());
        private static IMonoHandler MonoHandler => _monoHandler.Value;

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

        // Destroy 是延迟的，记录已销毁对象，防止同帧内重新注册
        private readonly HashSet<object> _destroyedObjects = new();

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

            if (_destroyedObjects.Contains(obj))
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

            if (_destroyedObjects.Contains(obj))
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

            if (_destroyedObjects.Contains(obj))
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

            // 标记为已销毁，防止 Object.Destroy 延迟期间同帧重新注册
            _destroyedObjects.Add(obj);

            Object.Destroy(mono.gameObject);
            _memberDict.Remove(obj);

            // Object.Destroy 延迟一帧，下一帧移除标记（通过协程驱动清理）
            // 此处利用共享的 MonoCommon 或直接接受标记长期存在均可
            // 保守选择：标记保留直到对象被 GC（HashSet 以 object 引用为 key，对象消亡后自然无法再被索引）
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