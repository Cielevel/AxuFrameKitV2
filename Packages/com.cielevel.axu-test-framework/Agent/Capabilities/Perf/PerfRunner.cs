using System;
using System.Collections;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Perf
{
    /// <summary>
    /// Perf 运行器：（可选加载场景）→ 挂起采样（warmup 丢弃）→ 真实时间 warmup → 恢复采样 →
    /// 度量期等待（逐帧可取消）→（可选）截图 → Passed。指标由宿主驱动的采集器产出。
    /// </summary>
    public class PerfRunner : IScenarioRunner
    {
        readonly ScenarioContext _context;

        public PerfRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;

            // ---- 可选：加载目标场景（空 = 对当前场景采样）----
            if (!string.IsNullOrWhiteSpace(parameters.Scene))
            {
                var loadResult = new AgentSceneLoadResult();
                yield return AgentScenes.LoadAsync(parameters.Scene, loadResult);
                if (!loadResult.Loaded)
                {
                    if (handle != null)
                    {
                        handle.Status = RunStatus.Error;
                        handle.AddFinding(FindingSeverity.Error, "PERF_SCENE_LOAD_FAILED",
                            $"场景加载失败：{parameters.Scene}（{loadResult.Error}）");
                    }
                    yield break;
                }
                log?.Info($"场景已加载：{loadResult.ResolvedPath}");
            }

            // ---- warmup：挂起采样丢弃 warmup 期数据，恢复后进入度量期 ----
            var session = MetricSession.Current;
            if (session != null) session.Suspend();
            yield return AgentWait.RealtimeSeconds(parameters.ClampedWarmupSeconds);
            if (session != null) session.Resume();

            // ---- 度量期：真实时钟逐帧等待，逐帧响应取消 ----
            var deadline = Time.realtimeSinceStartupAsDouble + parameters.ClampedMeasureSeconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (handle != null && handle.CancelRequested) yield break;
                yield return null;
            }

            // ---- 可选：结束截图（artifacts 目录由宿主填入 context）----
            var artifactsDir = _context != null ? _context.ArtifactsDir : null;
            if (parameters.Screenshot && !string.IsNullOrEmpty(artifactsDir))
            {
                var fileName = AgentPaths.SanitizeFileName(scenario.Id) + "_end.png";
                var path = AgentCapture.ResolveArtifactsPath(artifactsDir, fileName);
                yield return AgentCapture.CaptureScreenshot(path);
                handle?.Artifacts.Add(path);
            }

            log?.Info($"perf 度量完成（measure={parameters.ClampedMeasureSeconds:0.##}s，" +
                $"warmup={parameters.ClampedWarmupSeconds:0.##}s）");
            if (handle != null) handle.Status = RunStatus.Passed;
        }

        /// <summary>解析 params（解析失败回落默认实例，runner 不因参数类型错误直接崩）。</summary>
        static PerfParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<PerfParams>(scenario.Params) : null;
                return parsed ?? new PerfParams();
            }
            catch (Exception)
            {
                return new PerfParams();
            }
        }
    }
}
