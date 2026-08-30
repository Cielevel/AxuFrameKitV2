using System.Collections.Generic;

namespace AxuTestFramework
{
    /// <summary>
    /// 单个测试的结果明细（schema <c>axu-test-report/1</c>）。
    /// </summary>
    public class ReportResult
    {
        /// <summary>测试全名（程序集.命名空间.类.方法）。</summary>
        public string FullName { get; set; }

        /// <summary>结果状态（NUnit ResultState，如 "Passed" / "Failed:Error" / "Skipped:Ignored"）。</summary>
        public string Status { get; set; }

        /// <summary>执行耗时（毫秒，含小数）。</summary>
        public double DurationMs { get; set; }

        /// <summary>失败/断言消息；通过时通常为 null。</summary>
        public string Message { get; set; }

        /// <summary>失败堆栈；通过时通常为 null。</summary>
        public string StackTrace { get; set; }

        /// <summary>所属程序集名。</summary>
        public string Assembly { get; set; }

        /// <summary>所属类别（[Category] 特性值）；无类别时为 null。</summary>
        public List<string> Categories { get; set; }
    }
}
