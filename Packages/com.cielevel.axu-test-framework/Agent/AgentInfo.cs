namespace AxuTestFramework.Agent
{
    /// <summary>
    /// 场景测试 Agent 的元数据常量（随包版本维护）。
    /// </summary>
    public static class AgentInfo
    {
        /// <summary>Agent 版本，写入 axu-scenario-report/1 的 environment.agentVersion。</summary>
        public const string Version = "0.2.0";

        /// <summary>启用 Agent 公开入口的脚本定义符号（Standalone/Editor 手动开启；见包 README）。</summary>
        public const string DefineSymbol = "AXU_TEST_AGENT";
    }
}
