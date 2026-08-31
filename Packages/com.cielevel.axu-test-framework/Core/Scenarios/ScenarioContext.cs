namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// Runner 的宿主服务上下文（Phase 1：句柄 + 日志；后续阶段扩展指标采集/截图/等待原语等）。
    /// </summary>
    public class ScenarioContext
    {
        public ScenarioContext(ScenarioRunHandle handle, IAgentLog log)
        {
            Handle = handle;
            Log = log;
        }

        /// <summary>本次执行的运行句柄（状态/结论收集）。</summary>
        public ScenarioRunHandle Handle { get; }

        /// <summary>宿主日志。</summary>
        public IAgentLog Log { get; }
    }
}
