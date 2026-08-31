using System;
using System.Globalization;
using System.IO;
using System.Text;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Report;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AxuTestFramework.Agent.Runner
{
    /// <summary>
    /// scenario 运行状态文件（Editor 落 Temp/axu_scenario_status.json，Player 落 persistentDataPath），
    /// 供 pipeline/看板轮询 running/completed/error 与报告路径；
    /// 写入失败一律吞掉并 LogWarning，绝不影响测试本体。
    /// </summary>
    public static class ScenarioStatusFile
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>写入 running 状态（每次 scenario 完成后由宿主刷新进度）。</summary>
        public static void WriteRunning(string suiteId, DateTime startedAtUtc, int completedCount, int totalCount, string reportPath)
        {
            Write("running", suiteId, startedAtUtc, null, completedCount, totalCount, reportPath, null, null);
        }

        /// <summary>写入 completed 状态。</summary>
        public static void WriteCompleted(string suiteId, DateTime startedAtUtc, int completedCount, int totalCount, string reportPath, ReportSummary summary, int exitCode)
        {
            Write("completed", suiteId, startedAtUtc, DateTime.UtcNow, completedCount, totalCount, reportPath, summary, exitCode);
        }

        /// <summary>写入 error 状态（suite 加载/解析失败等基础设施错误，退出码 3）。</summary>
        public static void WriteError(string suiteId, DateTime startedAtUtc, string reportPath)
        {
            Write("error", suiteId, startedAtUtc, DateTime.UtcNow, 0, 0, reportPath, null, (int)ScenarioExitCode.InfrastructureError);
        }

        static void Write(string status, string suiteId, DateTime startedAtUtc, DateTime? endedAtUtc,
            int completedCount, int totalCount, string reportPath, ReportSummary summary, int? exitCode)
        {
            try
            {
                var path = AgentPaths.StatusFilePath;
                if (string.IsNullOrEmpty(path)) return;

                var root = new JObject
                {
                    ["status"] = status,
                    ["suiteId"] = suiteId ?? string.Empty,
                    ["startedAtUtc"] = FormatUtc(startedAtUtc),
                    ["endedAtUtc"] = endedAtUtc.HasValue ? FormatUtc(endedAtUtc.Value) : null,
                    ["completedCount"] = completedCount,
                    ["totalCount"] = totalCount,
                    ["reportPath"] = reportPath ?? string.Empty,
                    ["summary"] = summary == null ? null : JToken.FromObject(summary),
                    ["exitCode"] = exitCode,
                };

                AgentPaths.EnsureDirectoryForFile(path);
                File.WriteAllText(path, root.ToString(Formatting.Indented), Utf8NoBom);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AxuTestAgent] 状态文件写入失败：" + ex.Message);
            }
        }

        static string FormatUtc(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }
    }
}
