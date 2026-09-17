using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Utility.Multiton
{
    public abstract class MultitonAuto<T> : IDisposable where T : MultitonAuto<T>, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static ulong _index = 1;

        public ulong Key { get; private set; }

        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new();

        private static readonly Dictionary<ulong, T> Instances = new(8);

        static MultitonAuto() => MultitonRegistry.Register(typeof(T), ClearAllInstances);

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 清理当前多例类型的全部实例（逐个 Dispose），供域重载关闭时统一重置使用。
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

        public static T GetInstance(ulong key = 0)
        {
            if (key != 0)
            {
                if (Instances.TryGetValue(key, out var instance1))
                    return instance1;
            }

            lock (Lock)
            {
                if (key == 0)
                {
                    key = _index++;
                    var instance = new T { Key = key };
                    Instances.Add(key, instance);
                    return instance;
                }

                if (Instances.TryGetValue(key, out var instance2))
                    return instance2;

                return null;
            }
        }

        public static void DisposeInstance(ulong key)
        {
            lock (Lock)
            {
                if (Instances.Remove(key, out var instance))
                    instance?.Dispose();
            }
        }

        public static void DisposeAll() => ClearAllInstances();

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            lock (Lock)
                Instances.Remove(Key);

            OnDispose();
        }

        protected abstract void OnDispose();
    }
}