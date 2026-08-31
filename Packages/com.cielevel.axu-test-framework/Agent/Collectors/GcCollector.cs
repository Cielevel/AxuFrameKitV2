using System;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using Unity.Profiling;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// gc 采集器（id="gc"）：ProfilerRecorder（Scripts："GC Allocated In Frame"）每帧累计，
    /// OnInterval 记窗口均值（B→KB）序列；GC.CollectionCount(0) 相对 Start 的差值写入 "collections"（Last=全程总次数）。
    /// 自检同 rendering（recorder 无效或恒 0 连续 N 个采样窗口 → COLLECTOR_UNAVAILABLE Finding）。
    /// 输出 stats：allocPerFrameAvg（KB）/ collections。
    /// </summary>
    public class GcCollector : MetricCollector
    {
        /// <summary>数据恒 0 的连续采样窗口数达到该值即判定不可用（默认 250ms 下约 5s，防误报无分配期）。</summary>
        public const int MaxZeroIntervals = 20;

        const double BytesPerKilobyte = 1024.0;

        MetricSession _session;
        ProfilerRecorder _gcAlloc;
        long _collectionsAtStart;
        double _allocSum;
        int _framesInWindow;
        int _zeroStreak;
        bool _unavailable;
        string _unavailableMessage;

        /// <inheritdoc/>
        public override string Id => "gc";

        /// <inheritdoc/>
        public override CollectorOutput Output { get; } = new CollectorOutput(
            new CollectorStatOutput("allocPerFrameAvg", MetricAggregator.Avg, "allocPerFrame"),
            new CollectorStatOutput("collections", MetricAggregator.Last, "collections"));

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _zeroStreak = 0;
            _unavailable = false;
            _unavailableMessage = null;

            try
            {
                _collectionsAtStart = GC.CollectionCount(0);
                _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "GC Allocated In Frame");
                if (!_gcAlloc.Valid)
                    MarkUnavailable("ProfilerRecorder \"GC Allocated In Frame\" 无效（需 Development Build 或启用 Profiler）");
            }
            catch (Exception ex)
            {
                MarkUnavailable("GC 采集器初始化异常：" + ex.Message);
            }
        }

        /// <inheritdoc/>
        public override void OnFrame(double timeSeconds, float unscaledDeltaTime)
        {
            if (_unavailable || _session == null) return;

            try
            {
                if (!_gcAlloc.Valid) return;
                _allocSum += _gcAlloc.LastValue;
                _framesInWindow++;
            }
            catch (Exception ex)
            {
                MarkUnavailable("ProfilerRecorder 读取异常：" + ex.Message);
            }
        }

        /// <inheritdoc/>
        public override void OnInterval(double timeSeconds)
        {
            if (_unavailable || _session == null) return;

            try
            {
                _session.Record(Id, "collections", GC.CollectionCount(0) - _collectionsAtStart);
            }
            catch (Exception)
            {
                // GC.CollectionCount 理论上不抛；兜底不拦其他序列
            }

            if (_framesInWindow <= 0) return;

            var meanBytes = _allocSum / _framesInWindow;
            if (meanBytes == 0d)
            {
                _zeroStreak++;
                if (_zeroStreak >= MaxZeroIntervals)
                    MarkUnavailable($"GC 计数器数据连续 {_zeroStreak} 个采样窗口恒为 0（疑似 Release 构建无真实计数）");
            }
            else
            {
                _zeroStreak = 0;
                _session.Record(Id, "allocPerFrame", meanBytes / BytesPerKilobyte);
            }

            _allocSum = 0;
            _framesInWindow = 0;
        }

        /// <inheritdoc/>
        public override void Complete(double timeSeconds)
        {
            StopRecorder();
        }

        /// <inheritdoc/>
        public override void CollectFindings(ScenarioRunHandle handle)
        {
            if (_unavailable)
                handle?.AddFinding(FindingSeverity.Warning, "COLLECTOR_UNAVAILABLE", _unavailableMessage);
        }

        void StopRecorder()
        {
            try
            {
                if (_gcAlloc.Valid) _gcAlloc.Dispose();
            }
            catch (Exception)
            {
                // 释放失败不影响测试本体
            }
            _gcAlloc = default;
        }

        void MarkUnavailable(string message)
        {
            _unavailable = true;
            _unavailableMessage = message;
            StopRecorder();
        }
    }
}
