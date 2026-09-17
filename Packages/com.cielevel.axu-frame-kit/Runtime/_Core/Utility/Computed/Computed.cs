using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Utility.Computed
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

        // 当前依赖的上游节点集合：上游的 _downstream 中持有本实例的 Invalidate 委托（强引用本实例），
        // Dispose / 重算时需据此反注册，否则上游会一直拽着本实例造成泄漏
        private readonly HashSet<IComputedNode> _upstreams = new();

        // 本次求值期间实际访问到的上游（仅求值窗口内非 null，供 NoteUpstream 收集）
        private List<IComputedNode> _collectingUpstreams;

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

                // 若当前有上层 Computed 正在求值，在【本实例】的 downstream 中注册【上层实例】的失效回调，
                // 并让上层记录本实例为它的上游，供其 Dispose / 重算时反注册
                if (ComputedContext.Evaluating.Count > 0)
                {
                    var consumer = ComputedContext.Evaluating.Peek();
                    RegisterDownstream(consumer.Invalidate);
                    consumer.NoteUpstream(this);
                }

                if (!_dirty) return _cached;

                List<IComputedNode> evaluatedUpstreams = new();
                ComputedContext.Evaluating.Push(this);
                _collectingUpstreams = evaluatedUpstreams;
                try
                {
                    _cached = _compute();
                }
                finally
                {
                    _collectingUpstreams = null;
                    ComputedContext.Evaluating.Pop();
                }

                _dirty = false;

                // 同步上游集合：本次未再访问的旧上游说明依赖已变化，反注册其失效回调（防引用滞留与失效失真）
                foreach (var oldUpstream in _upstreams)
                {
                    if (evaluatedUpstreams.Contains(oldUpstream) == false)
                        oldUpstream.UnregisterDownstream(Invalidate);
                }
                _upstreams.Clear();
                foreach (var upstream in evaluatedUpstreams)
                    _upstreams.Add(upstream);

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

        /// <summary>
        /// 上游在求值期间建立依赖时回调：记录到本次求值正在收集的上游集合。
        /// 窗口外的调用属于时序异常，不记录。
        /// </summary>
        public void NoteUpstream(IComputedNode upstream)
        {
            _collectingUpstreams?.Add(upstream);
        }

        private void NotifyDownstream()
        {
            foreach (var cb in _downstream)
                cb();
        }

        /// <summary>
        /// 释放本实例：逐一反注册进上游的失效回调（解除上游对本实例的强引用），并清除所有下游订阅。
        /// 通常在持有此实例的 MonoBehaviour.OnDestroy 中调用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var upstream in _upstreams)
                upstream.UnregisterDownstream(Invalidate);
            _upstreams.Clear();

            _downstream.Clear();
        }
    }
}
