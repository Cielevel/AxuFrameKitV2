using System.IO;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 报告与运行状态的路径约定（相对工程根；Editor 专用）。
    /// 落盘目录默认 <c>Library/AxuTestReports/</c>（Library 不入库，留档经 out_path 参数改写）。
    /// </summary>
    public static class AxuTestReportPaths
    {
        /// <summary>报告默认落盘目录（相对工程根）。</summary>
        public const string ReportsDirectory = "Library/AxuTestReports";

        /// <summary>报告默认 artifacts 目录（相对工程根，与 Runtime 层 AxuCapture.DefaultArtifactsDirectory 一致）。</summary>
        public const string DefaultArtifactsDirectory = AxuCapture.DefaultArtifactsDirectory;

        /// <summary>axu_test_run 启动前写入的运行上下文文件（跨域重载传参载体，相对工程根）。</summary>
        public const string ContextFilePath = "Temp/axu_test_run_context.json";

        /// <summary>运行状态文件：axu_test_run 写 running，AxuTestReportWriter 在 RunFinished 时写 completed/error（相对工程根）。</summary>
        public const string StatusFilePath = "Temp/axu_test_run_status.json";

        /// <summary>报告 JSON 读取保护的截断上限（字节）。超过时 axu_test_report 拒绝返回原文。</summary>
        public const int ReportReadLimitBytes = 256 * 1024;

        /// <summary>工程根完整物理路径（Application.dataPath 的上一级）。</summary>
        public static string ProjectRootFullPath
        {
            get { return Path.GetDirectoryName(Application.dataPath); }
        }

        /// <summary>
        /// 把相对工程根的路径（正斜杠或反斜杠）转为完整物理路径；已为绝对路径时原样返回。
        /// </summary>
        public static string ToFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return ProjectRootFullPath;

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalized))
                return normalized;

            return Path.Combine(ProjectRootFullPath, normalized);
        }

        /// <summary>
        /// 由报告文件路径推导 artifacts 目录：报告所在目录下的 <c>artifacts</c> 子目录。
        /// </summary>
        public static string GetArtifactsDirectoryFor(string reportFilePath)
        {
            var directory = string.IsNullOrEmpty(reportFilePath)
                ? ToFullPath(ReportsDirectory)
                : Path.GetDirectoryName(ToFullPath(reportFilePath));
            return Path.Combine(directory ?? ProjectRootFullPath, "artifacts");
        }
    }
}
