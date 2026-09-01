using System.Collections;
using System.Collections.Generic;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// 容量探测（stress 亮点，方案 §4.4）：自反馈加压找"目标帧率下最大实体数"。
    /// <para>算法：从 startCount 起，每档加 rampStep 个实体、等 rampIntervalSec 稳定，观察滑动窗口
    /// fps（<see cref="FpsWindow"/>）——连续低于 targetFps 达 belowThresholdSec 判超载；未超载则记录
    /// (count, fps) 承载曲线采样并继续加压，直至超载或到达 maxCount。超载后在 [lastGood, firstBad]
    /// 区间二分（每档重设实体数、等 settleSec、按同一超载判据定向），区间相对宽度 ≤
    /// convergeTolerancePercent 时收敛，lastGood 即 capacity.maxEntities。</para>
    /// <para>产出：Result.MaxEntities（指标 capacity.maxEntities）+ Samples 承载曲线
    /// （runner 落盘 CSV 进 artifacts）。</para>
    /// </summary>
    public class CapacityProbe
    {
        const float OverloadRecoverySec = 0.5f; // fps 回到阈值之上持续该时长 → 判未超载
        const int MaxBinarySearchSteps = 32;     // 二分步数硬上限（防御 float 边界抖动）

        readonly StressSpawner _spawner;
        readonly StressPhaseParams _parameters;
        readonly IAgentLog _log;
        readonly FpsWindow _fps = new FpsWindow();

        bool _watchOverloaded;
        bool _sawValidFps;

        /// <summary>单次探测结果（承载曲线采样 + 结论）。</summary>
        public class Result
        {
            /// <summary>目标帧率下最大实体数（未超载到达 maxCount 时 = maxCount，见 MaxCountReached）。</summary>
            public int MaxEntities;

            /// <summary>true = 一直加压到 maxCount 仍未超载（maxEntities 为下界而非精确容量）。</summary>
            public bool MaxCountReached;

            /// <summary>收敛点（maxEntities 实体数下）的滑动窗口 fps（未验证时为 NaN）。</summary>
            public float VerifiedFps = float.NaN;

            /// <summary>承载曲线采样（按探测顺序；count, fps）。</summary>
            public List<CurveSample> Samples = new List<CurveSample>();
        }

        /// <summary>承载曲线单点：实体数 + 该档滑动窗口 fps。</summary>
        public struct CurveSample
        {
            /// <summary>该档实体数。</summary>
            public int Count;

            /// <summary>该档滑动窗口 fps。</summary>
            public float Fps;

            public CurveSample(int count, float fps)
            {
                Count = count;
                Fps = fps;
            }
        }

        /// <summary>
        /// 滑动窗口 fps：环形记录最近 WindowSize 帧的真实帧间隔（unscaled 真实时钟口径），
        /// avg = 窗口帧数 / 窗口总时长；窗口未填满时返回 NaN（调用方忽略）。
        /// </summary>
        public class FpsWindow
        {
            const int WindowSize = 60;

            readonly double[] _deltas = new double[WindowSize];
            int _next;
            int _filled;

            /// <summary>逐帧喂数（帧开头调用一次）。</summary>
            public void Push()
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (_started)
                {
                    _deltas[_next] = now - _last;
                    _next = (_next + 1) % WindowSize;
                    if (_filled < WindowSize) _filled++;
                }
                _started = true; // 首帧只记基准时间：旧实现以 _filled>0 判"已启动"，永远无法离开 0
                _last = now;
            }

            bool _started;
            double _last;

            /// <summary>当前窗口 fps（窗口未满或异常间隔时 NaN）。</summary>
            public float CurrentFps
            {
                get
                {
                    if (_filled < WindowSize) return float.NaN;
                    double sum = 0;
                    for (var i = 0; i < WindowSize; i++) sum += _deltas[i];
                    return sum > 1e-6 ? (float)(WindowSize / sum) : float.NaN;
                }
            }

            /// <summary>复位窗口（重新积累）。</summary>
            public void Reset()
            {
                _filled = 0;
                _next = 0;
                _started = false;
            }
        }

        /// <param name="spawner">对象生成器（probe 不接管其生命周期，由 runner Dispose）。</param>
        /// <param name="parameters">capacity 工具参数。</param>
        /// <param name="log">宿主日志（可空）。</param>
        public CapacityProbe(StressSpawner spawner, StressPhaseParams parameters, IAgentLog log)
        {
            _spawner = spawner;
            _parameters = parameters;
            _log = log;
        }

        /// <summary>执行探测（协程；结果写入 result，取消时中途退出保留已得结论）。</summary>
        public IEnumerator Probe(ScenarioRunHandle handle, Result result)
        {
            var lo = 0;
            var hi = -1; // -1 = 尚未出现超载档
            var count = _parameters.ClampedStartCount;
            var maxCount = _parameters.ClampedMaxCount;

            // ---- 初始档：加到 startCount，等稳定 ----
            _spawner.Add(count);
            yield return AgentWait.RealtimeSeconds(_parameters.ClampedSettleSec);

            // ---- ramp：逐档加压直到超载或到达 maxCount ----
            while (true)
            {
                if (handle != null && handle.CancelRequested) yield break;
                yield return WatchOverload(handle);
                if (handle != null && handle.CancelRequested) yield break;

                if (_watchOverloaded)
                {
                    hi = count;
                    break;
                }

                lo = count;
                result.Samples.Add(new CurveSample(count, _fps.CurrentFps));

                if (count >= maxCount)
                {
                    result.MaxCountReached = true;
                    break;
                }

                var next = Mathf.Min(count + _parameters.ClampedRampStep, maxCount);
                _spawner.Add(next - count);
                count = next;
                yield return AgentWait.RealtimeSeconds(_parameters.ClampedRampIntervalSec);
            }

            // ---- 二分收敛（未超载到达上限则跳过）----
            if (!result.MaxCountReached && hi > lo)
            {
                var steps = 0;
                while ((hi - lo) * 100.0 / Mathf.Max(1, lo) > _parameters.ClampedConvergeTolerancePercent
                       && steps < MaxBinarySearchSteps)
                {
                    if (handle != null && handle.CancelRequested) yield break;

                    var mid = lo + (hi - lo) / 2;
                    if (mid == lo || mid == hi) break; // 整数区间已到头

                    AdjustTo(mid);
                    yield return AgentWait.RealtimeSeconds(_parameters.ClampedSettleSec);
                    yield return WatchOverload(handle);
                    if (handle != null && handle.CancelRequested) yield break;

                    if (_watchOverloaded)
                    {
                        hi = mid;
                        result.Samples.Add(new CurveSample(mid, _fps.CurrentFps));
                    }
                    else
                    {
                        lo = mid;
                        result.Samples.Add(new CurveSample(mid, _fps.CurrentFps));
                    }
                    steps++;
                }
            }

            result.MaxEntities = result.MaxCountReached ? maxCount : lo;

            // ---- 收敛点复核：在 maxEntities 档再观察一窗，记录达成 fps ----
            if (result.MaxEntities > 0)
            {
                AdjustTo(result.MaxEntities);
                _fps.Reset();
                var watchEnd = Time.realtimeSinceStartupAsDouble + _parameters.ClampedBelowThresholdSec;
                while (Time.realtimeSinceStartupAsDouble < watchEnd)
                {
                    if (handle != null && handle.CancelRequested) yield break;
                    _fps.Push();
                    yield return null;
                }
                if (!float.IsNaN(_fps.CurrentFps)) result.VerifiedFps = _fps.CurrentFps;
            }

            _log?.Info($"capacity 探测完成：maxEntities={result.MaxEntities}" +
                       (result.MaxCountReached ? $"（到达 maxCount 上限，为下界）" : "") +
                       (float.IsNaN(result.VerifiedFps) ? "" : $"，收敛点 fps={result.VerifiedFps:0.##}"));
        }

        // -------------------------------------------------------------- 内部

        /// <summary>观察超载判据：滑动窗口 fps 连续低于 targetFps 达 belowThresholdSec → 超载；
        /// 回到阈值之上持续 OverloadRecoverySec → 未超载。逐帧响应取消。</summary>
        IEnumerator WatchOverload(ScenarioRunHandle handle)
        {
            var target = _parameters.ClampedTargetFps;
            var thresholdSec = _parameters.ClampedBelowThresholdSec;
            _fps.Reset();

            var watchStart = Time.realtimeSinceStartupAsDouble;
            var belowSince = double.NaN; // NaN = 当前不在低于状态
            _sawValidFps = false;

            while (true)
            {
                if (handle != null && handle.CancelRequested)
                {
                    _watchOverloaded = false;
                    yield break;
                }

                _fps.Push();
                var now = Time.realtimeSinceStartupAsDouble;
                var fps = _fps.CurrentFps;

                if (!float.IsNaN(fps))
                {
                    _sawValidFps = true;
                    if (fps < target)
                    {
                        if (double.IsNaN(belowSince)) belowSince = now;
                        if (now - belowSince >= thresholdSec)
                        {
                            _watchOverloaded = true; // 持续低于阈值：判超载
                            yield break;
                        }
                    }
                    else
                    {
                        belowSince = double.NaN;
                        if (now - watchStart >= thresholdSec + OverloadRecoverySec)
                        {
                            _watchOverloaded = false; // 观察期满仍在阈值之上：未超载
                            yield break;
                        }
                    }
                }

                // 超时兜底：即使 fps 抖动也必须在 threshold + recovery 内出结论。
                // 整个观察期窗口都没填满（fps 病态低，连 60 帧都攒不齐）视同低于阈值 → 超载
                if (now - watchStart > thresholdSec * 2f + OverloadRecoverySec)
                {
                    if (!_sawValidFps) _watchOverloaded = true;
                    else _watchOverloaded = !double.IsNaN(belowSince) && now - belowSince >= thresholdSec * 0.5;
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>把实体数调整到 target（增或减）。</summary>
        void AdjustTo(int target)
        {
            if (_spawner.CurrentCount < target) _spawner.Add(target - _spawner.CurrentCount);
            else if (_spawner.CurrentCount > target) _spawner.RemoveDownTo(target);
        }
    }
}
