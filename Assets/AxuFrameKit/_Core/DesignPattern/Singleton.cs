using System;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 标准C#单例模式基类 (Thread-Safe Lazy implementation)
    /// </summary>
    public abstract class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

        public static T Instance => _instance.Value;
    }
}
