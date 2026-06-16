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
    }

    public interface IReadOnlyComputed<out T> : IComputedNode
    {
        T Value { get; }
    }
}
