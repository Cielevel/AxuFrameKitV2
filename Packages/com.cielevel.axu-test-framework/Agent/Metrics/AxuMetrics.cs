using System;

namespace AxuTestFramework.Agent.Metrics
{
    /// <summary>
    /// 游戏代码打点静态门面（方案 §5.1）：Scope 计时 / Counter 累计 / Gauge 瞬时值。
    /// 无活动会话（MetricSession.Current == null）时全部 no-op 且零分配（先查静态会话引用，
    /// 空会话返回单例空作用域）——Release 构建可安全常驻调用，游戏侧无需 #if AXU_TEST_AGENT 守护。
    /// 打点归入 "custom" 通道：Counter → custom.counter.&lt;key&gt;（取 Sum）；
    /// Gauge → custom.gauge.&lt;key&gt;.avg/.max；Scope → custom.scope.&lt;name&gt;.avg/.p95/.max（Dispose 记毫秒）。
    /// </summary>
    public static class AxuMetrics
    {
        /// <summary>计时作用域：Dispose 时把耗时（毫秒）记入 scope.&lt;name&gt; 序列。</summary>
        public static IDisposable Scope(string name)
        {
            var session = MetricSession.Current; // 零分配 no-op 路径：先查静态会话引用
            return session != null && !string.IsNullOrEmpty(name)
                ? session.BeginScope(name)
                : NullScope.Instance;
        }

        /// <summary>累计计数（delta 默认 1；汇总取 Sum）。</summary>
        public static void Counter(string key, double delta = 1)
        {
            var session = MetricSession.Current;
            if (session != null && !string.IsNullOrEmpty(key)) session.RecordCounter(key, delta);
        }

        /// <summary>瞬时值（汇总取 Avg 与 Max）。</summary>
        public static void Gauge(string key, double value)
        {
            var session = MetricSession.Current;
            if (session != null && !string.IsNullOrEmpty(key)) session.RecordGauge(key, value);
        }

        /// <summary>空作用域单例（无会话时 Scope 的返回值，避免每次分配）。</summary>
        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            NullScope() { }

            public void Dispose() { }
        }
    }
}
