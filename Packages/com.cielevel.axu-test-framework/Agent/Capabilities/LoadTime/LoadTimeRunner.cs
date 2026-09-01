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

namespace AxuTestFramework.Agent.Capabilities.LoadTime
{
    /// <summary>
    /// LoadTime 运行器：场景清单逐个加载，记录 loadMs（加载完成）/ firstFrameMs（再等一渲染帧）/
    /// stableMs（连续 stableFrames 帧达 targetFps，超时记 SCENE_NOT_STABLE 警告）；
    /// 任一场景加载失败 → Error finding LOAD_FAILED 且最终 Failed，否则 Passed。
    /// </summary>
    public class LoadTimeRunner : IScenarioRunner
    {
        readonly ScenarioContext _context;

        public LoadTimeRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;

            var scenes = NormalizeScenes(parameters.Scenes);
            if (scenes.Count == 0)
            {
                if (handle != null)
                {
                    handle.Status = RunStatus.Error;
                    handle.AddFinding(FindingSeverity.Error, "LOADTIME_NO_SCENES",
                        "params.scenes 为空，无场景可测");
                }
                yield break;
            }

            // runner 自有结果采集器挂入度量会话（每场景一行 + 聚合）
            var collector = new LoadTimeCollector();
            if (MetricSession.Current == null || !MetricSession.Current.TryAttachCollector(collector))
                log?.Warning("loadtime 结果采集器附加失败，本 scenario 将无 loadtime 指标产出");

            var targetFrameSeconds = 1f / parameters.ClampedTargetFps;
            var loadFailedSeen = false;

            foreach (var scenePath in scenes)
            {
                if (handle != null && handle.CancelRequested) yield break;

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                collector.BeginScene(sceneName);
                var start = Time.realtimeSinceStartupAsDouble;

                var loadResult = new AgentSceneLoadResult();
                yield return AgentScenes.LoadAsync(scenePath, loadResult);

                if (handle != null && handle.CancelRequested) yield break;

                if (!loadResult.Loaded)
                {
                    loadFailedSeen = true;
                    handle?.AddFinding(FindingSeverity.Error, "LOAD_FAILED",
                        $"场景加载失败：{scenePath}（{loadResult.Error}）");
                    continue; // 失败场景不记 EndScene（无采样即不产出该场景 stat）
                }

                var loadMs = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
                yield return null; // 等一渲染帧再记首帧时长
                var firstFrameMs = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;

                // ---- 稳定帧判定：连续 N 帧 unscaledDeltaTime ≤ 1/targetFps；超时未稳定 → 警告 ----
                var stabilityStart = Time.realtimeSinceStartupAsDouble;
                var consecutiveStableFrames = 0;
                var stable = false;
                while (true)
                {
                    if (handle != null && handle.CancelRequested) yield break;

                    if (Time.unscaledDeltaTime <= targetFrameSeconds)
                    {
                        consecutiveStableFrames++;
                        if (consecutiveStableFrames >= parameters.ClampedStableFrames)
                        {
                            stable = true;
                            break;
                        }
                    }
                    else
                    {
                        consecutiveStableFrames = 0;
                    }

                    if (Time.realtimeSinceStartupAsDouble - stabilityStart >= parameters.ClampedStabilityTimeoutSeconds)
                    {
                        handle?.AddFinding(FindingSeverity.Warning, "SCENE_NOT_STABLE",
                            $"场景 \"{sceneName}\" 在 {parameters.ClampedStabilityTimeoutSeconds:0.#}s 内未达稳定" +
                            $"（连续 {parameters.ClampedStableFrames} 帧 ≥ {parameters.ClampedTargetFps:0.#}fps）");
                        break;
                    }

                    yield return null;
                }

                var stableMs = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
                collector.EndScene(loadMs, firstFrameMs, stableMs, stable);
                log?.Info($"场景 \"{sceneName}\" 记录完成：loadMs={loadMs:0.}，firstFrameMs={firstFrameMs:0.}，" +
                    $"stableMs={stableMs:0.}，stable={stable}");
            }

            if (handle != null)
                handle.Status = loadFailedSeen ? RunStatus.Failed : RunStatus.Passed;
        }

        /// <summary>场景清单去空白项；全部为空白视为空清单。</summary>
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
        static LoadTimeParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<LoadTimeParams>(scenario.Params) : null;
                return parsed ?? new LoadTimeParams();
            }
            catch (Exception)
            {
                return new LoadTimeParams();
            }
        }
    }
}
