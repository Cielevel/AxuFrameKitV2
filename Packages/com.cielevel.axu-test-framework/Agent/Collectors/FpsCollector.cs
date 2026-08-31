using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// fps 采集器（id="fps"）：OnFrame 累计帧数与无缩放帧时长；OnInterval 产出窗口 fps（帧数/窗口时长）
    /// 写入 "interval" 序列；Complete 补录全程口径 "avg"（总帧数/总时长，单点）。
    /// 输出 stats：avg（全程总口径）/ p50 / p95 / min（interval 序列聚合）。
    /// </summary>
    public class FpsCollector : MetricCollector
    {
        MetricSession _session;
        long _totalFrames;
        float _totalDuration;
        int _intervalFrames;
        float _intervalDuration;

        /// <inheritdoc/>
        public override string Id => "fps";

        /// <inheritdoc/>
        public override CollectorOutput Output { get; } = new CollectorOutput(
            new CollectorStatOutput("avg", MetricAggregator.Last),
            new CollectorStatOutput("p50", MetricAggregator.P50, "interval"),
            new CollectorStatOutput("p95", MetricAggregator.P95, "interval"),
            new CollectorStatOutput("min", MetricAggregator.Min, "interval"));

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _totalFrames = 0;
            _totalDuration = 0f;
            _intervalFrames = 0;
            _intervalDuration = 0f;
        }

        /// <inheritdoc/>
        public override void OnFrame(double timeSeconds, float unscaledDeltaTime)
        {
            _totalFrames++;
            _intervalFrames++;
            _totalDuration += unscaledDeltaTime;
            _intervalDuration += unscaledDeltaTime;
        }

        /// <inheritdoc/>
        public override void OnInterval(double timeSeconds)
        {
            FlushInterval();
        }

        /// <inheritdoc/>
        public override void Complete(double timeSeconds)
        {
            FlushInterval(); // 残段不足一个采样间隔：帧数计入全程口径
            if (_totalDuration > 0f && _totalFrames > 0)
                _session?.Record(Id, "avg", _totalFrames / _totalDuration);
        }

        void FlushInterval()
        {
            if (_intervalFrames > 0 && _intervalDuration > 0f)
                _session?.Record(Id, "interval", _intervalFrames / _intervalDuration);
            _intervalFrames = 0;
            _intervalDuration = 0f;
        }
    }
}
