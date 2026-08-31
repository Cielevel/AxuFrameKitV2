using System;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using Unity.Profiling;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// rendering 采集器（id="rendering"）：ProfilerRecorder（Render：Draw Calls / SetPass Calls / Triangles）
    /// 每帧读最新值累计，OnInterval 记窗口均值序列。
    /// 自检：recorder 无效，或三项计数恒为 0 连续 N 个采样窗口 → COLLECTOR_UNAVAILABLE Finding 后停采。
    /// 输出 stats：drawCallsAvg / setPassAvg / trisAvg。
    /// </summary>
    public class RenderingCollector : MetricCollector
    {
        /// <summary>数据恒 0 的连续采样窗口数达到该值即判定不可用（默认 250ms 下约 5s，防误报正常空闲帧）。</summary>
        public const int MaxZeroIntervals = 20;

        MetricSession _session;
        ProfilerRecorder _drawCalls;
        ProfilerRecorder _setPass;
        ProfilerRecorder _tris;
        double _drawSum;
        double _setPassSum;
        double _trisSum;
        int _framesInWindow;
        int _zeroStreak;
        bool _unavailable;
        string _unavailableMessage;

        /// <inheritdoc/>
        public override string Id => "rendering";

        /// <inheritdoc/>
        public override CollectorOutput Output { get; } = new CollectorOutput(
            new CollectorStatOutput("drawCallsAvg", MetricAggregator.Avg, "drawCalls"),
            new CollectorStatOutput("setPassAvg", MetricAggregator.Avg, "setPass"),
            new CollectorStatOutput("trisAvg", MetricAggregator.Avg, "tris"));

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _zeroStreak = 0;
            _unavailable = false;
            _unavailableMessage = null;

            try
            {
                _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls");
                _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls");
                _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles");

                if (!_drawCalls.Valid || !_setPass.Valid || !_tris.Valid)
                    MarkUnavailable("ProfilerRecorder 计数器无效（Draw Calls/SetPass Calls/Triangles；需 Development Build 或启用 Profiler）");
            }
            catch (Exception ex)
            {
                MarkUnavailable("ProfilerRecorder 创建异常：" + ex.Message);
            }
        }

        /// <inheritdoc/>
        public override void OnFrame(double timeSeconds, float unscaledDeltaTime)
        {
            if (_unavailable || _session == null) return;

            try
            {
                if (!_drawCalls.Valid || !_setPass.Valid || !_tris.Valid) return;
                _drawSum += _drawCalls.LastValue;
                _setPassSum += _setPass.LastValue;
                _trisSum += _tris.LastValue;
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
            if (_framesInWindow <= 0) return;

            var drawMean = _drawSum / _framesInWindow;
            var setPassMean = _setPassSum / _framesInWindow;
            var trisMean = _trisSum / _framesInWindow;

            if (drawMean == 0d && setPassMean == 0d && trisMean == 0d)
            {
                _zeroStreak++;
                if (_zeroStreak >= MaxZeroIntervals)
                    MarkUnavailable($"Profiler 计数器数据连续 {_zeroStreak} 个采样窗口恒为 0（疑似 Release 构建无真实计数）");
            }
            else
            {
                _zeroStreak = 0;
                _session.Record(Id, "drawCalls", drawMean);
                _session.Record(Id, "setPass", setPassMean);
                _session.Record(Id, "tris", trisMean);
            }

            _drawSum = 0;
            _setPassSum = 0;
            _trisSum = 0;
            _framesInWindow = 0;
        }

        /// <inheritdoc/>
        public override void Complete(double timeSeconds)
        {
            StopRecorders();
        }

        /// <inheritdoc/>
        public override void CollectFindings(ScenarioRunHandle handle)
        {
            if (_unavailable)
                handle?.AddFinding(FindingSeverity.Warning, "COLLECTOR_UNAVAILABLE", _unavailableMessage);
        }

        void StopRecorders()
        {
            try
            {
                if (_drawCalls.Valid) _drawCalls.Dispose();
                if (_setPass.Valid) _setPass.Dispose();
                if (_tris.Valid) _tris.Dispose();
            }
            catch (Exception)
            {
                // 释放失败不影响测试本体
            }
            _drawCalls = default;
            _setPass = default;
            _tris = default;
        }

        void MarkUnavailable(string message)
        {
            _unavailable = true;
            _unavailableMessage = message;
            StopRecorders();
        }
    }
}
