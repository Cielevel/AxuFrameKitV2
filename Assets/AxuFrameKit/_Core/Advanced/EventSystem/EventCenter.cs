using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Advanced.EventSystem
{
    /// <summary>
    /// 基于类型系统的事件中心（类型安全）
    /// </summary>
    public static class EventCenter
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new(16);

        /// <summary>
        /// 订阅事件
        /// </summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
            {
                _handlers[type] = new List<Delegate>();
            }
            _handlers[type].Add(handler);
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
            {
                var list = _handlers[type];
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _handlers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                // 倒序遍历，防止回调中移除监听导致的问题
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var handler = list[i] as Action<T>;
                    try
                    {
                        handler?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }
            }
        }

        /// <summary>
        /// 触发事件（无参数构建，用于 struct 事件）
        /// </summary>
        public static void Publish<T>() where T : new()
        {
            Publish(new T());
        }

        /// <summary>
        /// 清理所有事件
        /// </summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
