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

            if (req.asset is T asset)
                return asset;

            // 资源不存在或类型不匹配时不再静默返回 null
            Debug.LogWarning($"[ResourceLoader] LoadAsync 资源加载或类型转换失败：path='{path}'，目标类型={typeof(T).Name}，返回 null。");
            return null;
        }

        /// <summary>
        /// 使用Resources加载指定路径的资源 (Coroutine)
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        public static IEnumerator LoadCoroutine<T>(string path, Action<T> callback) where T : Object
            => LoadCoroutine(path, callback, null);

        /// <summary>
        /// 使用Resources加载指定路径的资源 (Coroutine)，加载或类型转换失败时回调 onFailed 并给出警告
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        /// <param name="onFailed">加载或类型转换失败时的回调</param>
        public static IEnumerator LoadCoroutine<T>(string path, Action<T> callback, Action onFailed) where T : Object
        {
            var req = Resources.LoadAsync<T>(path);
            yield return req;

            if (req.asset is T asset)
            {
                callback?.Invoke(asset);
            }
            else
            {
                Debug.LogWarning($"[ResourceLoader] LoadCoroutine 资源加载或类型转换失败：path='{path}'，目标类型={typeof(T).Name}。");
                onFailed?.Invoke();
            }
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
                var component = isChildComponent ? go.GetComponentInChildren<T>() : go.GetComponent<T>();

                if (component == null)
                    Debug.LogWarning($"[ResourceLoader] LoadGameObjectComponentAsync 实例化成功但未找到组件：path='{path}'，组件类型={typeof(T).Name}{(isChildComponent ? "（含子物体）" : "")}，返回 null。");

                return component;
            }

            Debug.LogWarning($"[ResourceLoader] LoadGameObjectComponentAsync GameObject 加载失败：path='{path}'，返回 null。");
            return null;
        }

        /// <summary>
        /// 使用Resources加载指定路径的GameObject，实例化一个物体，并返回它的一个指定组件 (Coroutine)
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        /// <param name="isChildComponent">是否在子物体中查找组件</param>
        public static IEnumerator LoadGameObjectComponentCoroutine<T>(string path, Action<T> callback, bool isChildComponent = false) where T : Component
            => LoadGameObjectComponentCoroutine(path, callback, null, isChildComponent);

        /// <summary>
        /// 使用Resources加载指定路径的GameObject，实例化一个物体，并返回它的一个指定组件 (Coroutine)，加载或找不到组件时回调 onFailed 并给出警告
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">加载完成后的回调</param>
        /// <param name="onFailed">加载失败或实例上找不到组件时的回调</param>
        /// <param name="isChildComponent">是否在子物体中查找组件</param>
        public static IEnumerator LoadGameObjectComponentCoroutine<T>(string path, Action<T> callback, Action onFailed, bool isChildComponent = false) where T : Component
        {
            var req = Resources.LoadAsync<GameObject>(path);
            yield return req;

            if (req.asset is GameObject prefab)
            {
                var go = Object.Instantiate(prefab);
                var component = isChildComponent ? go.GetComponentInChildren<T>() : go.GetComponent<T>();

                if (component != null)
                {
                    callback?.Invoke(component);
                }
                else
                {
                    Debug.LogWarning($"[ResourceLoader] LoadGameObjectComponentCoroutine 实例化成功但未找到组件：path='{path}'，组件类型={typeof(T).Name}{(isChildComponent ? "（含子物体）" : "")}。");
                    onFailed?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning($"[ResourceLoader] LoadGameObjectComponentCoroutine GameObject 加载失败：path='{path}'。");
                onFailed?.Invoke();
            }
        }
    }
}