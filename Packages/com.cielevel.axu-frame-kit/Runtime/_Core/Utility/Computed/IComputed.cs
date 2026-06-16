namespace AxuFrameKit._Core.Utility.Computed
{
    public interface IComputed<T> : IReadOnlyComputed<T>
    {
        new T Value { get; set; }
    }
}
