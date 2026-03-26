using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxuExperimental.EventSystem
{
    /// <summary>
    /// 实验性 EventBus 实现，尚未同步到 Core 层。
    /// 已知问题见下方 ISSUE 列表，修复后再考虑迁移。
    /// </summary>
    /// <remarks>
    /// ISSUE-1 [严重] Subscribe 带 filter 参数时无法取消订阅
    ///   原因：filter 包装生成的 Wrapper 委托引用在注册后丢失，
    ///         Unsubscribe 持有的是原始 listener，Delegate.Remove 无法匹配。
    ///   修复方向：Subscribe 改为返回 IDisposable，由闭包持有 Wrapper 引用。
    ///
    /// ISSUE-2 [严重] Publish 使用多播委托，单个订阅者抛出异常会中断后续订阅者的调用
    ///   原因：((Action-T-)del).Invoke(evt) 是多播委托整体调用，
    ///         任意一个订阅者异常会导致后续订阅者不被执行。
    ///   修复方向：改用 del.GetInvocationList() 逐个调用，每个单独 try/catch。
    ///
    /// ISSUE-3 [中] WaitFor 没有取消机制
    ///   原因：TaskCompletionSource 注册后若事件永远不触发，
    ///         tcs 和 waiter 条目会永远留在 _waiters 字典中，无法释放。
    ///   修复方向：WaitFor 增加 CancellationToken 参数，
    ///             取消时调用 tcs.TrySetCanceled() 并从 _waiters 中移除。
    ///
    /// ISSUE-4 [低] SubscribeOnce 在高频事件下存在重复触发的边缘风险
    ///   原因：wrapper 执行期间若同一事件再次 Publish，
    ///         Unsubscribe 尚未完成，wrapper 可能被调用两次。
    ///   备注：Unity 单线程环境下概率极低，但并发场景需注意。
    ///
    /// ISSUE-5 [低] 静态字典在 Unity Editor Domain Reload 时不会自动清空
    ///   原因：静态成员在域重载时残留，上一次 PlayMode 的订阅者会污染下一次运行。
    ///   修复方向：添加 [RuntimeInitializeOnLoadMethod] 在子系统注册阶段调用 Clear()，
    ///             仅在 UNITY_EDITOR 下启用。
    /// </remarks>
    public static class EventBus
    {
        // delegate storage per event type
        private static readonly Dictionary<Type, Delegate> _listeners = new();

        // Waiters for WaitFor<T>()
        private static readonly Dictionary<Type, List<object>> _waiters = new();

        // Subscribe with optional predicate filter (null means accept all)
        public static void Subscribe<T>(Action<T> listener, Predicate<T> filter = null) where T : IEvent
        {
            if (filter == null)
            {
                AddDelegate(typeof(T), listener);
            }
            else
            {
                // wrap listener with filter-check
                void Wrapper(T evt)
                {
                    if (filter(evt)) listener(evt);
                }

                AddDelegate(typeof(T), (Action<T>)Wrapper);
            }
        }

        private static void AddDelegate(Type t, Delegate del)
        {
            if (_listeners.TryGetValue(t, out var existing))
                _listeners[t] = Delegate.Combine(existing, del);
            else
                _listeners[t] = del;
        }

        // Unsubscribe
        public static void Unsubscribe<T>(Action<T> listener) where T : IEvent
        {
            var t = typeof(T);
            if (_listeners.TryGetValue(t, out var existing))
            {
                var updated = Delegate.Remove(existing, listener);
                if (updated == null) _listeners.Remove(t);
                else _listeners[t] = updated;
            }
        }

        // Publish
        public static void Publish<T>(T evt) where T : IEvent
        {
            var t = typeof(T);
            if (_listeners.TryGetValue(t, out var del))
            {
                // cast and invoke
                try
                {
                    ((Action<T>)del)?.Invoke(evt);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            // Resolve waiters (first-in list)
            if (_waiters.TryGetValue(t, out var list))
            {
                // copy to avoid modification during iteration
                var copy = list.ToArray();
                _waiters.Remove(t);
                foreach (var o in copy)
                {
                    if (o is TaskCompletionSource<T> tcs)
                    {
                        tcs.TrySetResult(evt);
                    }
                }
            }
        }

        // Subscribe once: automatically unsubscribes after first call
        public static void SubscribeOnce<T>(Action<T> listener) where T : IEvent
        {
            Action<T> wrapper = null;
            wrapper = (T evt) =>
            {
                try { listener(evt); }
                finally { Unsubscribe(wrapper); }
            };
            Subscribe(wrapper);
        }

        // Generic Unsubscribe overload used by SubscribeOnce wrapper
        // private static void Unsubscribe<T>(Action<T> listener) where T : IEvent
        // {
        //     Unsubscribe<T>(listener);
        // }

        // Async wait for next event of type T
        public static Task<T> WaitFor<T>() where T : IEvent
        {
            var t   = typeof(T);
            var tcs = new TaskCompletionSource<T>();
            if (!_waiters.TryGetValue(t, out var list))
            {
                list        = new List<object>();
                _waiters[t] = list;
            }
            list.Add(tcs);
            return tcs.Task;
        }

        // Clear all registrations (useful for tests / domain reload scenarios)
        public static void Clear()
        {
            _listeners.Clear();
            _waiters.Clear();
        }
    }
}