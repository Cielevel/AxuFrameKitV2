using System;
using UnityEngine;

// 与EventCentral完全隔离，无共享代码。

namespace AxuFrameKit.Common.Utility.Event
{
    /// <summary>
    /// EventChannel 非泛型基类，用于解耦。
    /// </summary>
    public abstract class EventChannelCore : ScriptableObject
    {
        public abstract void RaiseEvent();
        public abstract void RaiseEventDefaultValue();
        public abstract IUnRegisterChannel Register(Action action);
        public abstract IUnRegisterChannel RegisterOnce(Action action);
        public abstract IUnRegisterChannel Register(Action action, Func<bool> condition);
        public abstract IUnRegisterChannel RegisterOnce(Action action, Func<bool> condition);
        public abstract void Unregister(Action action);
        public abstract void Clear();
    }

    /// <summary>
    /// 泛型 EventChannel，兼具收发功能。
    /// </summary>
    public abstract class EventChannel<T> : EventChannelCore
    {
        [SerializeField] private T _value; // 最后发送的值 (主要用于调试)

        private Action<T> _onEventRaised;
        private Action _onEventRaisedNoParam;

        /// <summary>
        /// 发送事件 (带参数)
        /// </summary>
        public void RaiseEvent(T value)
        {
            _value = value;
            _onEventRaised?.Invoke(value);
        }

        /// <summary>
        /// 发送事件 (无参)
        /// </summary>
        public override void RaiseEvent() => _onEventRaisedNoParam?.Invoke();

        /// <summary>
        /// 发送事件 (使用最后一次的值)
        /// </summary>
        public override void RaiseEventDefaultValue() => _onEventRaised?.Invoke(_value);

        public IUnRegisterChannel Register(Action<T> action)
        {
            _onEventRaised += action;
            return new ChannelUnRegister(() => Unregister(action));
        }

        public override IUnRegisterChannel Register(Action action)
        {
            _onEventRaisedNoParam += action;
            return new ChannelUnRegister(() => Unregister(action));
        }

        public IUnRegisterChannel RegisterOnce(Action<T> action)
        {
            void Wrapper(T evt)
            {
                action(evt);
                Unregister(Wrapper);
            }
            return Register(Wrapper);
        }

        public IUnRegisterChannel Register(Action<T> action, Predicate<T> condition)
        {
            void Wrapper(T evt)
            {
                if (condition(evt)) action(evt);
            }
            return Register(Wrapper);
        }

        public IUnRegisterChannel RegisterOnce(Action<T> action, Predicate<T> condition)
        {
            void Wrapper(T evt)
            {
                if (condition(evt))
                {
                    action(evt);
                    Unregister(Wrapper);
                }
            }
            return Register(Wrapper);
        }

        public override IUnRegisterChannel RegisterOnce(Action action)
        {
            void Wrapper()
            {
                action();
                Unregister(Wrapper);
            }
            return Register(Wrapper);
        }

        public override IUnRegisterChannel Register(Action action, Func<bool> condition)
        {
            void Wrapper()
            {
                if (condition()) action();
            }
            return Register(Wrapper);
        }

        public override IUnRegisterChannel RegisterOnce(Action action, Func<bool> condition)
        {
            void Wrapper()
            {
                if (condition())
                {
                    action();
                    Unregister(Wrapper);
                }
            }
            return Register(Wrapper);
        }

        public void Unregister(Action<T> action) => _onEventRaised -= action;
        public override void Unregister(Action action) => _onEventRaisedNoParam -= action;

        public override void Clear()
        {
            _onEventRaised = null;
            _onEventRaisedNoParam = null;
        }
    }

    public static class EventChannelExtensions
    {
        /// <summary>
        /// 尝试发送事件 (类型安全检查)
        /// </summary>
        public static bool TryRaise<T>(this EventChannelCore channel, T value)
        {
            if (channel is EventChannel<T> typedChannel)
            {
                typedChannel.RaiseEvent(value);
                return true;
            }

            Debug.LogError($"TryRaise: Channel类型不匹配, Channel:{channel.GetType()}, value:{typeof(T)}");
            return false;
        }

        /// <summary>
        /// 发送事件 (使用默认值)
        /// </summary>
        public static void TryRaiseDefaultValue(this EventChannelCore channel) => channel.RaiseEventDefaultValue();

        /// <summary>
        /// 发送事件 (无参)
        /// </summary>
        public static void TryRaise(this EventChannelCore channel) => channel.RaiseEvent();

        /// <summary>
        /// 尝试注册监听 (类型安全检查)
        /// </summary>
        public static bool TryRegister<T>(this EventChannelCore channel, Action<T> action, out IUnRegisterChannel unRegister)
        {
            if (channel is EventChannel<T> typedChannel)
            {
                unRegister = typedChannel.Register(action);
                return true;
            }

            Debug.LogError($"TryRegister: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}");
            unRegister = null;
            return false;
        }

        /// <summary>
        /// 尝试注册监听 (类型安全检查)，并返回可注销句柄
        /// </summary>
        public static IUnRegisterChannel TryRegister<T>(this EventChannelCore channel, Action<T> action)
        {
            if (channel is EventChannel<T> typedChannel)
            {
                return typedChannel.Register(action);
            }

            Debug.LogError($"TryRegister: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}");
            return null;
        }

        /// <summary>
        /// 注册监听 (无参)
        /// </summary>
        public static IUnRegisterChannel TryRegister(this EventChannelCore channel, Action action) => channel.Register(action);

        public static IUnRegisterChannel TryRegisterOnce<T>(this EventChannelCore channel, Action<T> action)
        {
            if (channel is EventChannel<T> typedChannel) return typedChannel.RegisterOnce(action);
            Debug.LogError($"TryRegisterOnce: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}"); return null;
        }

        public static IUnRegisterChannel TryRegister<T>(this EventChannelCore channel, Action<T> action, Predicate<T> condition)
        {
            if (channel is EventChannel<T> typedChannel) return typedChannel.Register(action, condition);
            Debug.LogError($"TryRegister: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}"); return null;
        }

        public static IUnRegisterChannel TryRegisterOnce<T>(this EventChannelCore channel, Action<T> action, Predicate<T> condition)
        {
            if (channel is EventChannel<T> typedChannel) return typedChannel.RegisterOnce(action, condition);
            Debug.LogError($"TryRegisterOnce: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}"); return null;
        }

        public static IUnRegisterChannel TryRegisterOnce(this EventChannelCore channel, Action action) => channel.RegisterOnce(action);
        public static IUnRegisterChannel TryRegister(this EventChannelCore channel, Action action, Func<bool> condition) => channel.Register(action, condition);
        public static IUnRegisterChannel TryRegisterOnce(this EventChannelCore channel, Action action, Func<bool> condition) => channel.RegisterOnce(action, condition);
        public static void TryClear(this EventChannelCore channel) => channel.Clear();
    }

    #region Auto UnRegister

    public interface IUnRegisterChannel
    {
        void UnRegister();
    }

    public struct ChannelUnRegister : IUnRegisterChannel
    {
        private Action _onUnRegister;
        public ChannelUnRegister(Action unRegister) => _onUnRegister = unRegister;

        public void UnRegister()
        {
            _onUnRegister?.Invoke();
            _onUnRegister = null;
        }
    }

    public abstract class UnRegisterChannelTrigger : MonoBehaviour
    {
        private readonly System.Collections.Generic.HashSet<IUnRegisterChannel> _unRegisters = new(8);

        public IUnRegisterChannel AddUnRegister(IUnRegisterChannel unRegister)
        {
            _unRegisters.Add(unRegister);
            return unRegister;
        }

        public void RemoveUnRegister(IUnRegisterChannel unRegister) => _unRegisters.Remove(unRegister);

        public void UnRegister()
        {
            foreach (var unRegister in _unRegisters)
                unRegister.UnRegister();

            _unRegisters.Clear();
        }
    }

    public class UnRegisterChannelOnDestroyTrigger : UnRegisterChannelTrigger
    {
        private void OnDestroy() => UnRegister();
    }

    public class UnRegisterChannelOnDisableTrigger : UnRegisterChannelTrigger
    {
        private void OnDisable() => UnRegister();
    }

    public class UnRegisterChannelSceneUnloadedTrigger : UnRegisterChannelTrigger
    {
        private static UnRegisterChannelSceneUnloadedTrigger _defaultTrigger;

        public static UnRegisterChannelSceneUnloadedTrigger Get
        {
            get
            {
                if (!_defaultTrigger)
                {
                    _defaultTrigger = new GameObject("UnRegisterChannelSceneUnloadedTrigger").AddComponent<UnRegisterChannelSceneUnloadedTrigger>();
                }

                return _defaultTrigger;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy() => UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene) => UnRegister();
    }

    public static class UnRegisterChannelExtension
    {
        static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var trigger = gameObject.GetComponent<T>();
            if (!trigger) trigger = gameObject.AddComponent<T>();
            return trigger;
        }

        public static IUnRegisterChannel UnRegisterWhenGameObjectDestroyed<T>(this IUnRegisterChannel self, T component)
            where T : Component => self.UnRegisterWhenGameObjectDestroyed(component.gameObject);

        public static IUnRegisterChannel UnRegisterWhenGameObjectDestroyed(this IUnRegisterChannel unRegister, GameObject gameObject) =>
            GetOrAddComponent<UnRegisterChannelOnDestroyTrigger>(gameObject).AddUnRegister(unRegister);

        public static IUnRegisterChannel UnRegisterWhenDisabled<T>(this IUnRegisterChannel self, T component)
            where T : Component => self.UnRegisterWhenDisabled(component.gameObject);

        public static IUnRegisterChannel UnRegisterWhenDisabled(this IUnRegisterChannel unRegister, GameObject gameObject) =>
            GetOrAddComponent<UnRegisterChannelOnDisableTrigger>(gameObject).AddUnRegister(unRegister);

        public static IUnRegisterChannel UnRegisterWhenCurrentSceneUnloaded(this IUnRegisterChannel self) =>
            UnRegisterChannelSceneUnloadedTrigger.Get.AddUnRegister(self);
    }

    #endregion
}
