using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Utility.Computed
{
    /// <summary>
    /// 可读写的源头值。
    /// 值变化时自动通知所有下游 <see cref="Computed{T}"/> 失效。
    /// </summary>
    public class ComputedSource<T> : IComputed<T>
    {
        private T _value;
        private readonly HashSet<Action> _downstream = new();

        public ComputedSource(T initial = default)
        {
            _value = initial;
        }

        public T Value
        {
            get
            {
                // 若当前有正在进行的 Computed 计算，则建立依赖关系
                if (ComputedContext.Evaluating.Count > 0)
                    RegisterDownstream(ComputedContext.Evaluating.Peek().Invalidate);
                return _value;
            }
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                _value = value;
                NotifyDownstream();
            }
        }

        public void Invalidate() => NotifyDownstream();

        public void RegisterDownstream(Action onInvalidate) => _downstream.Add(onInvalidate);

        public void UnregisterDownstream(Action onInvalidate) => _downstream.Remove(onInvalidate);

        private void NotifyDownstream()
        {
            foreach (var cb in _downstream)
                cb();
        }
    }
}
