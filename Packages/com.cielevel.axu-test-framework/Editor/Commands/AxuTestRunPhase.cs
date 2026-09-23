using System;
using System.Globalization;
using AxuTestFramework.Core.Report;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 一次测试运行中单个阶段（EditMode / PlayMode）的结果。
    /// <para>存在原因：<c>mode=all</c> 会先后跑两段，旧实现只保留最后一次写状态文件的那一段，
    /// EditMode 段的超时/错误会被 PlayMode 段的 "playmode_running" 覆盖，调用方看不出整体结论。
    /// 阶段模型让两段结果各自留存（<see cref="AxuTestRunOutcome.Phases"/>），并可被
    /// <c>axu_test_status</c> 的轮询方经状态文件的 <c>PrecedingPhase</c> 读到。</para>
    /// </summary>
    public sealed class AxuTestRunPhase
    {
        /// <summary>阶段状态：已完成（有 summary/report）。</summary>
        public const string StatusCompleted = "completed";

        /// <summary>阶段状态：等待完成超时（未拿到终态）。</summary>
        public const string StatusTimeout = "timeout";

        /// <summary>阶段状态：基础设施错误（报告流程异常等）。</summary>
        public const string StatusError = "error";

        /// <summary>阶段状态：已启动、异步进行中。</summary>
        public const string StatusRunning = "running";

        /// <summary>阶段模式："editor" / "playmode"。</summary>
        public string Mode { get; set; }

        /// <summary>阶段状态（见 Status* 常量）。</summary>
        public string Status { get; set; }

        /// <summary>该阶段结果汇总（status=completed 时填写）。
        /// 类型是 UTF 报告用的 <c>AxuTestFramework.ReportSummary</c>（命名空间嵌套决定）——
        /// 与 scenario 的 <c>AxuTestFramework.Core.Report.ReportSummary</c> 同名，勿混。</summary>
        public ReportSummary Summary { get; set; }

        /// <summary>该阶段报告 JSON 完整路径（有报告时填写）。</summary>
        public string ReportPath { get; set; }

        /// <summary>该阶段耗时（毫秒）。</summary>
        public double DurationMs { get; set; }

        /// <summary>该阶段是否全部通过（total&gt;0 且 failed==0 且 inconclusive==0）。</summary>
        public bool AllPassed { get; set; }

        /// <summary>该阶段未正常完成的原因（status=timeout/error 时填写）。</summary>
        public string Error { get; set; }

        /// <summary>阶段是否已到达终态（completed/timeout/error；running 不是）。</summary>
        public bool IsTerminal =>
            string.Equals(Status, StatusCompleted, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, StatusTimeout, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, StatusError, StringComparison.OrdinalIgnoreCase);

        /// <summary>把 <see cref="AxuTestRunOutcome"/>（单段运行的返回值）投影为该阶段的模型。</summary>
        public static AxuTestRunPhase FromOutcome(string mode, AxuTestRunOutcome outcome)
        {
            if (outcome == null)
                return new AxuTestRunPhase { Mode = mode, Status = StatusError, Error = "（无返回值）" };

            return new AxuTestRunPhase
            {
                Mode = mode,
                Status = outcome.Status,
                Summary = outcome.Summary,
                ReportPath = outcome.ReportPath,
                DurationMs = outcome.DurationMs,
                AllPassed = outcome.AllPassed,
                Error = outcome.Error,
            };
        }

        /// <summary>从状态文件模型投影（异步段由轮询方读状态文件时使用）。</summary>
        public static AxuTestRunPhase FromStatus(string mode, AxuTestRunStatus status)
        {
            if (status == null)
                return new AxuTestRunPhase { Mode = mode, Status = StatusError, Error = "（无状态文件）" };

            var completed = string.Equals(status.Status, AxuTestRunStatus.Completed, StringComparison.OrdinalIgnoreCase);
            var failed = string.Equals(status.Status, AxuTestRunStatus.Error, StringComparison.OrdinalIgnoreCase);
            return new AxuTestRunPhase
            {
                Mode = mode,
                Status = completed ? StatusCompleted : (failed ? StatusError : StatusRunning),
                Summary = status.Summary,
                ReportPath = status.ReportPath,
                AllPassed = completed && status.Summary != null && status.Summary.Total > 0 &&
                            status.Summary.Failed == 0 && status.Summary.Inconclusive == 0,
                Error = failed ? (status.Message ?? "运行结束但报告流程出错，详见 Editor Console。") : null,
            };
        }

        /// <summary>
        /// <c>mode=all</c> 的续跑决策：EditMode 段正常完成（completed）才继续 PlayMode 段。
        /// 超时/错误时编辑器多半已处于卡顿或异常状态，再进 Play 只会把故障放大并掩盖第一段的问题——
        /// 此时停止并明确报告"已跳过 PlayMode 段"。
        /// </summary>
        public static bool ShouldStartPlayModePhase(AxuTestRunPhase editorPhase)
        {
            return editorPhase != null
                   && string.Equals(editorPhase.Status, StatusCompleted, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// <c>mode=all</c> 的整体结论（一句话中文）：两段各自的结果 + 是否跳过/仍在进行。
        /// 任一阶段失败或超时都不会被另一阶段的状态掩盖。
        /// </summary>
        public static string BuildAllModeConclusion(AxuTestRunPhase editorPhase, bool playModeStarted)
        {
            var editorText = "EditMode 段：" + Describe(editorPhase);
            return playModeStarted
                ? $"{editorText}；PlayMode 段已启动（异步进行中，经 axu_test_status 取其终值）——整体结论待两段都结束。"
                : $"{editorText}；PlayMode 段已跳过（EditMode 段未正常完成，继续只会叠加故障）——本次 mode=all 未完成。";
        }

        /// <summary>单段结果的一句话描述（中文，供结论与看板复用）。</summary>
        public static string Describe(AxuTestRunPhase phase)
        {
            if (phase == null)
                return "未知（无阶段数据）";

            switch ((phase.Status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case StatusCompleted:
                    var summary = phase.Summary;
                    if (summary == null)
                        return "已完成（无汇总数据）";
                    return summary.Failed == 0
                        ? $"全部通过（{summary.Passed}/{summary.Total}）"
                        : $"有失败（{summary.Passed}/{summary.Total} 通过，{summary.Failed} 失败）";
                case StatusRunning:
                    return "进行中";
                case StatusTimeout:
                    return "等待完成超时" + (string.IsNullOrEmpty(phase.Error) ? "" : $"（{phase.Error}）");
                default:
                    return "出错" + (string.IsNullOrEmpty(phase.Error) ? "" : $"（{phase.Error}）");
            }
        }

        /// <summary>耗时的人类可读形态（毫秒 → 秒/分钟；无耗时为 null）。</summary>
        public static string FormatDuration(double durationMs)
        {
            if (durationMs <= 0)
                return null;
            var seconds = durationMs / 1000.0;
            return seconds >= 60
                ? (seconds / 60.0).ToString("0.#", CultureInfo.InvariantCulture) + " 分钟"
                : seconds.ToString("0.#", CultureInfo.InvariantCulture) + " 秒";
        }
    }
}
