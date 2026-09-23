using System;
using System.IO;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// axu_test_run 的运行状态文件模型（<see cref="AxuTestReportPaths.StatusFilePath"/>）。
    /// 生命周期：axu_test_run 启动时写 <see cref="Running"/>；运行期间由心跳（<see cref="TouchHeartbeat"/>）
    /// 周期性刷新 <see cref="HeartbeatUtc"/>；<c>AxuTestReportWriter.RunFinished</c> 写
    /// <see cref="Completed"/>（含 summary 与 reportPath）或 <see cref="Error"/>。
    /// axu_test_status 命令与看板窗口均轮询此文件。
    /// <para>判活规则：只认"最后活动时间"（心跳 &gt; 文件写入时间 &gt; 启动时间）——PlayMode 单条用例
    /// 可以跑十几分钟，用启动时间判定会把仍在运行的测试误判为陈旧并放行并发运行。
    /// 字段为 PascalCase（沿用 0.3.0 起的既有形态；本文件与 scenario 状态文件的 camelCase 形态不同，
    /// 二者是独立契约，勿互抄）。</para>
    /// </summary>
    public sealed class AxuTestRunStatus
    {
        /// <summary>状态值：运行中。</summary>
        public const string Running = "running";

        /// <summary>状态值：运行完成（不代表全部用例通过，看 summary）。</summary>
        public const string Completed = "completed";

        /// <summary>状态值：运行出错（基础设施级错误，非用例失败）。</summary>
        public const string Error = "error";

        /// <summary>最后活动超过该时长（分钟）的 running 视为中断残留（与 scenario 侧同口径）。</summary>
        public const int StaleMinutes = AxuRunLiveness.StaleMinutes;

        /// <summary>当前状态：running / completed / error。</summary>
        public string Status { get; set; }

        /// <summary>运行模式："editor" / "playmode"。</summary>
        public string Mode { get; set; }

        /// <summary>运行开始时间（UTC，ISO 8601）；陈旧判定请用 <see cref="HeartbeatUtc"/> 或文件写入时间。</summary>
        public string StartedAtUtc { get; set; }

        /// <summary>最后心跳时间（UTC，ISO 8601）：运行期间周期性刷新，是"仍在运行"的活信号。</summary>
        public string HeartbeatUtc { get; set; }

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

        /// <summary>
        /// <c>mode=all</c> 的前置阶段（EditMode 段）结果：PlayMode 段异步运行时，轮询方据此看到
        /// 第一段已确定的结果（否则被 "playmode_running" 掩盖）。非 all 模式为 null。
        /// 跨域传递路径：运行上下文文件 → 报告器 → 本字段（域重载会清空静态字段，文件是唯一载体）。
        /// </summary>
        public AxuTestRunPhase PrecedingPhase { get; set; }

        /// <summary>读状态文件时记录的文件写入时间（UTC）：心跳之外的第二道活信号，不参与序列化。</summary>
        [JsonIgnore] public DateTime? FileLastWriteUtc { get; set; }

        /// <summary>是否运行中。</summary>
        public bool IsRunning => string.Equals(Status, Running, StringComparison.OrdinalIgnoreCase);

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

        /// <summary>
        /// 心跳续写：仅当状态文件当前为 running 时刷新 <see cref="HeartbeatUtc"/>
        /// （读-改-写，保留 mode/assemblies 等字段），终态不被触碰。
        /// 由运行期间的 <see cref="AxuTestHeartbeat"/> 周期调用——长跑用例不写心跳会让互斥闸误判为陈旧。
        /// </summary>
        public static void TouchHeartbeat()
        {
            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                var status = TryRead(fullPath);
                if (status == null || !status.IsRunning)
                    return;

                status.HeartbeatUtc = DateTime.UtcNow.ToString("o");
                status.FileLastWriteUtc = null;
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(status, Formatting.Indented));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[AxuTestFramework] 运行心跳写入失败: {ex.Message}");
            }
        }

        /// <summary>读默认路径状态文件；不存在或解析失败返回 null。</summary>
        public static AxuTestRunStatus TryRead()
        {
            return TryRead(AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath));
        }

        /// <summary>读指定路径状态文件；不存在/解析失败返回 null（顺带记录文件写入时间作活信号）。</summary>
        public static AxuTestRunStatus TryRead(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    return null;

                var status = JsonConvert.DeserializeObject<AxuTestRunStatus>(File.ReadAllText(fullPath));
                if (status != null)
                    status.FileLastWriteUtc = File.GetLastWriteTimeUtc(fullPath);
                return status;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>最后活动时间（UTC）：心跳 &gt; 文件写入时间 &gt; 启动时间，取三者最新。</summary>
        public bool TryGetLastActivityUtc(out DateTime lastActivityUtc)
        {
            lastActivityUtc = DateTime.MinValue;
            var found = false;

            if (TryParseUtc(StartedAtUtc, out var started))
            {
                lastActivityUtc = started;
                found = true;
            }
            if (TryParseUtc(HeartbeatUtc, out var heartbeat) && (!found || heartbeat > lastActivityUtc))
            {
                lastActivityUtc = heartbeat;
                found = true;
            }
            if (FileLastWriteUtc.HasValue && (!found || FileLastWriteUtc.Value > lastActivityUtc))
            {
                lastActivityUtc = FileLastWriteUtc.Value;
                found = true;
            }
            return found;
        }

        /// <summary>
        /// 运行是否在途（供互斥闸）：running 且最后活动未越过 <see cref="StaleMinutes"/>。
        /// 心跳在跑的长测试（含 >15 分钟）恒判在途；崩溃/中断留下的 running 会随时间自动放行。
        /// </summary>
        public static bool IsInFlight(AxuTestRunStatus status, DateTime utcNow)
        {
            if (status == null || !status.IsRunning)
                return false;

            // 时间字段全缺时保守视为陈旧（无法判活的 running 不应长期占用互斥闸）
            return status.TryGetLastActivityUtc(out var lastActivity)
                   && utcNow - lastActivity <= TimeSpan.FromMinutes(StaleMinutes);
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

        static bool TryParseUtc(string value, out DateTime utc)
        {
            return DateTime.TryParse(value, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out utc);
        }
    }
}
