using System;
using System.Collections.Generic;
using AxuTestFramework.Agent.Collectors;
using AxuTestFramework.Core.Metrics;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Metrics
{
    /// <summary>
    /// 单个 scenario 的度量会话：持有采集器实例与采样序列（collector 键 → stat → 环形序列），
    /// 由宿主逐帧（OnFrame）/按采样间隔（OnInterval）/收尾（Complete）驱动；
    /// BuildSummary 按各采集器 <see cref="CollectorOutput"/> 声明聚合出报告 metrics。
    /// "custom" 通道为会话内建（AxuMetrics 打点），不经采集器注册表。
    /// 采集器回调异常会停用对应采集器（记一次警告），不影响测试本体。
    /// </summary>
    public class MetricSession
    {
        /// <summary>custom 打点通道名（BuildSummary 的顶层键；恒可用，不经注册表）。</summary>
        public const string CustomCollectorId = "custom";

        /// <summary>当前活动会话（AxuMetrics 打点入口；宿主 Begin/EndCurrent 维护）。</summary>
        public static MetricSession Current { get; private set; }

        readonly List<MetricCollector> _collectors = new List<MetricCollector>();
        readonly List<bool> _disabled = new List<bool>();
        readonly Dictionary<string, Dictionary<string, MetricSeries>> _series =
            new Dictionary<string, Dictionary<string, MetricSeries>>(StringComparer.Ordinal);
        readonly Dictionary<string, double> _counters = new Dictionary<string, double>(StringComparer.Ordinal);
        readonly Dictionary<string, MetricSeries> _gauges = new Dictionary<string, MetricSeries>(StringComparer.Ordinal);
        readonly Dictionary<string, MetricSeries> _scopes = new Dictionary<string, MetricSeries>(StringComparer.Ordinal);
        readonly double _startTimeSeconds;
        ScenarioContext _context;

        MetricSession(IReadOnlyList<string> collectorIds, double startTimeSeconds)
        {
            _startTimeSeconds = startTimeSeconds;
            if (collectorIds == null) return;

            foreach (var id in collectorIds)
            {
                if (string.IsNullOrEmpty(id) || id == CustomCollectorId) continue; // custom 为内建通道
                if (CollectorRegistry.TryCreate(id, out var collector))
                {
                    _collectors.Add(collector);
                    _disabled.Add(false);
                }
                else
                {
                    MissingCollectorIds.Add(id);
                }
            }
        }

        /// <summary>注册表解析失败的采集器 id（宿主据此产出 COLLECTOR_UNKNOWN Finding）。</summary>
        public List<string> MissingCollectorIds { get; } = new List<string>();

        /// <summary>会话起始时刻（宿主真实时钟口径）。</summary>
        public double StartTimeSeconds => _startTimeSeconds;

        /// <summary>开始会话（隐式结束上一会话，防 Player 不重载域时的静态残留）。</summary>
        public static MetricSession Begin(IReadOnlyList<string> collectorIds, double startTimeSeconds)
        {
            EndCurrent();
            var session = new MetricSession(collectorIds, startTimeSeconds);
            Current = session;
            return session;
        }

        /// <summary>结束当前会话（解除静态引用；宿主在 Complete/汇总之后调用）。</summary>
        public static void EndCurrent()
        {
            Current = null;
        }

        // -------------------------------------------------------------- 宿主驱动节拍

        /// <summary>启动全部采集器（每 scenario 一次）。</summary>
        public void StartCollectors(ScenarioContext context)
        {
            _context = context;
            for (var i = 0; i < _collectors.Count; i++)
            {
                var collector = _collectors[i];
                try { collector.Start(context, this); }
                catch (Exception ex) { Disable(collector, i, "Start", ex); }
            }
        }

        /// <summary>帧节拍（宿主每帧驱动；采集器在此累计帧级计数，不直接写入序列）。</summary>
        public void OnFrame(double timeSeconds, float unscaledDeltaTime)
        {
            for (var i = 0; i < _collectors.Count; i++)
            {
                if (_disabled[i]) continue;
                var collector = _collectors[i];
                try { collector.OnFrame(timeSeconds, unscaledDeltaTime); }
                catch (Exception ex) { Disable(collector, i, "OnFrame", ex); }
            }
        }

        /// <summary>采样节拍（宿主按 sampleIntervalMs 累计驱动；采集器在此把窗口聚合值写入序列）。</summary>
        public void OnInterval(double timeSeconds)
        {
            for (var i = 0; i < _collectors.Count; i++)
            {
                if (_disabled[i]) continue;
                var collector = _collectors[i];
                try { collector.OnInterval(timeSeconds); }
                catch (Exception ex) { Disable(collector, i, "OnInterval", ex); }
            }
        }

        /// <summary>收尾节拍（runner 结束后调用；采集器在此补录全程口径并释放资源）。</summary>
        public void CompleteCollectors(double timeSeconds)
        {
            for (var i = 0; i < _collectors.Count; i++)
            {
                if (_disabled[i]) continue;
                var collector = _collectors[i];
                try { collector.Complete(timeSeconds); }
                catch (Exception ex) { Disable(collector, i, "Complete", ex); }
            }
        }

        /// <summary>把采集器自检产出的不可用 Finding 汇入句柄。</summary>
        public void CollectFindings(ScenarioRunHandle handle)
        {
            for (var i = 0; i < _collectors.Count; i++)
            {
                try { _collectors[i].CollectFindings(handle); }
                catch (Exception) { /* Finding 汇集失败不影响测试本体 */ }
            }
        }

        // -------------------------------------------------------------- 采集器写入

        /// <summary>
        /// 采集器写入一个采样。采样节拍由宿主驱动（OnInterval/Complete），序列容量由环形缓冲兜底，无需二次节流。
        /// </summary>
        public void Record(string collector, string stat, double value)
        {
            if (string.IsNullOrEmpty(collector) || string.IsNullOrEmpty(stat)) return;
            GetOrCreateSeries(collector, stat).Record(value, RealtimeSeconds());
        }

        // -------------------------------------------------------------- custom 打点（AxuMetrics 调用）

        /// <summary>累计计数（汇总取 Sum，输出键 counter.&lt;key&gt;）。</summary>
        public void RecordCounter(string key, double delta)
        {
            if (string.IsNullOrEmpty(key)) return;
            _counters.TryGetValue(key, out var current);
            _counters[key] = current + delta;
        }

        /// <summary>瞬时值（汇总取 Avg/Max，输出键 gauge.&lt;key&gt;.avg/.max）。</summary>
        public void RecordGauge(string key, double value)
        {
            if (string.IsNullOrEmpty(key)) return;
            GetOrCreateCustom(_gauges, key).Record(value, RealtimeSeconds());
        }

        /// <summary>开始计时作用域（Dispose 记毫秒；输出键 scope.&lt;name&gt;.avg/.p95/.max）。</summary>
        public IDisposable BeginScope(string name)
        {
            return new TimedScope(this, name, RealtimeMilliseconds());
        }

        // -------------------------------------------------------------- 汇总

        /// <summary>
        /// 构建报告 metrics：{ collector: { stat: 值 }, "custom": { counter.X / gauge.Y.avg / scope.Z.p95 … } }。
        /// 聚合方式由各采集器 Output 声明驱动；无采样的 stat 不产出（绝不静默 0 值）。
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> BuildSummary()
        {
            var summary = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);

            for (var i = 0; i < _collectors.Count; i++)
            {
                if (_disabled[i]) continue;

                CollectorOutput output = null;
                try { output = _collectors[i].Output; }
                catch (Exception) { /* 输出声明异常：该采集器不产出 */ }
                if (output == null || output.Stats == null || output.Stats.Count == 0) continue;

                var stats = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (var stat in output.Stats)
                {
                    var series = FindSeries(_collectors[i].Id, stat.Source ?? stat.Name);
                    if (series == null || series.Count == 0) continue;
                    stats[stat.Name] = Aggregate(series, stat.Aggregator);
                }
                if (stats.Count > 0) summary[_collectors[i].Id] = stats;
            }

            var custom = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var pair in _counters) custom[$"counter.{pair.Key}"] = pair.Value;
            foreach (var pair in _gauges)
            {
                var stat = MetricStats.Summarize(pair.Value.SnapshotValues());
                custom[$"gauge.{pair.Key}.avg"] = stat.Avg;
                custom[$"gauge.{pair.Key}.max"] = stat.Max;
            }
            foreach (var pair in _scopes)
            {
                var stat = MetricStats.Summarize(pair.Value.SnapshotValues());
                custom[$"scope.{pair.Key}.avg"] = stat.Avg;
                custom[$"scope.{pair.Key}.p95"] = stat.P95;
                custom[$"scope.{pair.Key}.max"] = stat.Max;
            }
            if (custom.Count > 0) summary[CustomCollectorId] = custom;

            return summary;
        }

        // -------------------------------------------------------------- 内部

        MetricSeries GetOrCreateSeries(string collector, string stat)
        {
            if (!_series.TryGetValue(collector, out var stats))
            {
                stats = new Dictionary<string, MetricSeries>(StringComparer.Ordinal);
                _series.Add(collector, stats);
            }

            if (!stats.TryGetValue(stat, out var series))
            {
                series = new MetricSeries($"{collector}.{stat}");
                stats.Add(stat, series);
            }
            return series;
        }

        MetricSeries FindSeries(string collector, string stat)
        {
            return _series.TryGetValue(collector, out var stats)
                && stats.TryGetValue(stat, out var series)
                ? series
                : null;
        }

        static MetricSeries GetOrCreateCustom(Dictionary<string, MetricSeries> map, string key)
        {
            if (!map.TryGetValue(key, out var series))
            {
                series = new MetricSeries(key);
                map.Add(key, series);
            }
            return series;
        }

        static double Aggregate(MetricSeries series, MetricAggregator aggregator)
        {
            if (aggregator == MetricAggregator.Sum)
            {
                double sum = 0;
                var samples = series.Samples;
                for (var i = 0; i < samples.Count; i++) sum += samples[i].Value;
                return sum;
            }

            if (aggregator == MetricAggregator.Last)
            {
                var samples = series.Samples;
                return samples[samples.Count - 1].Value;
            }

            var stat = MetricStats.Summarize(series.SnapshotValues());
            switch (aggregator)
            {
                case MetricAggregator.P50: return stat.P50;
                case MetricAggregator.P95: return stat.P95;
                case MetricAggregator.Min: return stat.Min;
                case MetricAggregator.Max: return stat.Max;
                default: return stat.Avg;
            }
        }

        void Disable(MetricCollector collector, int index, string phase, Exception ex)
        {
            if (index >= 0 && index < _disabled.Count) _disabled[index] = true;
            var message =
                $"[AxuTestAgent] 采集器 \"{collector.Id}\" 在 {phase} 阶段异常，已停用：{ex.GetType().Name}: {ex.Message}";
            if (_context != null && _context.Log != null) _context.Log.Warning(message);
            else Debug.LogWarning(message);
        }

        static double RealtimeSeconds()
        {
            return Time.realtimeSinceStartupAsDouble;
        }

        static double RealtimeMilliseconds()
        {
            return Time.realtimeSinceStartupAsDouble * 1000.0;
        }

        /// <summary>计时作用域（嵌套类型：单一用途，不单列文件；Dispose 幂等）。</summary>
        sealed class TimedScope : IDisposable
        {
            readonly MetricSession _session;
            readonly string _name;
            readonly double _startMilliseconds;
            bool _disposed;

            public TimedScope(MetricSession session, string name, double startMilliseconds)
            {
                _session = session;
                _name = name;
                _startMilliseconds = startMilliseconds;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _session.RecordScope(_name, RealtimeMilliseconds() - _startMilliseconds);
            }
        }

        void RecordScope(string name, double elapsedMilliseconds)
        {
            if (string.IsNullOrEmpty(name)) return;
            GetOrCreateCustom(_scopes, name).Record(elapsedMilliseconds, RealtimeSeconds());
        }
    }
}
