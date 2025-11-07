using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PreTestFunc.EventSystem
{
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