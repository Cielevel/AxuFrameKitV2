using System;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Utility.Multiton
{
    public abstract class MultitonAuto<T> : IDisposable where T : MultitonAuto<T>, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static ulong _index = 1;
        
        public ulong Key { get; private set; }
        
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new();
        
        private static readonly Dictionary<ulong, T> Instances = new(8);

        public static T GetInstance(ulong key = 0)
        {
            lock (Lock)
            {
                if (key == 0)
                {
                    key = _index++;
                    var instance = new T { Key = key };
                    Instances.Add(key, instance);
                }
                
                if (0 < key && Instances.TryGetValue(key, out var instance1))
                    return instance1;

                return null;
            }
        }
        
        public static void DisposeInstance(ulong key)
        {
            lock (Lock)
            {
                Instances.Remove(key, out var instance);
                instance?.Dispose();
            }
        }

        public static void DisposeAll()
        {
            foreach (var instance in Instances.Values)
                instance.Dispose();
        }

        public abstract void Dispose();
    }
}