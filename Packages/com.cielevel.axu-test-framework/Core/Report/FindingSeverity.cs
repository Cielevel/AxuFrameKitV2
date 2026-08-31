namespace AxuTestFramework.Core.Report
{
    /// <summary>结论严重级别（schema 串映射为 "info" / "warn" / "error"）。</summary>
    public enum FindingSeverity
    {
        /// <summary>信息（留痕，不影响判定）。</summary>
        Info,

        /// <summary>警告（默认计入退出码 2）。</summary>
        Warning,

        /// <summary>错误（计入退出码 1）。</summary>
        Error,
    }
}
