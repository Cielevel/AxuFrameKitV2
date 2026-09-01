namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// Runner 的宿主服务上下文（Phase 1：句柄 + 日志；Phase 3 增补 artifacts 目录与生效种子）。
    /// </summary>
    public class ScenarioContext
    {
        public ScenarioContext(ScenarioRunHandle handle, IAgentLog log, string artifactsDir = null, int? seed = null)
        {
            Handle = handle;
            Log = log;
            ArtifactsDir = artifactsDir;
            Seed = seed;
        }

        /// <summary>本次执行的运行句柄（状态/结论收集）。</summary>
        public ScenarioRunHandle Handle { get; }

        /// <summary>宿主日志。</summary>
        public IAgentLog Log { get; }

        /// <summary>截图等工件的输出目录（可空；空表示本次运行无 artifacts 目录，runner 据此跳过截图）。</summary>
        public string ArtifactsDir { get; }

        /// <summary>本次生效的随机种子（launch.Seed 优先，其次 params.seed，缺省 0；可空表示未应用）。</summary>
        public int? Seed { get; }
    }
}
