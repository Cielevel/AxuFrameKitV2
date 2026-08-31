using System.Collections.Generic;

namespace AxuTestFramework.Core.Metrics
{
    /// <summary>同一 stat 的采样序列（键 + 有界环形缓冲；容量默认 4096，长跑防涨内存）。</summary>
    public class MetricSeries
    {
        /// <summary>默认容量（250ms 采样间隔下约 17 分钟历史）。</summary>
        public const int DefaultCapacity = 4096;

        readonly RingBuffer<MetricSample> _samples;

        public MetricSeries(string key, int capacity = DefaultCapacity)
        {
            Key = key;
            _samples = new RingBuffer<MetricSample>(capacity);
        }

        /// <summary>序列键（诊断/展示用）。</summary>
        public string Key { get; }

        /// <summary>当前采样数。</summary>
        public int Count => _samples.Count;

        /// <summary>底层环形缓冲（含时间戳的完整采样）。</summary>
        public RingBuffer<MetricSample> Samples => _samples;

        /// <summary>追加一个采样。</summary>
        public void Record(double value, double timeSeconds)
        {
            _samples.Add(new MetricSample(value, timeSeconds));
        }

        /// <summary>采样值快照（统计入参用；每次分配一个列表，仅在聚合时调用）。</summary>
        public List<double> SnapshotValues()
        {
            var values = new List<double>(_samples.Count);
            for (var i = 0; i < _samples.Count; i++) values.Add(_samples[i].Value);
            return values;
        }
    }
}
