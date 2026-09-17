using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Utility.Multiton
{
    public abstract class Multiton<T> : IDisposable where T : Multiton<T>, new()
    {
        public string Key { get; private set; }

        private static readonly object Lock = new();

        private static readonly Dictionary<string, T> Instances = new(8);

        static Multiton() => MultitonRegistry.Register(typeof(T), ClearAllInstances);

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 清理当前多例类型的全部实例（逐个 Dispose）。
        /// 供宿主在域重载关闭时统一重置静态多例使用（见 Common 层 AxuStaticResetGuard）。
        /// </summary>
        public static void ClearAllInstances()
        {
            T[] snapshot;
            lock (Lock)
            {
                if (Instances.Count == 0)
                    return;

                snapshot = new T[Instances.Count];
                Instances.Values.CopyTo(snapshot, 0);
            }

            foreach (var instance in snapshot)
                instance?.Dispose();
        }

        public static T GetInstance(string key)
        {
            if (Instances.TryGetValue(key, out var instance))
                return instance;

            lock (Lock)
            {
                if (Instances.TryGetValue(key, out instance) == false)
                {
                    instance = new T { Key = key };
                    Instances.Add(key, instance);
                }

                return instance;
            }
        }

        public static void DisposeInstance(string key)
        {
            lock (Lock)
            {
                if (Instances.Remove(key, out var instance))
                    instance?.OnDispose();
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            lock (Lock)
                Instances.Remove(Key);

            OnDispose();
        }

        public abstract void OnDispose();
    }
}