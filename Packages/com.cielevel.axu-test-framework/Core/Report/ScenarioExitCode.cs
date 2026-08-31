namespace AxuTestFramework.Core.Report
{
    /// <summary>一次 suite 运行的进程退出码（CI 门禁依据，见 DOC/ScenarioSchema.md §3）。</summary>
    public enum ScenarioExitCode
    {
        /// <summary>0：全部 pass。</summary>
        Success = 0,

        /// <summary>1：存在 fail（含 scenario 执行 error）。</summary>
        Failures = 1,

        /// <summary>2：仅 warn（-axuWarnAsFail 时归入 1）。</summary>
        WarningsOnly = 2,

        /// <summary>3：基础设施错误（suite 加载/解析失败、能力缺失、报告写盘失败等）。</summary>
        InfrastructureError = 3,
    }
}
