using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// Stress 运行器（组合能力，方案 §4.4）：顺序执行 phases（spawner / capacity / churn / flood）。
    /// <para>phase 间隔纪律：每个 phase 结束后若还有后续 phase，挂起采样（MetricSession.Suspend，
    /// 复用 P0 语义）→ 清场（销毁本 phase 实体）→ 真实时间等待 0.5s 让 GC/渲染安定 → 恢复采样，
    /// 使每个 phase 的指标段不被上一个 phase 的销毁尖峰污染。</para>
    /// <para>各 phase 指标段经 <see cref="StressCollector"/>（挂入会话）产出 phase.&lt;i&gt;.&lt;tool&gt;.durationMs
    /// 与各工具键；capacity/spawner 的承载曲线 (count, fps) 落盘 CSV 进 artifacts。</para>
    /// <para>flood 工具压的是自建托管事件（N 订阅者的简单发布列表，不依赖任何 AxuFrameKit.* 事件中心），
    /// 语义为"托管事件洪峰吞吐基准"。</para>
    /// </summary>
    public class StressRunner : IScenarioRunner
    {
        const float PhaseTransitionRecoverySec = 0.5f;

        readonly ScenarioContext _context;

        /// <param name="context">宿主服务上下文。</param>
        public StressRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;

            if (parameters.Phases == null || parameters.Phases.Count == 0)
            {
                if (handle != null)
                {
                    handle.Status = RunStatus.Error;
                    handle.AddFinding(FindingSeverity.Error, "STRESS_NO_PHASES",
                        "stress phases 列表为空（params.phases 需至少一项 {tool: spawner|capacity|churn|flood}）");
                }
                yield break;
            }

            // runner 自有结果采集器挂入度量会话
            var collector = new StressCollector();
            if (MetricSession.Current == null || !MetricSession.Current.TryAttachCollector(collector))
                log?.Warning("stress 结果采集器附加失败，本 scenario 将无 stress 指标产出");

            var scenarioKey = AgentPaths.SanitizeFileName(scenario.Id);
            var errorSeen = false;

            for (var i = 0; i < parameters.Phases.Count; i++)
            {
                if (handle != null && handle.CancelRequested) yield break;

                var phase = parameters.Phases[i];
                var startMs = Time.realtimeSinceStartupAsDouble * 1000.0;
                var phaseError = false;

                switch (phase.NormalizedTool)
                {
                    case "spawner":
                        yield return RunSpawnerPhase(phase, handle, collector, scenarioKey);
                        break;
                    case "capacity":
                        yield return RunCapacityPhase(phase, handle, collector, scenarioKey);
                        break;
                    case "churn":
                        yield return RunChurnPhase(phase, handle, collector);
                        break;
                    case "flood":
                        yield return RunFloodPhase(phase, handle, collector);
                        break;
                    default:
                        phaseError = true;
                        if (handle != null)
                        {
                            handle.Status = RunStatus.Error;
                            handle.AddFinding(FindingSeverity.Error, "STRESS_UNKNOWN_TOOL",
                                $"stress phase[{i}] 未知工具：\"{phase.Tool}\"（合法值 spawner/capacity/churn/flood）");
                        }
                        log?.Error($"stress phase[{i}] 未知工具：{phase.Tool}，终止后续 phase");
                        break;
                }

                if (phaseError)
                {
                    errorSeen = true;
                    break; // 参数写错属配置错误：不静默跳过，立即终止
                }

                var durationMs = Time.realtimeSinceStartupAsDouble * 1000.0 - startMs;
                collector.PhaseDuration(i, phase.NormalizedTool, durationMs);

                // ---- phase 间隔：挂起采样 → 清场 → 恢复（销毁尖峰不污染下一 phase 指标段）----
                if (i < parameters.Phases.Count - 1)
                {
                    var session = MetricSession.Current;
                    if (session != null) session.Suspend();
                    yield return AgentWait.RealtimeSeconds(PhaseTransitionRecoverySec);
                    if (session != null) session.Resume();
                }
            }

            if (handle != null && !errorSeen && handle.Status == RunStatus.Running)
                handle.Status = RunStatus.Passed;
        }

        // -------------------------------------------------------------- 各工具 phase

        /// <summary>spawner phase：阶梯加压 startCount → maxCount（每档 rampStep / rampIntervalSec），
        /// 每档记录 (count, fps) 采样；结束落盘承载曲线 CSV。</summary>
        IEnumerator RunSpawnerPhase(StressPhaseParams phase, ScenarioRunHandle handle,
            StressCollector collector, string scenarioKey)
        {
            var log = _context != null ? _context.Log : null;
            var spawner = ScopedSpawner(phase, log);
            var fps = new CapacityProbe.FpsWindow();
            var samples = new System.Collections.Generic.List<CapacityProbe.CurveSample>();

            try
            {
                spawner.Value.Add(phase.ClampedStartCount);
                yield return AgentWait.RealtimeSeconds(phase.ClampedSettleSec);

                var count = phase.ClampedStartCount;
                var maxCount = phase.ClampedMaxCount;
                while (true)
                {
                    if (handle != null && handle.CancelRequested) break;

                    // 观察一窗（与 capacity 判据同口径的窗口时长：belowThresholdSec 或 1s 取小者）
                    var windowSec = Mathf.Min(Mathf.Max(phase.ClampedBelowThresholdSec, 0.25f), 1f);
                    var windowEnd = Time.realtimeSinceStartupAsDouble + windowSec;
                    fps.Reset();
                    while (Time.realtimeSinceStartupAsDouble < windowEnd)
                    {
                        if (handle != null && handle.CancelRequested) break;
                        fps.Push();
                        yield return null;
                    }
                    if (handle != null && handle.CancelRequested) break;

                    if (!float.IsNaN(fps.CurrentFps))
                        samples.Add(new CapacityProbe.CurveSample(count, fps.CurrentFps));

                    if (count >= maxCount) break;
                    var next = Mathf.Min(count + phase.ClampedRampStep, maxCount);
                    spawner.Value.Add(next - count);
                    count = next;
                    yield return AgentWait.RealtimeSeconds(phase.ClampedRampIntervalSec);
                }

                collector.SpawnerStats(spawner.Value.MaxConcurrent, spawner.Value.InstantiatedTotal);
                WriteCurveCsv(scenarioKey, "spawner", samples);
                log?.Info($"spawner 完成：maxCount={spawner.Value.MaxConcurrent}，instantiated={spawner.Value.InstantiatedTotal}");
            }
            finally
            {
                spawner.Dispose();
            }
        }

        /// <summary>capacity phase：自反馈加压二分收敛（详见 <see cref="CapacityProbe"/>），落盘承载曲线 CSV。</summary>
        IEnumerator RunCapacityPhase(StressPhaseParams phase, ScenarioRunHandle handle,
            StressCollector collector, string scenarioKey)
        {
            var log = _context != null ? _context.Log : null;
            var spawner = ScopedSpawner(phase, log);
            try
            {
                var probe = new CapacityProbe(spawner.Value, phase, log);
                var result = new CapacityProbe.Result();
                yield return probe.Probe(handle, result);

                collector.CapacityResult(result.MaxEntities, result.Samples.Count);
                collector.SpawnerStats(spawner.Value.MaxConcurrent, spawner.Value.InstantiatedTotal);
                WriteCurveCsv(scenarioKey, "capacity", result.Samples);
            }
            finally
            {
                spawner.Dispose();
            }
        }

        /// <summary>churn phase：每秒 Create/Destroy createPerSec 个对象持续 durationSec 秒。</summary>
        IEnumerator RunChurnPhase(StressPhaseParams phase, ScenarioRunHandle handle, StressCollector collector)
        {
            var log = _context != null ? _context.Log : null;
            var spawner = ScopedSpawner(phase, log);
            try
            {
                var churn = new ChurnRunner(spawner.Value, log);
                yield return churn.Run(phase, handle);
                collector.ChurnStats(churn.CreatedTotal, churn.DestroyedTotal, churn.PeakPerTick);
            }
            finally
            {
                spawner.Dispose();
            }
        }

        /// <summary>
        /// flood phase：向自建托管事件中心每帧发布 eventsPerFrame 个事件 × durationSec 秒。
        /// 自建 hub = N 订阅者的数组发布列表（订阅者做一次字段累加，防被优化掉）；
        /// 统计事件总量、峰值 eventsPerSec、单帧发布最大耗时（ms）。
        /// </summary>
        IEnumerator RunFloodPhase(StressPhaseParams phase, ScenarioRunHandle handle, StressCollector collector)
        {
            var log = _context != null ? _context.Log : null;
            var hub = new ManagedEventHub(phase.ClampedSubscribers);
            var payload = new FloodEvent { Payload = 1 };
            var perFrame = phase.ClampedEventsPerFrame;

            long eventsTotal = 0;
            double maxPublishMs = 0;
            double lastSecMark = Time.realtimeSinceStartupAsDouble;
            long eventsAtLastSec = 0;
            double peakEventsPerSec = 0;
            var watch = Stopwatch.StartNew();

            var deadline = Time.realtimeSinceStartupAsDouble + phase.ClampedDurationSec;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (handle != null && handle.CancelRequested) break;

                watch.Restart();
                for (var i = 0; i < perFrame; i++) hub.Publish(payload);
                var publishMs = watch.Elapsed.TotalMilliseconds;
                if (publishMs > maxPublishMs) maxPublishMs = publishMs;
                eventsTotal += perFrame;

                var now = Time.realtimeSinceStartupAsDouble;
                if (now - lastSecMark >= 1.0)
                {
                    var rate = (eventsTotal - eventsAtLastSec) / (now - lastSecMark);
                    if (rate > peakEventsPerSec) peakEventsPerSec = rate;
                    eventsAtLastSec = eventsTotal;
                    lastSecMark = now;
                }

                yield return null;
            }

            collector.FloodStats(eventsTotal, peakEventsPerSec, maxPublishMs);
            log?.Info($"flood 完成：events={eventsTotal}，peakRate={peakEventsPerSec:0.}/s，" +
                      $"maxPublishMs={maxPublishMs:0.###}，subscribers={phase.ClampedSubscribers}");
        }

        // -------------------------------------------------------------- 内部

        /// <summary>scoped 包装：协程结束（含异常/取消路径由 using 保证）时 Dispose 清场。</summary>
        Scoped ScopedSpawner(StressPhaseParams phase, IAgentLog log)
        {
            return new Scoped(new StressSpawner(phase.PrefabPath, log));
        }

        /// <summary>承载曲线落盘：artifacts/{scenarioKey}_{tool}_curve.csv（count,fps）。</summary>
        void WriteCurveCsv(string scenarioKey, string tool,
            System.Collections.Generic.List<CapacityProbe.CurveSample> samples)
        {
            var artifactsDir = _context != null ? _context.ArtifactsDir : null;
            if (string.IsNullOrEmpty(artifactsDir) || samples == null || samples.Count == 0) return;

            try
            {
                var path = Path.Combine(artifactsDir, $"{scenarioKey}_{tool}_curve.csv");
                AgentPaths.EnsureDirectoryForFile(path);
                var builder = new StringBuilder("count,fps\n");
                foreach (var sample in samples)
                    builder.Append(sample.Count).Append(',').Append(sample.Fps.ToString("0.##")).Append('\n');
                File.WriteAllText(path, builder.ToString());
                _context.Handle?.Artifacts.Add(path);
            }
            catch (Exception ex)
            {
                _context?.Log?.Warning($"stress {tool} 承载曲线写盘失败：{ex.Message}");
            }
        }

        /// <summary>解析 params（解析失败回落默认实例——默认 phases 为空会走 STRESS_NO_PHASES）。</summary>
        static StressParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<StressParams>(scenario.Params) : null;
                return parsed ?? new StressParams();
            }
            catch (Exception)
            {
                return new StressParams();
            }
        }

        /// <summary>StressSpawner 的 using 包装（协程内保证 Dispose）。</summary>
        sealed class Scoped : IDisposable
        {
            public readonly StressSpawner Value;

            public Scoped(StressSpawner spawner)
            {
                Value = spawner;
            }

            public void Dispose()
            {
                Value.Dispose();
            }
        }

        /// <summary>自建事件载荷（class 复用同一实例，洪峰期间零分配）。</summary>
        sealed class FloodEvent
        {
            public int Payload;
        }

        /// <summary>
        /// 自建托管事件中心（stress 专用基准件，不依赖任何框架事件系统）：
        /// N 个订阅者的发布列表（数组顺序遍历、无 LINQ/闭包），Publish 一次回调 N 次。
        /// 订阅者把 payload 累加进字段（副作用不可被消除，IL2CPP 下也保真）。
        /// </summary>
        sealed class ManagedEventHub
        {
            readonly Action<FloodEvent>[] _subscribers;
            long _sink;

            public ManagedEventHub(int subscriberCount)
            {
                _subscribers = new Action<FloodEvent>[subscriberCount];
                for (var i = 0; i < subscriberCount; i++) _subscribers[i] = OnEvent;
            }

            void OnEvent(FloodEvent evt)
            {
                _sink += evt.Payload;
            }

            /// <summary>发布一个事件（触发全部订阅者回调）。</summary>
            public void Publish(FloodEvent evt)
            {
                var subscribers = _subscribers;
                for (var i = 0; i < subscribers.Length; i++) subscribers[i](evt);
            }

            /// <summary>订阅者累计值（防优化消除的观察口，诊断用）。</summary>
            public long Sink => _sink;
        }
    }
}
