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
    /// <para>字段契约（消费端见 Editor 侧 <c>AxuScenarioRunStatus</c>）：
    /// <c>runId</c> 为发起方生成的本轮标识（bridge 传入，未提供则为空——消费端据此拒收陈旧状态）；
    /// <c>heartbeatUtc</c> 每次写入刷新，是"仍在运行"的唯一可靠信号
    /// （长跑 scenario 单个场景可跑 20 分钟以上，启动时间无法用于判活）。</para>
    /// </summary>
    public static class ScenarioStatusFile
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>运行中。</summary>
        public const string Running = "running";

        /// <summary>运行完成（不代表全部通过；看 summary/exitCode）。</summary>
        public const string Completed = "completed";

        /// <summary>基础设施错误（suite 加载失败 / 报告写盘失败等，退出码 3）。</summary>
        public const string Error = "error";

        /// <summary>error 原因：suite/scenario 文件加载或解析失败。</summary>
        public const string ReasonSuiteLoadFailed = "suite_load_failed";

        /// <summary>error 原因：报告写盘失败。</summary>
        public const string ReasonReportWriteFailed = "report_write_failed";

        /// <summary>写入 running 状态（每次 scenario 完成后由宿主刷新进度）。</summary>
        public static void WriteRunning(string suiteId, DateTime startedAtUtc, int completedCount, int totalCount,
            string reportPath, string runId = null)
        {
            Write(Running, suiteId, startedAtUtc, null, completedCount, totalCount, reportPath, null, null, runId, null, null);
        }

        /// <summary>
        /// 心跳续写：运行仍在进行（长 scenario 期间周期调用）——内容与 running 一致、刷新 heartbeatUtc，
        /// 让互斥闸与看板据此判定"仍在运行"而不是依据启动时间误判为陈旧。
        /// </summary>
        public static void Heartbeat(string suiteId, DateTime startedAtUtc, int completedCount, int totalCount,
            string reportPath, string runId = null)
        {
            WriteRunning(suiteId, startedAtUtc, completedCount, totalCount, reportPath, runId);
        }

        /// <summary>写入 completed 状态。</summary>
        public static void WriteCompleted(string suiteId, DateTime startedAtUtc, int completedCount, int totalCount,
            string reportPath, ReportSummary summary, int exitCode, string runId = null)
        {
            Write(Completed, suiteId, startedAtUtc, DateTime.UtcNow, completedCount, totalCount, reportPath, summary,
                exitCode, runId, null, null);
        }

        /// <summary>写入 error 状态（suite 加载/解析失败、报告写盘失败等基础设施错误，退出码 3）。</summary>
        public static void WriteError(string suiteId, DateTime startedAtUtc, string reportPath,
            string reason = null, string message = null, string runId = null)
        {
            Write(Error, suiteId, startedAtUtc, DateTime.UtcNow, 0, 0, reportPath, null,
                (int)ScenarioExitCode.InfrastructureError, runId, reason, message);
        }

        static void Write(string status, string suiteId, DateTime startedAtUtc, DateTime? endedAtUtc,
            int completedCount, int totalCount, string reportPath, ReportSummary summary, int? exitCode,
            string runId, string reason, string message)
        {
            try
            {
                var path = AgentPaths.StatusFilePath;
                if (string.IsNullOrEmpty(path)) return;

                var root = new JObject
                {
                    ["status"] = status,
                    ["suiteId"] = suiteId ?? string.Empty,
                    ["runId"] = runId ?? string.Empty,
                    ["startedAtUtc"] = FormatUtc(startedAtUtc),
                    ["heartbeatUtc"] = FormatUtc(DateTime.UtcNow),
                    ["endedAtUtc"] = endedAtUtc.HasValue ? FormatUtc(endedAtUtc.Value) : null,
                    ["completedCount"] = completedCount,
                    ["totalCount"] = totalCount,
                    ["reportPath"] = reportPath ?? string.Empty,
                    ["summary"] = summary == null ? null : JToken.FromObject(summary),
                    ["exitCode"] = exitCode,
                    // 失败原因只在错误态写入，成功/运行中保持 null（消费端据 reason 区分"加载失败/写盘失败/被中断"）
                    ["reason"] = status == Error ? reason : null,
                    ["message"] = status == Error ? message : null,
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
