using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>单次场景加载的结果（由 <see cref="AgentScenes.LoadAsync"/> 填充）。</summary>
    public sealed class AgentSceneLoadResult
    {
        /// <summary>是否加载成功（激活场景已 isLoaded）。</summary>
        public bool Loaded;

        /// <summary>成功时实际使用的加载路径（三级回退之一）；失败为 null。</summary>
        public string ResolvedPath;

        /// <summary>失败原因（Editor 缺构建清单时含可读指引 + 全部尝试路径）；成功为 null。</summary>
        public string Error;
    }

    /// <summary>
    /// 场景加载原语：三级回退解析（① 原样路径 → ② 去 ".unity" 后缀的路径 → ③ 纯文件名），
    /// 每级 <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> 返回 null 即试下一级；
    /// 成功后等待 isDone 并经激活场景 isLoaded 确认（Single 模式下激活场景即最后加载的场景）。
    /// <para><b>前置条件（Unity 6000.0.23f1 实测）</b>：目标场景须已加入 Build Settings/Build Profile
    /// 场景清单——LoadSceneAsync 在 Editor 下与 Player <b>一致强制校验</b>
    /// （"has not been added to the active build profile or shared scene list"，当前已打开场景是唯一例外）。
    /// Editor 分支仅做<b>前置检查</b>：清单缺失时在 <see cref="AgentSceneLoadResult.Error"/> 给出可读指引，
    /// 随后照旧尝试引擎加载（引擎报原始错误，我们的指引可读性更好；已打开场景等例外由引擎放行）。
    /// 不做运行时临时注册 EditorBuildSettings 的方案——实测同会话内改动对加载校验不可见，
    /// 且在测试运行中变异用户 ProjectSettings 不合框架气质。</para>
    /// </summary>
    public static class AgentScenes
    {
        const string SceneSuffix = ".unity";

        /// <summary>
        /// 按三级回退解析加载场景（Single 模式），结果写入 <paramref name="result"/>；不抛异常。
        /// result 为 null 属调用方程序错误，抛 <see cref="ArgumentNullException"/>。
        /// </summary>
        public static IEnumerator LoadAsync(string scenePath, AgentSceneLoadResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                result.Loaded = false;
                result.ResolvedPath = null;
                result.Error = "scenePath 为空";
                yield break;
            }

            var candidates = BuildCandidates(scenePath);
            var tried = new List<string>(candidates.Count);

#if UNITY_EDITOR
            // 前置检查：目标是否在构建场景清单（归一化路径比对）；缺失时失败原因给出可读指引。
            // 纯场景名（无 Assets 段）无法归一化，跳过检查交由引擎判定。
            var buildPath = ToBuildSettingsScenePath(scenePath);
            var missingFromBuildList = buildPath != null && !BuildSceneListContains(buildPath);
#endif

            foreach (var candidate in candidates)
            {
                tried.Add(candidate);

                var operation = SceneManager.LoadSceneAsync(candidate, LoadSceneMode.Single);
                while (operation != null && !operation.isDone) yield return null;

                if (operation != null)
                {
                    // Single 模式下加载完成的场景成为激活场景：以其 isLoaded 作为加载成功确认
                    var active = SceneManager.GetActiveScene();
                    if (active.IsValid() && active.isLoaded)
                    {
                        result.Loaded = true;
                        result.ResolvedPath = candidate;
                        result.Error = null;
                        yield break;
                    }
                }
                // operation == null（引擎拒绝该路径）或确认失败：试下一级
            }

            result.Loaded = false;
            result.ResolvedPath = null;

#if UNITY_EDITOR
            if (missingFromBuildList)
            {
                result.Error =
                    $"场景未加入构建设置场景清单（Unity 6 Editor 与 Player 一致校验）：请将 {buildPath} 加入 Build Settings/Build Profile 后重试。" +
                    "已尝试路径：" + string.Join(" / ", tried.ToArray());
                yield break;
            }
#endif

            result.Error = "场景加载失败，已尝试路径：" + string.Join(" / ", tried.ToArray());
        }

        /// <summary>三级回退候选：原样路径 → 去 .unity 后缀 → 纯文件名（去重、去空）。</summary>
        static List<string> BuildCandidates(string scenePath)
        {
            var candidates = new List<string>(3);
            AddCandidate(candidates, scenePath);

            if (scenePath.EndsWith(SceneSuffix, StringComparison.OrdinalIgnoreCase))
                AddCandidate(candidates, scenePath.Substring(0, scenePath.Length - SceneSuffix.Length));

            AddCandidate(candidates, Path.GetFileNameWithoutExtension(scenePath));
            return candidates;
        }

        static void AddCandidate(List<string> candidates, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) return;
            if (candidates.Contains(candidate)) return;
            candidates.Add(candidate);
        }

#if UNITY_EDITOR
        /// <summary>把候选路径归一化为构建场景清单条目路径（"Assets/....unity"）；无法定位 Assets 根时返回 null（跳过前置检查）。</summary>
        static string ToBuildSettingsScenePath(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return null;

            var normalized = candidate.Replace('\\', '/');
            var assetsIndex = normalized.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex < 0) return null;

            var path = normalized.Substring(assetsIndex);
            if (!path.EndsWith(SceneSuffix, StringComparison.OrdinalIgnoreCase)) path += SceneSuffix;
            return path;
        }

        /// <summary>构建场景清单是否已含该路径（序数忽略大小写）。</summary>
        static bool BuildSceneListContains(string buildScenePath)
        {
            var current = EditorBuildSettings.scenes;
            if (current == null) return false;

            foreach (var entry in current)
            {
                if (string.Equals(entry.path, buildScenePath, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
#endif
    }
}
