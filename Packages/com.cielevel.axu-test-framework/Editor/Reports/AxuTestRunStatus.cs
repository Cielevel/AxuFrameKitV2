using System;
using System.IO;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// axu_test_run 的运行状态文件模型（<see cref="AxuTestReportPaths.StatusFilePath"/>）。
    /// 生命周期：axu_test_run 启动时写 <see cref="Running"/>；<c>AxuTestReportWriter.RunFinished</c>
    /// 写 <see cref="Completed"/>（含 summary 与 reportPath）或 <see cref="Error"/>。
    /// axu_test_status 命令与看板窗口均轮询此文件。
    /// </summary>
    public sealed class AxuTestRunStatus
    {
        /// <summary>状态值：运行中。</summary>
        public const string Running = "running";

        /// <summary>状态值：运行完成（不代表全部用例通过，看 summary）。</summary>
        public const string Completed = "completed";

        /// <summary>状态值：运行出错（基础设施级错误，非用例失败）。</summary>
        public const string Error = "error";

        /// <summary>当前状态：running / completed / error。</summary>
        public string Status { get; set; }

        /// <summary>运行模式："editor" / "playmode"。</summary>
        public string Mode { get; set; }

        /// <summary>运行开始时间（UTC，ISO 8601）；轮询方可据此判断陈旧状态。</summary>
        public string StartedAtUtc { get; set; }

        /// <summary>运行结束时间（UTC，ISO 8601）；完成/出错时写入。</summary>
        public string EndedAtUtc { get; set; }

        /// <summary>报告 JSON 路径；report=false 或写报告失败时为 null。</summary>
        public string ReportPath { get; set; }

        /// <summary>结果汇总；完成时写入。</summary>
        public ReportSummary Summary { get; set; }

        /// <summary>补充消息（错误原因等）。</summary>
        public string Message { get; set; }

        /// <summary>本次实际运行的程序集清单。</summary>
        public System.Collections.Generic.List<string> Assemblies { get; set; }

        /// <summary>写状态文件（覆盖写，容错：失败仅记日志不抛出——状态文件是尽力而为的信道）。</summary>
        public static void Write(AxuTestRunStatus status)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(status, Formatting.Indented));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[AxuTestFramework] 写运行状态文件失败: {ex.Message}");
            }
        }

        /// <summary>读状态文件；不存在或解析失败返回 null。</summary>
        public static AxuTestRunStatus TryRead()
        {
            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                if (!File.Exists(fullPath))
                    return null;
                return JsonConvert.DeserializeObject<AxuTestRunStatus>(File.ReadAllText(fullPath));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>删除状态文件。</summary>
        public static void Delete()
        {
            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception)
            {
                // 静默
            }
        }
    }
}
