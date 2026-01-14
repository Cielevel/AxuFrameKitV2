using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Utility.UseMono
{
    // 所有继承IUseMonoBehaviourCommon的类"实例"，共享一个GameObject挂载一个MonoBehaviour脚本
    // 非MonoBehaviour脚本，也可以使用协程，Update等Unity特性
    // 相比Separate模式，Common模式更轻量，适合大量实例的场景

    /// <summary>
    /// 所有实例共享一个生命周期载体
    /// </summary>
    public interface IUseMonoBehaviourCommon { }

    /// <summary>
    /// 所有实例共享一个生命周期载体，并且可以使用Update
    /// </summary>
    public interface IUseMonoBehaviourCommonUpdate : IUseMonoBehaviourCommon { }

    public static class IUseMonoBehaviourCommonExtension
    {
        private static IMonoHandlerCommon MonoHandler => _monoHandler ??= new MonoHandlerCommon();
        private static IMonoHandlerCommon _monoHandler;

        public static Coroutine StartCoroutine(this IUseMonoBehaviourCommon self, IEnumerator enumerator)
        {
            return MonoHandler.StartCoroutine(enumerator, self);
        }

        public static void StopAllCoroutines(this IUseMonoBehaviourCommon self)
        {
            MonoHandler.StopAllCoroutines(self);
        }

        public static void StopCoroutine(this IUseMonoBehaviourCommon self, Coroutine coroutine)
        {
            MonoHandler.StopCoroutine(coroutine, self);
        }

        public static void RegisterUpdate(this IUseMonoBehaviourCommonUpdate self, Action onUpdate)
        {
            MonoHandler.RegisterUpdate(onUpdate, self);
        }

        public static void RegisterUpdate(this IUseMonoBehaviourCommonUpdate self, Action<float> onUpdate)
        {
            MonoHandler.RegisterUpdate(onUpdate, self);
        }

        public static void ClearUpdate(this IUseMonoBehaviourCommonUpdate self)
        {
            MonoHandler.ClearUpdate(self);
        }

        public static void Destroy(this IUseMonoBehaviourCommon self)
        {
            MonoHandler.Destroy(self);
        }
    }

    public interface IMonoHandlerCommon
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
    /// 控制器，非Mono的一个管理类；通过单一MonoBehaviour提供生成，生命周期功能，销毁等功能
    /// </summary>
    public class MonoHandlerCommon : IMonoHandlerCommon
    {
        private const int DefaultCapacity = 8;

        // 共享的MonoBehaviour载体，延迟初始化
        private MonoCommon _monoCommon;
        private MonoCommon MonoCommon => _monoCommon ? _monoCommon : (_monoCommon = CreateMonoCommon());

        // 存储每个对象关联的协程列表
        private Dictionary<object, List<Coroutine>> _coroutineDict = new(DefaultCapacity);

        // 存储每个对象的Update回调
        private Dictionary<object, Action> _updateDict = new(DefaultCapacity);
        private Dictionary<object, Action<float>> _updateTimeDict = new(DefaultCapacity);

        private MonoCommon CreateMonoCommon()
        {
            var go = new GameObject("[MonoCommon_Shared]");
            Object.DontDestroyOnLoad(go);
            var mono = go.AddComponent<MonoCommon>();
            mono.OnUpdate += ExecuteUpdate;
            return mono;
        }

        private void ExecuteUpdate()
        {
            var deltaTime = Time.deltaTime;

            foreach (var kvp in _updateDict)
            {
                kvp.Value?.Invoke();
            }

            foreach (var kvp in _updateTimeDict)
            {
                kvp.Value?.Invoke(deltaTime);
            }
        }

        public Coroutine StartCoroutine(IEnumerator enumerator, object obj)
        {
            if (_coroutineDict.ContainsKey(obj) == false)
            {
                _coroutineDict.Add(obj, new List<Coroutine>(4));
            }

            var coroutine = MonoCommon.StartCoroutine(enumerator);
            _coroutineDict[obj].Add(coroutine);
            return coroutine;
        }

        public void StopAllCoroutines(object obj)
        {
            if (_coroutineDict.TryGetValue(obj, out var coroutines) == false)
                return;

            foreach (var coroutine in coroutines)
            {
                if (coroutine != null)
                    MonoCommon.StopCoroutine(coroutine);
            }

            coroutines.Clear();
        }

        public void StopCoroutine(Coroutine coroutine, object obj)
        {
            if (coroutine == null)
                return;

            if (_coroutineDict.TryGetValue(obj, out var coroutines))
            {
                coroutines.Remove(coroutine);
            }

            MonoCommon.StopCoroutine(coroutine);
        }

        public void RegisterUpdate(Action onUpdate, object obj)
        {
            if (obj is not IUseMonoBehaviourCommonUpdate)
            {
                Debug.Log("注册失败，对象未实现IUseMonoBehaviourCommonUpdate接口");
                return;
            }

            if (_updateDict.ContainsKey(obj) == false)
                _updateDict.Add(obj, onUpdate);
            else
                _updateDict[obj] += onUpdate;
        }

        public void RegisterUpdate(Action<float> onUpdate, object obj)
        {
            if (obj is not IUseMonoBehaviourCommonUpdate)
            {
                Debug.Log("注册失败，对象未实现IUseMonoBehaviourCommonUpdate接口");
                return;
            }

            if (_updateTimeDict.ContainsKey(obj) == false)
                _updateTimeDict.Add(obj, onUpdate);
            else
                _updateTimeDict[obj] += onUpdate;
        }

        public void ClearUpdate(object obj)
        {
            _updateDict.Remove(obj);
            _updateTimeDict.Remove(obj);
        }

        public void Destroy(object obj)
        {
            // 停止该对象的所有协程
            StopAllCoroutines(obj);
            _coroutineDict.Remove(obj);

            // 清理Update回调
            ClearUpdate(obj);
        }
    }

    /// <summary>
    /// 共享的MonoBehaviour载体，提供生命周期
    /// </summary>
    public class MonoCommon : MonoBehaviour
    {
        public event Action OnUpdate;

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}