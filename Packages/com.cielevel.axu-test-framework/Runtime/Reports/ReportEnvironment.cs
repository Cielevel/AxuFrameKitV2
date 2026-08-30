namespace AxuTestFramework
{
    /// <summary>
    /// 报告的环境信息节（schema <c>axu-test-report/1</c>）。
    /// </summary>
    public class ReportEnvironment
    {
        /// <summary>Unity 编辑器/运行时版本（Application.unityVersion）。</summary>
        public string UnityVersion { get; set; }

        /// <summary>Unity Pipeline 包版本（未安装 pipeline 时为 null）。</summary>
        public string PipelineVersion { get; set; }

        /// <summary>报告器自身版本（本包版本，当前 0.1.0）。</summary>
        public string ReporterVersion { get; set; }
    }
}
