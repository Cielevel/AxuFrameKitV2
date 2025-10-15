using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.UseMono
{
    // 每个继承IUseMonoBehaviour的类“实例”，都会有一个GameObject挂载一个MonoBehaviour脚本
    // 非MonoBehaviour脚本，也可以使用协程，Update等Unity特性
    
    /// <summary>
    /// 每一个实例都是独立生命周期
    /// </summary>
    public interface IUseMonoBehaviourSeparate { } 

    public static class IUseMonoBehaviourSeparateExtension
    {
        private static IMonoHandler MonoHandler => _monoHandler ??= new MonoHandler();
        private static IMonoHandler _monoHandler;
        
        public static Coroutine StartCoroutine(this IUseMonoBehaviourSeparate self, IEnumerator enumerator)
        {
            return MonoHandler.StartCoroutine(enumerator, self);
        }
        
        public static void StopAllCoroutines(this IUseMonoBehaviourSeparate self)
        {
            MonoHandler.StopAllCoroutines(self);
        }
        
        public static void StopCoroutine(this IUseMonoBehaviourSeparate self, Coroutine coroutine)
        {
            MonoHandler.StopCoroutine(coroutine, self);
        }
        
        public static void Destroy(this IUseMonoBehaviourSeparate self)
        {
            MonoHandler.Destroy(self);
        }
    }

    public interface IMonoHandler
    {
        Coroutine StartCoroutine(IEnumerator enumerator, object obj);

        void StopAllCoroutines(object obj);
        
        void StopCoroutine(Coroutine coroutine, object obj);

        void Destroy(object obj);
    }
    
    /// <summary>
    /// 控制器，提供生成，生命周期功能，销毁等功能
    /// </summary>
    public class MonoHandler : IMonoHandler
    {
        private const int DefaultCapacity = 8;
        
        private Dictionary<object, MonoSingle> _memberDict = new(DefaultCapacity);
        
        public Coroutine StartCoroutine(IEnumerator enumerator, object obj)
        {
            if (_memberDict.ContainsKey(obj) == false)
                _memberDict.Add(obj, new GameObject($"[MonoSingle_{obj.GetType().Name}_{_memberDict.Count}]").AddComponent<MonoSingle>());
            
            return _memberDict[obj].StartCoroutine(enumerator);
        }

        public void StopAllCoroutines(object obj)
        {
            if (_memberDict.ContainsKey(obj) == false)
                return;
            
            _memberDict[obj].StopAllCoroutines();
        }

        public void StopCoroutine(Coroutine coroutine, object obj)
        {
            if (_memberDict.ContainsKey(obj) == false)
                return;
            
            _memberDict[obj].StopCoroutine(coroutine);
        }

        public void Destroy(object obj)
        {
            if (_memberDict.TryGetValue(obj, out var monoSingle) == false)
                return;
            
            Object.Destroy(monoSingle.gameObject);
            _memberDict.Remove(obj);
        }
    }

    /// <summary>
    /// 挂载器，提供生命周期
    /// </summary>
    public class MonoSingle : MonoBehaviour { }
}