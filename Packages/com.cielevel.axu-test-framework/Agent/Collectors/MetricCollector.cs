using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Core.Scenarios;

namespace AxuTestFramework.Agent.Collectors
{
    /// <summary>
    /// 度量采集器抽象。生命周期由 MetricSession/宿主驱动：
    /// Start（每 scenario 一次）→ OnFrame（每帧累计，不直接入序列）→ OnInterval（按 sampleIntervalMs 聚合入序列）
    /// → Complete（补录全程口径、释放资源）→ CollectFindings（自检结论汇入句柄）。
    /// 纪律：不使用任何 YieldInstruction 计时（等待契约由宿主逐帧推进保证）；
    /// 不可用环境（如 Release 构建无 Profiler 数据）自检并产出明确 Finding（COLLECTOR_UNAVAILABLE），绝不静默 0 值。
    /// </summary>
    public abstract class MetricCollector
    {
        /// <summary>采集器 id（注册表键；预算键 &lt;collector&gt;.&lt;stat&gt; 的 collector 段）。</summary>
        public abstract string Id { get; }

        /// <summary>输出声明（stat 固定清单 + 每 stat 聚合方式）。</summary>
        public abstract CollectorOutput Output { get; }

        /// <summary>每 scenario 开始（保存会话引用、复位内部状态、创建底层句柄）。</summary>
        public virtual void Start(ScenarioContext context, MetricSession session) { }

        /// <summary>帧节拍（宿主每帧驱动）。</summary>
        public virtual void OnFrame(double timeSeconds, float unscaledDeltaTime) { }

        /// <summary>采样节拍（宿主按 sampleIntervalMs 驱动；把窗口聚合值写入会话序列）。</summary>
        public virtual void OnInterval(double timeSeconds) { }

        /// <summary>收尾（补录全程口径统计、Stop/Dispose 底层句柄）。</summary>
        public virtual void Complete(double timeSeconds) { }

        /// <summary>把自检产出的不可用等结论汇入句柄（默认无）。</summary>
        public virtual void CollectFindings(ScenarioRunHandle handle) { }
    }
}
