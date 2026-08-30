namespace AxuTestFramework
{
    /// <summary>
    /// 报告的结果汇总计数（schema <c>axu-test-report/1</c>）。
    /// </summary>
    public class ReportSummary
    {
        /// <summary>叶子测试总数（不含套件节点）。</summary>
        public int Total { get; set; }

        /// <summary>通过数。</summary>
        public int Passed { get; set; }

        /// <summary>失败数。</summary>
        public int Failed { get; set; }

        /// <summary>跳过数（含显式 Ignored）。</summary>
        public int Skipped { get; set; }

        /// <summary>不确定结果数（Inconclusive）。</summary>
        public int Inconclusive { get; set; }
    }
}
