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
    /// Agent 开始运行写 <see cref="Running"/>（每完成一个 scenario 刷新 CompletedCount/TotalCount）→
    /// 结束写 <see cref="Completed"/>（含 Summary/ExitCode/ReportPath）或 <see cref="Error"/>（基础设施错误）。
    /// 状态文件是尽力而为的信道：缺失/损坏/陈旧时调用方按"无运行"处理。</para>
    /// </summary>
    public sealed class AxuScenarioRunStatus
    {
        /// <summary>状态值：运行中。</summary>
        public const string Running = "running";

        /// <summary>状态值：运行完成（不代表全部通过，看 summary/exitCode）。</summary>
        public const string Completed = "completed";

        /// <summary>状态值：基础设施错误（suite 加载失败等，退出码 3）。</summary>
        public const string Error = "error";

        /// <summary>running 状态陈旧阈值（分钟）：与 pipeline 的 15 分钟自动放行语义一致。</summary>
        public const int StaleMinutes = 15;

        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("suiteId")] public string SuiteId { get; set; }

        [JsonProperty("startedAtUtc")] public string StartedAtUtc { get; set; }

        [JsonProperty("endedAtUtc")] public string EndedAtUtc { get; set; }

        [JsonProperty("completedCount")] public int CompletedCount { get; set; }

        [JsonProperty("totalCount")] public int TotalCount { get; set; }

        [JsonProperty("reportPath")] public string ReportPath { get; set; }

        [JsonProperty("summary")] public ReportSummary Summary { get; set; }

        [JsonProperty("exitCode")] public int? ExitCode { get; set; }

        /// <summary>是否运行中。</summary>
        public bool IsRunning => string.Equals(Status, Running, StringComparison.OrdinalIgnoreCase);

        /// <summary>是否终态（completed/error）。</summary>
        public bool IsTerminal =>
            string.Equals(Status, Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, Error, StringComparison.OrdinalIgnoreCase);

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
                return JsonConvert.DeserializeObject<AxuScenarioRunStatus>(File.ReadAllText(fullPath));
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

        /// <summary>running 且开始时间距今超过 <see cref="StaleMinutes"/> 分钟 = 陈旧（判定语义与 pipeline 一致）。</summary>
        public bool IsStale(DateTime utcNow)
        {
            if (!IsRunning)
                return false;
            return TryGetStartedUtc(out var started) && utcNow - started > TimeSpan.FromMinutes(StaleMinutes);
        }
    }
}
