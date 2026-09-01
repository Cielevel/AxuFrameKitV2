using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AxuTestFramework.Agent.Capabilities.Smoke
{
    /// <summary>
    /// Smoke 运行器：清单解析（list / buildsettings / directory[仅 Editor]）→ 逐场景
    /// 日志监视加载 → 稳定窗（先等 stabilityFrames 帧，ResetCounts 后等 stabilitySeconds 计增量错误）
    /// → 截图 → EndScene；加载失败照记行（loaded=false）继续。有失败最终 Failed，否则 Passed。
    /// </summary>
    public class SmokeRunner : IScenarioRunner
    {
        readonly ScenarioContext _context;

        public SmokeRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;

            var manifest = ResolveManifest(parameters, scenario, handle);
            if (manifest == null) yield break; // 清单错误已置 Error + finding

            // runner 自有结果采集器挂入度量会话（每场景一行 + 聚合）
            var collector = new SmokeCollector();
            if (MetricSession.Current == null || !MetricSession.Current.TryAttachCollector(collector))
                log?.Warning("smoke 结果采集器附加失败，本 scenario 将无 smoke 指标产出");

            var artifactsDir = _context != null ? _context.ArtifactsDir : null;
            var scenarioKey = AgentPaths.SanitizeFileName(scenario.Id);
            var loadFailedSeen = false;

            foreach (var scenePath in manifest)
            {
                if (handle != null && handle.CancelRequested) yield break;

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                collector.BeginScene(sceneName);
                var start = Time.realtimeSinceStartupAsDouble;

                using (var logWatch = new AgentLogWatch())
                {
                    var loadResult = new AgentSceneLoadResult();
                    yield return AgentScenes.LoadAsync(scenePath, loadResult);

                    if (handle != null && handle.CancelRequested) yield break;

                    if (!loadResult.Loaded)
                    {
                        // 加载失败照记行（loaded=false）继续下一个
                        loadFailedSeen = true;
                        var elapsedMs = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
                        collector.EndScene(elapsedMs, logWatch.ErrorCount, false);
                        log?.Warning($"场景加载失败（照记行继续）：{scenePath}（{loadResult.Error}）");
                        continue;
                    }

                    // ---- 稳定窗：先等前置帧数，ResetCounts 后等稳定秒数，增量错误即该场景错误数 ----
                    for (var frame = 0; frame < parameters.ClampedStabilityFrames; frame++)
                    {
                        if (handle != null && handle.CancelRequested) yield break;
                        yield return null;
                    }
                    logWatch.ResetCounts();
                    yield return AgentWait.RealtimeSeconds(parameters.ClampedStabilitySeconds);
                    if (handle != null && handle.CancelRequested) yield break;

                    var errorCount = logWatch.ErrorCount;

                    // ---- 截图留证 ----
                    var artifactsDirForScene = artifactsDir;
                    if (parameters.Screenshot && !string.IsNullOrEmpty(artifactsDirForScene))
                    {
                        var fileName = scenarioKey + "_" + AgentPaths.SanitizeFileName(sceneName) + ".png";
                        var path = AgentCapture.ResolveArtifactsPath(artifactsDirForScene, fileName);
                        yield return AgentCapture.CaptureScreenshot(path);
                        handle?.Artifacts.Add(path);
                    }

                    var loadMs = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
                    collector.EndScene(loadMs, errorCount, true);
                    log?.Info($"场景 \"{sceneName}\" 冒烟通过窗口：loadMs={loadMs:0.}，errors={errorCount}");
                }
            }

            if (handle != null)
                handle.Status = loadFailedSeen ? RunStatus.Failed : RunStatus.Passed;
        }

        /// <summary>
        /// 场景清单解析：list → params.scenes；buildsettings → 构建清单；directory → 目录扫描（仅 Editor）。
        /// 清单错误置 Error + finding 并返回 null（已含 SMOKE_DIRECTORY_UNAVAILABLE / SMOKE_NO_SCENES 两类）。
        /// </summary>
        List<string> ResolveManifest(SmokeParams parameters, ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            List<string> manifest;
            switch (parameters.NormalizedSource)
            {
                case "buildsettings":
                    manifest = ExpandBuildSettings();
                    break;
                case "directory":
                    if (Application.isEditor)
                    {
                        manifest = ExpandDirectory(parameters.DirectoryPath);
                    }
                    else
                    {
                        handle.Status = RunStatus.Error;
                        handle.AddFinding(FindingSeverity.Error, "SMOKE_DIRECTORY_UNAVAILABLE",
                            "smoke source=directory 仅在 Editor 可用（Player 无文件系统工程布局），请改用 list/buildsettings");
                        return null;
                    }
                    break;
                default:
                    manifest = NormalizeScenes(parameters.Scenes);
                    break;
            }

            if (manifest == null || manifest.Count == 0)
            {
                handle.Status = RunStatus.Error;
                handle.AddFinding(FindingSeverity.Error, "SMOKE_NO_SCENES",
                    "smoke 场景清单为空（source=" + parameters.NormalizedSource + "）");
                return null;
            }
            return manifest;
        }

        /// <summary>构建清单场景（仅已启用场景；路径形如 Assets/...）。</summary>
        static List<string> ExpandBuildSettings()
        {
            var scenes = new List<string>();
            var count = SceneManager.sceneCountInBuildSettings;
            for (var i = 0; i < count; i++)
            {
                var path = SceneManager.GetSceneByBuildIndex(i).path;
                if (!string.IsNullOrWhiteSpace(path)) scenes.Add(path);
            }
            return scenes;
        }

        /// <summary>目录扫描（*.unity 递归）→ Assets/... 相对路径；目录为空/不存在返回空清单。</summary>
        static List<string> ExpandDirectory(string directoryPath)
        {
            var scenes = new List<string>();
            if (string.IsNullOrWhiteSpace(directoryPath)) return scenes;

            string full;
            try { full = Path.GetFullPath(directoryPath); }
            catch (Exception) { return scenes; }
            if (!Directory.Exists(full)) return scenes;

            string[] files;
            try { files = Directory.GetFiles(full, "*.unity", SearchOption.AllDirectories); }
            catch (Exception) { return scenes; }

            foreach (var file in files)
            {
                var normalized = file.Replace('\\', '/');
                var assetsIndex = normalized.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (assetsIndex < 0) continue; // Assets 目录树之外：跳过
                scenes.Add(normalized.Substring(assetsIndex));
            }
            return scenes;
        }

        /// <summary>场景清单去空白项。</summary>
        static List<string> NormalizeScenes(List<string> scenes)
        {
            var normalized = new List<string>();
            if (scenes == null) return normalized;
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrWhiteSpace(scene)) normalized.Add(scene.Trim());
            }
            return normalized;
        }

        /// <summary>解析 params（解析失败回落默认实例）。</summary>
        static SmokeParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<SmokeParams>(scenario.Params) : null;
                return parsed ?? new SmokeParams();
            }
            catch (Exception)
            {
                return new SmokeParams();
            }
        }
    }
}
