using System;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Utility.Multiton
{
    public abstract class Multiton<T> : IDisposable where T : Multiton<T>, new()
    {
        public string Key { get; private set; }

        private static readonly object Lock = new();

        private static readonly Dictionary<string, T> Instances = new(8);

        public bool IsDisposed { get; private set; }

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