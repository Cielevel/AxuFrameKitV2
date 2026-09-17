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
                // 若当前有正在进行的 Computed 计算，则建立依赖关系，
                // 并让下游记录本源头为上游（供其 Dispose / 重算时反注册）
                if (ComputedContext.Evaluating.Count > 0)
                {
                    var consumer = ComputedContext.Evaluating.Peek();
                    RegisterDownstream(consumer.Invalidate);
                    consumer.NoteUpstream(this);
                }
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

        /// <summary>源头不求值、无上游，无需记录依赖</summary>
        public void NoteUpstream(IComputedNode upstream) { }

        private void NotifyDownstream()
        {
            foreach (var cb in _downstream)
                cb();
        }
    }
}
