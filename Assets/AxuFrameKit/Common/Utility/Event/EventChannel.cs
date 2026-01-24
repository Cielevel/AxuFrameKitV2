using System;
using UnityEngine;
using UnityEngine.Events;

namespace AxuFrameKit.Common.Utility.Event
{
    /// <summary>
    /// EventChannel 非泛型基类，用于解耦。
    /// </summary>
    public abstract class EventChannelCore : ScriptableObject
    {
        public abstract void RaiseEvent();
        public abstract void RaiseEventDefaultValue();
        public abstract void Register(Action action);
        public abstract void Unregister(Action action);
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

        public void Register(Action<T> action) => _onEventRaised += action;
        public override void Register(Action action) => _onEventRaisedNoParam += action;

        public void Unregister(Action<T> action) => _onEventRaised -= action;
        public override void Unregister(Action action) => _onEventRaisedNoParam -= action;
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
        public static bool TryRegister<T>(this EventChannelCore channel, Action<T> action)
        {
            if (channel is EventChannel<T> typedChannel)
            {
                typedChannel.Register(action);
                return true;
            }

            Debug.LogError($"TryRegister: Channel类型不匹配, Channel:{channel.GetType()}, action:{typeof(T)}");
            return false;
        }

        /// <summary>
        /// 注册监听 (无参)
        /// </summary>
        public static void TryRegister(this EventChannelCore channel, Action action) => channel.Register(action);
    }
}
