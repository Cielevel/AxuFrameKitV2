using System;

namespace AxuFrameKit._Core.Utility.Computed
{
    /// <summary>
    /// 内部计算节点接口，用于非泛型的依赖追踪。
    /// </summary>
    public interface IComputedNode
    {
        void Invalidate();
        void RegisterDownstream(Action onInvalidate);
        void UnregisterDownstream(Action onInvalidate);

        /// <summary>
        /// 上游在求值期间建立依赖（注册失效回调）时回调本方法，
        /// 由下游记录该上游，供其 Dispose 或重算时反注册、解除上游对下游的强引用。
        /// </summary>
        void NoteUpstream(IComputedNode upstream);
    }

    public interface IReadOnlyComputed<out T> : IComputedNode
    {
        T Value { get; }
    }
}
