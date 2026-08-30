namespace AxuTestFramework
{
    /// <summary>
    /// 报告的 artifacts 条目（截图等运行期产物，schema <c>axu-test-report/1</c>）。
    /// </summary>
    public class ReportArtifact
    {
        /// <summary>关联测试全名；null 表示运行级产物（不属于单个测试）。</summary>
        public string Test { get; set; }

        /// <summary>产物文件路径（如 AxuCapture 截图的完整路径）。</summary>
        public string Screenshot { get; set; }

        /// <summary>备注说明；可为 null。</summary>
        public string Note { get; set; }
    }
}
