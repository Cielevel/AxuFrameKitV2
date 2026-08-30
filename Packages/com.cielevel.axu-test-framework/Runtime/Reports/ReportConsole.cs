using System.Collections.Generic;

namespace AxuTestFramework
{
    /// <summary>
    /// 报告的 Console 快照节（schema <c>axu-test-report/1</c>）。由 M2 报告器在运行结束时刻采样。
    /// </summary>
    public class ReportConsole
    {
        /// <summary>运行期间 Console 中 Error 级条目数。</summary>
        public int ErrorCount { get; set; }

        /// <summary>运行期间 Console 中 Warning 级条目数。</summary>
        public int WarningCount { get; set; }

        /// <summary>代表性条目样本（限条数采样，原文截取）；无样本时为 null 或空列表。</summary>
        public List<string> Samples { get; set; }
    }
}
