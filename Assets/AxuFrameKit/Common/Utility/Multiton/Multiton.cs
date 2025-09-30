using System;
using System.Collections.Generic;

namespace AxuFrameKit.Common.Utility.Multiton
{
    public abstract class Multiton<T> : IDisposable where T : Multiton<T>, new()
    {
        public string Key { get; private set; }
        
        private static readonly object Lock = new();
        
        private static readonly Dictionary<string, T> Instances = new(8);

        public static T GetInstance(string key)
        {
            lock (Lock)
            {
                if (Instances.ContainsKey(key) == false)
                {
                    var instance = new T { Key = key };
                    Instances.Add(key, instance);
                }

                return Instances[key];
            }
        }
        
        public static void DisposeInstance(string key)
        {
            lock (Lock)
            {
                Instances.Remove(key, out var instance);
                instance?.Dispose();
            }
        }

        public abstract void Dispose();
    }
}