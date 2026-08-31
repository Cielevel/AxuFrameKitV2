namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>单个 scenario 的运行期状态（运行句柄用；报告 status 串映射：pass/warn/fail/error）。</summary>
    public enum RunStatus
    {
        /// <summary>执行中。</summary>
        Running,

        /// <summary>通过。</summary>
        Passed,

        /// <summary>通过但有警告。</summary>
        Warned,

        /// <summary>预算/条件判定失败。</summary>
        Failed,

        /// <summary>执行异常（超时/能力缺失/未捕获异常等，区别于预算 fail）。</summary>
        Error,

        /// <summary>被取消。</summary>
        Cancelled,
    }
}
