using System;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 标准C#单例模式基类 (Thread-Safe Lazy implementation)
    /// </summary>
    /// <typeparam name="T">单例类型，需具备公有的无参构造函数</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

        /// <summary>
        /// 获取类的全局单例实例
        /// </summary>
        public static T Instance => _instance.Value;
    }
}
