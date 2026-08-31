namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 预算违约判定级别（schema 串 "fail" / "warn"，默认 fail，见 DOC/ScenarioSchema.md §1）。
    /// </summary>
    public enum BudgetSeverity
    {
        /// <summary>违约即 fail（计入退出码 1）。</summary>
        Fail,

        /// <summary>违约仅 warn（默认计入退出码 2）。</summary>
        Warn,
    }
}
