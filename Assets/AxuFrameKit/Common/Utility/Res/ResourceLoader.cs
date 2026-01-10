using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Utility.Res
{
    public static class ResourceLoader
    {
        /// <summary>
        /// 使用Resources加载指定路径的资源 (Async/Task)
        /// </summary>
        public static async Task<T> LoadAsync<T>(string path) where T : Object
        {
            var req = Resources.LoadAsync<T>(path);
            await req;
            return req.asset as T;
        }

        /// <summary>
        /// 使用Resources加载指定路径的资源 (Coroutine)
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        public static IEnumerator LoadCoroutine<T>(string path, Action<T> callback) where T : Object
        {
            var req = Resources.LoadAsync<T>(path);
            yield return req;
            callback?.Invoke(req.asset as T);
        }

        /// <summary>
        /// 使用Resources加载指定路径的GameObject，实例化一个物体，并返回它的一个指定组件 (Async/Task)
        /// </summary>
        public static async Task<T> LoadGameObjectComponentAsync<T>(string path, bool isChildComponent = false) where T : Component
        {
            var req = Resources.LoadAsync<GameObject>(path);
            await req;

            if (req.asset is GameObject prefab)
            {
                var go = Object.Instantiate(prefab);
                return isChildComponent ? go.GetComponentInChildren<T>() : go.GetComponent<T>();
            }

            return null;
        }

        /// <summary>
        /// 使用Resources加载指定路径的GameObject，实例化一个物体，并返回它的一个指定组件 (Coroutine)
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        /// <param name="isChildComponent">是否在子物体中查找组件</param>
        public static IEnumerator LoadGameObjectComponentCoroutine<T>(string path, Action<T> callback, bool isChildComponent = false) where T : Component
        {
            var req = Resources.LoadAsync<GameObject>(path);
            yield return req;

            if (req.asset is GameObject prefab)
            {
                var go = Object.Instantiate(prefab);
                var component = isChildComponent ? go.GetComponentInChildren<T>() : go.GetComponent<T>();
                callback?.Invoke(component);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
}