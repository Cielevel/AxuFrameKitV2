using System;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// frameTime 采集器（id="frameTime"）：OnInterval 经 FrameTimingManager 取最近一帧的 cpu/gpu 帧时间（ms）
    /// 写入 "cpu"/"gpu" 序列。自检：连续 N 个采样窗口取数为 0 或调用异常 → COLLECTOR_UNAVAILABLE Finding 后停采。
    /// 输出 stats：cpuAvg / cpuP95 / gpuAvg / gpuP95。
    /// </summary>
    public class FrameTimeCollector : MetricCollector
    {
        /// <summary>连续空读次数达到该值即判定不可用（默认 250ms 间隔下约 1.25s）。</summary>
        public const int MaxEmptyIntervals = 5;

        MetricSession _session;
        int _emptyStreak;
        bool _unavailable;
        string _unavailableMessage;

        /// <inheritdoc/>
        public override string Id => "frameTime";

        /// <inheritdoc/>
        public override CollectorOutput Output { get; } = new CollectorOutput(
            new CollectorStatOutput("cpuAvg", MetricAggregator.Avg, "cpu"),
            new CollectorStatOutput("cpuP95", MetricAggregator.P95, "cpu"),
            new CollectorStatOutput("gpuAvg", MetricAggregator.Avg, "gpu"),
            new CollectorStatOutput("gpuP95", MetricAggregator.P95, "gpu"));

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _emptyStreak = 0;
            _unavailable = false;
            _unavailableMessage = null;
        }

        /// <inheritdoc/>
        public override void OnInterval(double timeSeconds)
        {
            if (_unavailable || _session == null) return;

            try
            {
                var timings = new FrameTiming[1]; // 每 interval 一次的小分配，可接受
                if (FrameTimingManager.GetLatestTimings(1, timings) == 0)
                {
                    _emptyStreak++;
                    if (_emptyStreak >= MaxEmptyIntervals)
                        MarkUnavailable("FrameTimingManager 无数据（Release 构建或平台不支持帧计时，frameTime 采集器不可用）");
                    return;
                }

                _emptyStreak = 0;
                _session.Record(Id, "cpu", timings[0].cpuFrameTime);
                _session.Record(Id, "gpu", timings[0].gpuFrameTime);
            }
            catch (Exception ex)
            {
                MarkUnavailable("FrameTimingManager 调用异常：" + ex.Message);
            }
        }

        /// <inheritdoc/>
        public override void CollectFindings(ScenarioRunHandle handle)
        {
            if (_unavailable)
                handle?.AddFinding(FindingSeverity.Warning, "COLLECTOR_UNAVAILABLE", _unavailableMessage);
        }

        void MarkUnavailable(string message)
        {
            _unavailable = true;
            _unavailableMessage = message;
        }
    }
}
