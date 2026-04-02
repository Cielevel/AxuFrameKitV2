using System;
using System.Collections.Generic;

namespace AxuFrameKit.Core.Utility.Computed
{
    /// <summary>
    /// 只读派生值。
    /// 首次求值时自动追踪依赖的上游，上游失效时自动连锁失效，读取时惰性重算。
    /// </summary>
    /// <remarks>
    /// 不再使用时请调用 <see cref="Dispose"/> 以清除上游订阅，避免内存泄漏。
    /// </remarks>
    internal static class ComputedContext
    {
        [ThreadStatic]
        private static Stack<IComputedNode> _evaluating;
        public static Stack<IComputedNode> Evaluating => _evaluating ??= new Stack<IComputedNode>();
    }

    /// <summary>
    /// 只读派生值。
    /// 首次求值时自动追踪依赖的上游，上游失效时自动连锁失效，读取时惰性重算。
    /// </summary>
    /// <remarks>
    /// 不再使用时请调用 <see cref="Dispose"/> 以清除上游订阅，避免内存泄漏。
    /// </remarks>
    public class Computed<T> : IReadOnlyComputed<T>, IDisposable
    {
        private readonly Func<T> _compute;
        private T _cached;
        private bool _dirty = true;
        private bool _disposed = false;

        private readonly HashSet<Action> _downstream = new();

        public Computed(Func<T> compute)
        {
            _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        }

        public T Value
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Computed<T>));

                // 若当前有上层 Computed 正在求值，在【本实例】的 downstream 中注册【上层实例】的失效回调
                if (ComputedContext.Evaluating.Count > 0)
                    RegisterDownstream(ComputedContext.Evaluating.Peek().Invalidate);

                if (!_dirty) return _cached;

                ComputedContext.Evaluating.Push(this);
                try
                {
                    _cached = _compute();
                }
                finally
                {
                    ComputedContext.Evaluating.Pop();
                }

                _dirty = false;
                return _cached;
            }
        }

        /// <summary>
        /// 将本实例标记为脏，并向所有下游传播失效。
        /// </summary>
        public void Invalidate()
        {
            if (_dirty) return;
            _dirty = true;
            NotifyDownstream();
        }

        public void RegisterDownstream(Action onInvalidate) => _downstream.Add(onInvalidate);

        public void UnregisterDownstream(Action onInvalidate) => _downstream.Remove(onInvalidate);

        private void NotifyDownstream()
        {
            foreach (var cb in _downstream)
                cb();
        }

        /// <summary>
        /// 释放本实例，清除所有下游订阅。
        /// 通常在持有此实例的 MonoBehaviour.OnDestroy 中调用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _downstream.Clear();
        }
    }
}
