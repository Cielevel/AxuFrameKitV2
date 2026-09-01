using System.Collections.Generic;
using AxuTestFramework.Agent.Collectors;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Capabilities.Stress
{
    /// <summary>
    /// Stress 结果采集器（runner 自有，经 <see cref="MetricSession.TryAttachCollector"/> 挂入会话，
    /// 不进 CollectorRegistry，模式同 SmokeCollector）：各 phase / 各工具产出动态 stat。
    /// 产出键（collector = "stress"，详见 DOC.md）：
    /// phase.&lt;i&gt;.&lt;tool&gt;.durationMs（Last）/ phase.count（Sum）；
    /// spawner.maxCount（Last）/ spawner.instantiated.total（Sum）；
    /// churn.created.total / churn.destroyed.total（Sum）/ churn.peakPerTick（Max）；
    /// flood.events.total（Sum）/ flood.eventsPerSec（Last）/ flood.publishMs.max（Max）；
    /// capacity.maxEntities（Last）/ capacity.samples（Sum）。
    /// </summary>
    public class StressCollector : MetricCollector
    {
        readonly List<CollectorStatOutput> _declared = new List<CollectorStatOutput>();
        MetricSession _session;

        /// <inheritdoc/>
        public override string Id => "stress";

        /// <summary>动态输出声明（收尾汇总时调用，每 scenario 一次，允许分配）。</summary>
        public override CollectorOutput Output => new CollectorOutput(_declared.ToArray());

        /// <inheritdoc/>
        public override void Start(ScenarioContext context, MetricSession session)
        {
            _session = session;
            _declared.Clear();

            // total 类聚合恒产出（含 0 值）：同 SmokeCollector 的播种策略，
            // 让"0"成为可命中预算的合法产出，"缺失=fail"只针对真正缺失的观测
            Record("churn.created.total", "churn.created", 0, MetricAggregator.Sum);
            Record("churn.destroyed.total", "churn.destroyed", 0, MetricAggregator.Sum);
            Record("flood.events.total", "flood.events", 0, MetricAggregator.Sum);
            Record("phase.count", "phase.count", 0, MetricAggregator.Sum);
        }

        /// <summary>记录一个 phase 的耗时（毫秒）。</summary>
        public void PhaseDuration(int phaseIndex, string tool, double durationMs)
        {
            Record($"phase.{phaseIndex}.{tool}.durationMs", $"phase.{phaseIndex}.{tool}.durationMs",
                durationMs, MetricAggregator.Last);
            Record("phase.count", "phase.count", 1, MetricAggregator.Sum);
        }

        /// <summary>记录 spawner phase 的峰值实体数与累计实例化数。</summary>
        public void SpawnerStats(int maxCount, long instantiatedTotal)
        {
            Record("spawner.maxCount", "spawner.maxCount", maxCount, MetricAggregator.Last);
            Record("spawner.instantiated.total", "spawner.instantiated", instantiatedTotal, MetricAggregator.Sum);
        }

        /// <summary>记录 churn phase 的产出/销毁总量与单秒峰值。</summary>
        public void ChurnStats(long createdTotal, long destroyedTotal, int peakPerTick)
        {
            Record("churn.created.total", "churn.created", createdTotal, MetricAggregator.Sum);
            Record("churn.destroyed.total", "churn.destroyed", destroyedTotal, MetricAggregator.Sum);
            Record("churn.peakPerTick", "churn.peakPerTick", peakPerTick, MetricAggregator.Max);
        }

        /// <summary>记录 flood phase 的事件总量 / 峰值速率 / 单帧发布最大耗时。</summary>
        public void FloodStats(long eventsTotal, double eventsPerSec, double maxPublishMs)
        {
            Record("flood.events.total", "flood.events", eventsTotal, MetricAggregator.Sum);
            Record("flood.eventsPerSec", "flood.eventsPerSec", eventsPerSec, MetricAggregator.Last);
            Record("flood.publishMs.max", "flood.publishMs", maxPublishMs, MetricAggregator.Max);
        }

        /// <summary>记录 capacity 探测结论与曲线采样数。</summary>
        public void CapacityResult(int maxEntities, int sampleCount)
        {
            Record("capacity.maxEntities", "capacity.maxEntities", maxEntities, MetricAggregator.Last);
            Record("capacity.samples", "capacity.samples", sampleCount, MetricAggregator.Sum);
        }

        // -------------------------------------------------------------- 内部

        /// <summary>写采样 + 按需声明输出 stat（首个采样即声明，聚合方式固定）。</summary>
        void Record(string outputName, string sourceSeries, double value, MetricAggregator aggregator)
        {
            foreach (var stat in _declared)
            {
                if (stat.Name == outputName)
                {
                    _session?.Record(Id, stat.Source, value);
                    return;
                }
            }

            _declared.Add(new CollectorStatOutput(outputName, aggregator, sourceSeries));
            _session?.Record(Id, sourceSeries, value);
        }
    }
}
