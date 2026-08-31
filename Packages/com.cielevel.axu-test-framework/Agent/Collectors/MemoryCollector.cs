using System;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine.Profiling;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// memory 采集器（id="memory"）：OnInterval 采样 Unity Profiler 内存口径（MB）。
    /// API 全平台可用，无自检分支（异常仍兜底停采并产出 Finding）。
    /// 输出 stats：allocatedMax（Max）/ allocatedEnd（Last）/ monoUsedMax（Max）/ reservedMax（Max）。
    /// </summary>
    public class MemoryCollector : MetricCollector
    {
        const double BytesPerMegabyte = 1048576.0;

        MetricSession _session;
        bool _broken;
        string _brokenMessage;

        /// <inheritdoc/>
        public override string Id => "memory";

        /// <inheritdoc/>
        public override CollectorOutput Output { get; } = new CollectorOutput(
            new CollectorStatOutput("allocatedMax", MetricAggregator.Max, "allocatedMax"),
            new CollectorStatOutput("allocatedEnd", MetricAggregator.Last, "allocatedEnd"),
            new CollectorStatOutput("monoUsedMax", MetricAggregator.Max, "monoUsedMax"),
            new CollectorStatOutput("reservedMax", MetricAggregator.Max, "reservedMax"));

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _broken = false;
            _brokenMessage = null;
        }

        /// <inheritdoc/>
        public override void OnInterval(double timeSeconds)
        {
            if (_broken || _session == null) return;

            try
            {
                var allocatedMb = Profiler.GetTotalAllocatedMemoryLong() / BytesPerMegabyte;
                _session.Record(Id, "allocatedMax", allocatedMb); // 与 allocatedEnd 同源采样，聚合口径不同
                _session.Record(Id, "allocatedEnd", allocatedMb);
                _session.Record(Id, "monoUsedMax", Profiler.GetMonoUsedSizeLong() / BytesPerMegabyte);
                _session.Record(Id, "reservedMax", Profiler.GetTotalReservedMemoryLong() / BytesPerMegabyte);
            }
            catch (Exception ex)
            {
                _broken = true;
                _brokenMessage = "Profiler 内存 API 调用异常：" + ex.Message;
            }
        }

        /// <inheritdoc/>
        public override void CollectFindings(ScenarioRunHandle handle)
        {
            if (_broken)
                handle?.AddFinding(FindingSeverity.Error, "COLLECTOR_UNAVAILABLE", _brokenMessage);
        }
    }
}
