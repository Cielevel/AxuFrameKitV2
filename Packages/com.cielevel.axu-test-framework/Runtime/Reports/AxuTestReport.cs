using System.Collections.Generic;

namespace AxuTestFramework
{
    /// <summary>
    /// 测试报告数据模型（schema <c>axu-test-report/1</c>）。纯 POCO，无行为；
    /// 由 AxuTestFramework.Editor 层的报告器（M2：AxuTestReportWriter）填充并序列化为 JSON。
    /// </summary>
    public class AxuTestReport
    {
        /// <summary>当前报告 schema 标识常量。</summary>
        public const string SchemaId = "axu-test-report/1";

        /// <summary>schema 标识，默认取 <see cref="SchemaId"/>。</summary>
        public string Schema { get; set; } = SchemaId;

        /// <summary>运行开始时间（UTC，ISO 8601 字符串）。</summary>
        public string StartedAt { get; set; }

        /// <summary>运行结束时间（UTC，ISO 8601 字符串）。</summary>
        public string EndedAt { get; set; }

        /// <summary>运行模式："editor" / "playmode" / "all"。</summary>
        public string Mode { get; set; }

        /// <summary>测试过滤条件。</summary>
        public FilterInfo Filter { get; set; }

        /// <summary>结果汇总。</summary>
        public ReportSummary Summary { get; set; }

        /// <summary>逐测试结果明细。</summary>
        public List<ReportResult> Results { get; set; }

        /// <summary>运行期间的 Console 快照。</summary>
        public ReportConsole Console { get; set; }

        /// <summary>运行期间产生的 artifacts（截图等）。</summary>
        public List<ReportArtifact> Artifacts { get; set; }

        /// <summary>环境信息（Unity 版本 / pipeline 版本 / 报告器版本）。</summary>
        public ReportEnvironment Environment { get; set; }
    }
}
