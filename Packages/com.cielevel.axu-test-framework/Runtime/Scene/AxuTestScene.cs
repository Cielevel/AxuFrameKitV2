using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AxuTestFramework
{
    /// <summary>
    /// PlayMode 场景助手：按路径异步加载 / 卸载 / 重载场景，供 [UnitySetUp] 与 [UnityTest] 协程 yield 使用。
    /// Editor 测试环境下可直接使用 Assets/ 完整路径（无需加入 Build Settings）；
    /// 玩家端运行时要求场景已加入 Build Settings。
    /// </summary>
    public static class AxuTestScene
    {
        /// <summary>
        /// 按路径异步加载场景，加载完成（AsyncOperation.isDone）后返回。
        /// </summary>
        /// <param name="scenePath">场景路径，如 "Assets/Scenes/Demo.unity"；Editor 测试环境支持加载 Assets/ 下任意场景。</param>
        /// <param name="mode">加载模式：Single（默认，替换当前场景）或 Additive。</param>
        /// <param name="unloadCurrentFirst">加载前先卸载所有已加载场景。Single 模式加载本就会替换当前场景，
        /// 该参数通常仅在 Additive 模式下需要；Unity 不允许卸载最后一个场景，此类卸载请求会被跳过。</param>
        public static IEnumerator LoadAsync(string scenePath, LoadSceneMode mode = LoadSceneMode.Single, bool unloadCurrentFirst = false)
        {
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("scenePath 不能为空。", nameof(scenePath));

            if (unloadCurrentFirst)
            {
                foreach (AsyncOperation unloadOp in UnloadAllLoadedScenes())
                {
                    while (!unloadOp.isDone)
                        yield return null;
                }
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(scenePath, mode);
            if (op == null)
            {
                Assert.Fail(
                    $"AxuTestScene.LoadAsync：无法加载场景 '{scenePath}'" +
                    "（场景不存在、未加入 Build Settings 或路径不正确）。" +
                    "Editor 测试环境请使用 Assets/ 完整路径。");
                yield break; // 不可达：Assert.Fail 总是抛出
            }

            while (!op.isDone)
                yield return null;
        }

        /// <summary>
        /// 按场景名卸载已加载场景，卸载完成后返回。
        /// </summary>
        /// <param name="sceneName">场景名（不带路径与扩展名），如 "Demo"。</param>
        public static IEnumerator UnloadAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("sceneName 不能为空。", nameof(sceneName));

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Assert.Fail(
                    $"AxuTestScene.UnloadAsync：找不到已加载的场景 '{sceneName}'" +
                    $"（当前已加载 {SceneManager.sceneCount} 个场景）。");
                yield break; // 不可达：Assert.Fail 总是抛出
            }

            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            if (op == null)
                yield break; // Unity 拒绝卸载（如最后一个场景）等情形

            while (!op.isDone)
                yield return null;
        }

        /// <summary>
        /// 重载场景：若同名场景已加载则先卸载，再以 Single 模式按路径重新加载，全部完成后返回。
        /// </summary>
        /// <param name="scenePath">场景路径，如 "Assets/Scenes/Demo.unity"。</param>
        public static IEnumerator ReloadAsync(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("scenePath 不能为空。", nameof(scenePath));

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
                yield return UnloadAsync(sceneName);

            yield return LoadAsync(scenePath, LoadSceneMode.Single);
        }

        /// <summary>
        /// 卸载当前所有已加载场景，返回有效的卸载请求（Unity 拒绝的请求——如最后一个场景——会被跳过）。
        /// 注意先收集再卸载：卸载过程中场景索引会变化，不能边遍历边卸载。
        /// </summary>
        private static List<AsyncOperation> UnloadAllLoadedScenes()
        {
            List<Scene> loadedScenes = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene current = SceneManager.GetSceneAt(i);
                if (current.isLoaded)
                    loadedScenes.Add(current);
            }

            List<AsyncOperation> ops = new List<AsyncOperation>();
            for (int i = 0; i < loadedScenes.Count; i++)
            {
                AsyncOperation op = SceneManager.UnloadSceneAsync(loadedScenes[i]);
                if (op != null)
                    ops.Add(op);
            }
            return ops;
        }
    }
}
