using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// QFramework的事件系统，作者：liangxie。
// 代码做过命名与结构的调整。
namespace AxuFrameKit.Common.Utility.Event
{
    /// <summary>
    /// 静态事件Bus，可直接使用。
    /// </summary>
    public static class EventBus
    {
        private static readonly EventContainer Container = new();

        public static void Send<T>() where T : new() => Container.GetEvent<EventUnit<T>>()?.Trigger(new T());

        public static void Send<T>(T e) => Container.GetEvent<EventUnit<T>>()?.Trigger(e);

        public static IUnRegister Register<T>(Action<T> onEvent) =>
            Container.GetOrAddEvent<EventUnit<T>>().Register(onEvent);

        public static void UnRegister<T>(Action<T> onEvent)
        {
            var e = Container.GetEvent<EventUnit<T>>();
            e?.UnRegister(onEvent);
        }
    }

    #region Interface

    public interface IUnRegister // 可注销接口
    {
        void UnRegister();
    }

    public interface IEvent // Event基础
    {
        IUnRegister Register(Action onEvent);
    }

    #endregion

    #region Auto UnRegister

    public abstract class UnRegisterTrigger : MonoBehaviour
    {
        private readonly HashSet<IUnRegister> _unRegisters = new HashSet<IUnRegister>();

        public IUnRegister AddUnRegister(IUnRegister unRegister)
        {
            _unRegisters.Add(unRegister);
            return unRegister;
        }

        public void RemoveUnRegister(IUnRegister unRegister) => _unRegisters.Remove(unRegister);

        public void UnRegister()
        {
            foreach (var unRegister in _unRegisters)
            {
                unRegister.UnRegister();
            }

            _unRegisters.Clear();
        }
    }

    public class UnRegisterOnDestroyTrigger : UnRegisterTrigger
    {
        private void OnDestroy()
        {
            UnRegister();
        }
    }

    public class UnRegisterOnDisableTrigger : UnRegisterTrigger
    {
        private void OnDisable()
        {
            UnRegister();
        }
    }

    public class UnRegisterCurrentSceneUnloadedTrigger : UnRegisterTrigger
    {
        private static UnRegisterCurrentSceneUnloadedTrigger DefaultTrigger;

        public static UnRegisterCurrentSceneUnloadedTrigger Get
        {
            get
            {
                if (!DefaultTrigger)
                {
                    DefaultTrigger = new GameObject("UnRegisterCurrentSceneUnloadedTrigger").AddComponent<UnRegisterCurrentSceneUnloadedTrigger>();
                }

                return DefaultTrigger;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            hideFlags                  =  HideFlags.HideInHierarchy;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy() => SceneManager.sceneUnloaded -= OnSceneUnloaded;
        void OnSceneUnloaded(Scene scene) => UnRegister();
    }

    public static class UnRegisterExtension
    {
        static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var trigger = gameObject.GetComponent<T>();

            if (!trigger)
            {
                trigger = gameObject.AddComponent<T>();
            }

            return trigger;
        }

        public static IUnRegister UnRegisterWhenGameObjectDestroyed(this IUnRegister unRegister,
                                                                    UnityEngine.GameObject gameObject) =>
            GetOrAddComponent<UnRegisterOnDestroyTrigger>(gameObject)
                .AddUnRegister(unRegister);

        public static IUnRegister UnRegisterWhenGameObjectDestroyed<T>(this IUnRegister self, T component)
            where T : UnityEngine.Component =>
            self.UnRegisterWhenGameObjectDestroyed(component.gameObject);

        public static IUnRegister UnRegisterWhenDisabled<T>(this IUnRegister self, T component)
            where T : UnityEngine.Component =>
            self.UnRegisterWhenDisabled(component.gameObject);

        public static IUnRegister UnRegisterWhenDisabled(this IUnRegister unRegister,
                                                         UnityEngine.GameObject gameObject) =>
            GetOrAddComponent<UnRegisterOnDisableTrigger>(gameObject)
                .AddUnRegister(unRegister);

        public static IUnRegister UnRegisterWhenCurrentSceneUnloaded(this IUnRegister self) =>
            UnRegisterCurrentSceneUnloadedTrigger.Get.AddUnRegister(self);
    }

    #endregion

    public struct CommonUnRegister : IUnRegister
    {
        private Action OnUnRegister { get; set; }
        public CommonUnRegister(Action unRegister) => OnUnRegister = unRegister;

        public void UnRegister()
        {
            OnUnRegister.Invoke();
            OnUnRegister = null;
        }
    }

    /// <summary>
    /// 常规-事件-单元
    /// </summary>
    public class EventUnit : IEvent
    {
        private Action _onEvent = () => { };

        public IUnRegister Register(Action onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => { UnRegister(onEvent); });
        }

        public void UnRegister(Action onEvent) => _onEvent -= onEvent;

        public void Trigger() => _onEvent?.Invoke();
    }

    /// <summary>
    /// 泛型-事件-单元
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventUnit<T> : IEvent
    {
        private Action<T> _onEvent = e => { };

        public IUnRegister Register(Action<T> onEvent)
        {
            _onEvent += onEvent;
            return new CommonUnRegister(() => { UnRegister(onEvent); });
        }

        public void UnRegister(Action<T> onEvent) => _onEvent -= onEvent;


        public void Trigger(T t) => _onEvent?.Invoke(t);

        IUnRegister IEvent.Register(Action onEvent)
        {
            return Register(Action);
            void Action(T _) => onEvent();
        }
    }

    // 中间间
    // 负责存储IEvent
    public class EventContainer
    {
        private const int DefaultCapacity = 16;

        private readonly Dictionary<Type, IEvent> _typeEvents = new(DefaultCapacity);

        public void Register<T>() where T : IEvent, new() => AddEvent<T>();

        private void AddEvent<T>() where T : IEvent, new() => _typeEvents.Add(typeof(T), new T());

        public T GetEvent<T>() where T : IEvent => _typeEvents.TryGetValue(typeof(T), out var e) ? (T)e : default;

        public T GetOrAddEvent<T>() where T : IEvent, new()
        {
            var eType = typeof(T);
            if (_typeEvents.TryGetValue(eType, out var e))
                return (T)e;

            var t = new T();
            _typeEvents.Add(eType, t);
            return t;
        }
    }
}