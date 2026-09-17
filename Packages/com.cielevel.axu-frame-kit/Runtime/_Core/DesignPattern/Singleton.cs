using System;
using AxuFrameKit._Core.Utility.Multiton;

namespace AxuFrameKit._Core.DesignPattern
{
    /// <summary>
    /// 标准C#单例模式基类 (Thread-Safe Lazy implementation)
    /// </summary>
    /// <typeparam name="T">单例类型，需具备公有的无参构造函数</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        // 非 readonly：域重载关闭时需要由 MultitonRegistry 统一重置
        private static Lazy<T> _instance = new Lazy<T>(() => new T());

        static Singleton() => MultitonRegistry.Register(typeof(T), ResetInstance);

        /// <summary>
        /// 重置单例实例（每次进入 Play 前由 MultitonRegistry 统一调用，防止跨 Play 残留旧状态）
        /// </summary>
        private static void ResetInstance() => _instance = new Lazy<T>(() => new T());

        /// <summary>
        /// 获取类的全局单例实例
        /// </summary>
        public static T Instance => _instance.Value;
    }
}
