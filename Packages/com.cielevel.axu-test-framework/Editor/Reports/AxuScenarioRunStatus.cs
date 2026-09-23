using System;
using System.Globalization;
using System.IO;
using AxuTestFramework.Core.Report;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 运行状态文件模型（Temp/axu_scenario_status.json，Agent 侧 ScenarioStatusFile 写入；
    /// 字段与 pipeline 命令 axu_scenario_status 读取的原文同构）。
    /// <para>看板 Scenario 页轮询此文件渲染"第 x/N 个场景"进度。生命周期：
    /// Agent 开始运行写 <see cref="Running"/>（每完成一个 scenario 刷新 CompletedCount/TotalCount，
    /// 长 scenario 期间按心跳间隔续写）→ 结束写 <see cref="Completed"/>（含 Summary/ExitCode/ReportPath）、
    /// <see cref="Error"/>（基础设施错误，见 <see cref="Reason"/>）或由 Editor 看门狗补写 <see cref="Aborted"/>
    /// （Play Mode 提前退出 = 用户手动停止/Editor 中断）。</para>
    /// <para>本文件是"最近一次 scenario 运行"的事实来源：<see cref="RunId"/> 由发起方生成并写入桥文件，
    /// 消费端据此确认归属（不匹配即拒收），<see cref="HeartbeatUtc"/> 是判活的唯一可靠依据
    /// （单个 scenario 可跑 20 分钟以上，启动时间不足以判活）。状态文件是尽力而为的信道：
    /// 缺失/损坏/陈旧时调用方按"无运行"处理。</para>
    /// </summary>
    public sealed class AxuScenarioRunStatus
    {
        /// <summary>状态值：运行中。</summary>
        public const string Running = "running";

        /// <summary>状态值：运行完成（不代表全部通过，看 summary/exitCode）。</summary>
        public const string Completed = "completed";

        /// <summary>状态值：基础设施错误（suite 加载失败 / 报告写盘失败等，退出码 3）。</summary>
        public const string Error = "error";

        /// <summary>状态值：被中断（Play Mode 在写出终态前退出 = 用户手动停止或 Editor 中断，无本次结果）。</summary>
        public const string Aborted = "aborted";

        /// <summary>reason：suite/scenario 文件加载或解析失败。</summary>
        public const string ReasonSuiteLoadFailed = "suite_load_failed";

        /// <summary>reason：报告写盘失败。</summary>
        public const string ReasonReportWriteFailed = "report_write_failed";

        /// <summary>reason：Play Mode 在运行结束前退出（手动停止/Editor 中断）。</summary>
        public const string ReasonInterrupted = "interrupted";

        /// <summary>reason：Agent 未接手运行（Agent 入口未编译或未产生状态）。</summary>
        public const string ReasonAgentNotRun = "agent_not_run";

        /// <summary>running 状态陈旧阈值（分钟）：最后活动（心跳/写入/启动）超过该时长的 running 视为中断残留。</summary>
        public const int StaleMinutes = AxuRunLiveness.StaleMinutes;

        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("suiteId")] public string SuiteId { get; set; }

        /// <summary>本轮运行标识（发起方生成，桥文件传入；未提供/非桥文件来源时为空）。</summary>
        [JsonProperty("runId")] public string RunId { get; set; }

        [JsonProperty("startedAtUtc")] public string StartedAtUtc { get; set; }

        /// <summary>最后心跳时间（UTC）：每次状态写入刷新，"仍在运行"的活信号。</summary>
        [JsonProperty("heartbeatUtc")] public string HeartbeatUtc { get; set; }

        [JsonProperty("endedAtUtc")] public string EndedAtUtc { get; set; }

        [JsonProperty("completedCount")] public int CompletedCount { get; set; }

        [JsonProperty("totalCount")] public int TotalCount { get; set; }

        [JsonProperty("reportPath")] public string ReportPath { get; set; }

        /// <summary>
        /// 结果汇总（completed 时写入）。类型必须显式写全：本文件命名空间是 <c>AxuTestFramework.Editor</c>，
        /// 裸写 <c>ReportSummary</c> 会先命中 <c>AxuTestFramework</c> 下 UTF 报告用的那个同名类型
        /// （字段 total/passed/failed + skipped/inconclusive，**没有 warned**），导致 scenario 的
        /// warned 计数被静默丢弃。
        /// </summary>
        [JsonProperty("summary")] public AxuTestFramework.Core.Report.ReportSummary Summary { get; set; }

        [JsonProperty("exitCode")] public int? ExitCode { get; set; }

        /// <summary>错误/中断原因码（仅 error/aborted 终态写入；见 Reason* 常量）。</summary>
        [JsonProperty("reason")] public string Reason { get; set; }

        /// <summary>面向人的失败/中断说明（仅 error/aborted 终态写入）。</summary>
        [JsonProperty("message")] public string Message { get; set; }

        /// <summary>读状态文件时记录的文件写入时间（UTC）：心跳之外的第二道活信号，不参与序列化。</summary>
        [JsonIgnore] public DateTime? FileLastWriteUtc { get; set; }

        /// <summary>是否运行中。</summary>
        public bool IsRunning => string.Equals(Status, Running, StringComparison.OrdinalIgnoreCase);

        /// <summary>是否终态（completed/error/aborted）。</summary>
        public bool IsTerminal =>
            string.Equals(Status, Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, Error, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, Aborted, StringComparison.OrdinalIgnoreCase);

        /// <summary>运行是否正常结束（completed）。</summary>
        public bool IsCompleted => string.Equals(Status, Completed, StringComparison.OrdinalIgnoreCase);

        /// <summary>读取默认 Editor 状态文件（Temp/axu_scenario_status.json）；不存在/解析失败返回 null。</summary>
        public static AxuScenarioRunStatus TryRead()
        {
            return TryRead(ScenarioRunService.StatusFileFullPath);
        }

        /// <summary>读取指定路径的状态文件；不存在/解析失败返回 null（状态文件损坏不抛异常）。</summary>
        public static AxuScenarioRunStatus TryRead(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    return null;

                var status = JsonConvert.DeserializeObject<AxuScenarioRunStatus>(File.ReadAllText(fullPath));
                if (status != null)
                    status.FileLastWriteUtc = File.GetLastWriteTimeUtc(fullPath);
                return status;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>解析开始时间（UTC）；字段缺失/格式非法返回 false。</summary>
        public bool TryGetStartedUtc(out DateTime startedUtc)
        {
            return DateTime.TryParse(StartedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out startedUtc);
        }

        /// <summary>解析最后心跳时间（UTC）；缺失/非法返回 false。</summary>
        public bool TryGetHeartbeatUtc(out DateTime heartbeatUtc)
        {
            return DateTime.TryParse(HeartbeatUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out heartbeatUtc);
        }

        /// <summary>
        /// 最后活动时间（UTC）：心跳 &gt; 状态文件写入时间 &gt; 启动时间，取三者最新——任一缺失都可降级。
        /// 这是"运行是否已停止"的唯一依据（长跑 scenario 期间启动时间恒早，不能用于判活）。
        /// </summary>
        public bool TryGetLastActivityUtc(out DateTime lastActivityUtc)
        {
            lastActivityUtc = DateTime.MinValue;
            var found = false;

            if (TryGetStartedUtc(out var started))
            {
                lastActivityUtc = started;
                found = true;
            }
            if (TryGetHeartbeatUtc(out var heartbeat) && (!found || heartbeat > lastActivityUtc))
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

        /// <summary>running 且最后活动距今超过 <see cref="StaleMinutes"/> 分钟 = 陈旧（判定语义与 pipeline 一致）。</summary>
        public bool IsStale(DateTime utcNow)
        {
            if (!IsRunning)
                return false;
            // 时间字段全缺时保守视为陈旧：无法判活的 running 不应长期占用互斥闸
            return !TryGetLastActivityUtc(out var lastActivity) || utcNow - lastActivity > TimeSpan.FromMinutes(StaleMinutes);
        }

        /// <summary>
        /// 运行是否在途（供互斥闸）：running 且不陈旧。判活依据是最后活动时间（心跳/写入/启动），
        /// 因此 20 分钟的 soak 只要持续心跳就不会被当作陈旧放行。
        /// </summary>
        public static bool IsInFlight(AxuScenarioRunStatus status, DateTime utcNow)
        {
            return status != null && status.IsRunning && !status.IsStale(utcNow);
        }
    }
}
