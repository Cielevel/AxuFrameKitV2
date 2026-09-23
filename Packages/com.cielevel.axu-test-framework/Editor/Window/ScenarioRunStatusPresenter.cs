using System;
using System.IO;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 运行状态（<see cref="AxuScenarioRunStatus"/>）→ 界面文案/色调的纯函数映射。
    /// 让"未运行 / 正在运行 / 已中断 / 基础设施错误 / 通过 / 有警告 / 有失败"在总览页与场景套件页
    /// 有同一套稳定且不互相混淆的表述（阶段 3 的信息分层在此基础上做细化）。
    /// </summary>
    public static class ScenarioRunStatusPresenter
    {
        /// <summary>状态短标签（含符号），如"✓ 全部通过"。</summary>
        public static string Label(AxuScenarioRunStatus status)
        {
            if (status == null)
                return "— 尚无运行记录";

            if (status.IsRunning)
                return status.IsStale(DateTime.UtcNow) ? "… 运行状态已超时" : "… 正在运行";

            switch ((status.Status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case AxuScenarioRunStatus.Completed:
                    var summary = status.Summary;
                    if (summary == null)
                        return "✓ 运行完成";
                    if (summary.Failed > 0)
                        return "✗ 有失败";
                    return summary.Warned > 0 ? "⚠ 通过（有警告）" : "✓ 全部通过";
                case AxuScenarioRunStatus.Aborted:
                    return "■ 已中断";
                case AxuScenarioRunStatus.Error:
                    return "✗ 运行失败";
                default:
                    return $"[{status.Status}]";
            }
        }

        /// <summary>USS 色调类（ok/warn/bad/neutral）：中断与基础设施错误都是"没有结果"，用 bad 色。</summary>
        public static string ToneClass(AxuScenarioRunStatus status)
        {
            if (status == null || status.IsRunning)
                return "ax-tone-neutral";

            switch ((status.Status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case AxuScenarioRunStatus.Completed:
                    var summary = status.Summary;
                    if (summary == null)
                        return "ax-tone-neutral";
                    if (summary.Failed > 0)
                        return "ax-tone-bad";
                    return summary.Warned > 0 ? "ax-tone-warn" : "ax-tone-ok";
                case AxuScenarioRunStatus.Aborted:
                case AxuScenarioRunStatus.Error:
                    return "ax-tone-bad";
                default:
                    return "ax-tone-neutral";
            }
        }

        /// <summary>总览徽章样式类（与 <see cref="ToneClass"/> 同判定，映射到 ax-badge-* 家族）。</summary>
        public static string BadgeClass(AxuScenarioRunStatus status)
        {
            switch (ToneClass(status))
            {
                case "ax-tone-ok": return "ax-badge-ok";
                case "ax-tone-warn": return "ax-badge-warn";
                case "ax-tone-bad": return "ax-badge-bad";
                default: return "ax-badge-neutral";
            }
        }

        /// <summary>
        /// 未完成（中断/基础设施错误）的原因说明：优先用 Agent/看门狗写入的 message，
        /// 缺失时按 reason 码给出可辨识的中文（suite 加载失败 / 报告写盘失败 / 被中断 / Agent 未接手）。
        /// </summary>
        public static string ReasonText(AxuScenarioRunStatus status)
        {
            if (status == null)
                return "（无状态）";

            if (!string.IsNullOrWhiteSpace(status.Message))
                return status.Message;

            switch ((status.Reason ?? string.Empty).Trim().ToLowerInvariant())
            {
                case AxuScenarioRunStatus.ReasonSuiteLoadFailed:
                    return "suite/scenario 文件加载或解析失败（文件内容或引用的 scenario 有问题）。";
                case AxuScenarioRunStatus.ReasonReportWriteFailed:
                    return "报告写盘失败（磁盘/权限问题，测试本体可能已跑完）。";
                case AxuScenarioRunStatus.ReasonInterrupted:
                    return "Play Mode 在运行结束前退出：可能是在 Unity 里手动点了停止，或编辑器被中断。";
                case AxuScenarioRunStatus.ReasonAgentNotRun:
                    return "Agent 未接手这次运行（AXU_TEST_AGENT 未开启或未编译完成），没有产生任何结果。";
                default:
                    return "运行未正常结束，未产生本次结果。";
            }
        }

        /// <summary>
        /// 一句话状态说明（总览页详情行用）：范围（suite）+ 时间 + 计数 + 未完成原因。
        /// </summary>
        public static string DetailText(AxuScenarioRunStatus status, string displayName)
        {
            if (status == null)
                return "还没有场景套件的运行记录——在「场景套件」页选中套件并运行，结果会汇总到这里。";

            var scope = string.IsNullOrEmpty(displayName) ? status.SuiteId : displayName;
            var when = FormatLocalTime(status.EndedAtUtc ?? status.StartedAtUtc);

            if (status.IsRunning)
                return $"{scope} 正在运行（第 {status.CompletedCount}/{status.TotalCount} 个场景），" +
                       "跑完会自动退出 Play 并在这里给出结论。";

            var summary = status.Summary;
            if (status.IsCompleted && summary != null)
            {
                return $"{scope} · {when} · {summary.Total} 项 · {summary.Passed} 通过 · " +
                       $"{summary.Warned} 警告 · {summary.Failed} 失败";
            }

            return $"{scope} · {when} · {ReasonText(status)}";
        }

        /// <summary>运行来源的中文短句（运行记录文件里的 source）。</summary>
        public static string SourceText(string source)
        {
            switch ((source ?? string.Empty).Trim().ToLowerInvariant())
            {
                case ScenarioRunRecord.SourceDashboard: return "看板";
                case ScenarioRunRecord.SourceMenu: return "菜单";
                case ScenarioRunRecord.SourcePipeline: return "命令行";
                default: return string.IsNullOrEmpty(source) ? "未知来源" : source;
            }
        }

        static string FormatLocalTime(string utcIso)
        {
            if (string.IsNullOrEmpty(utcIso))
                return "-";
            return DateTime.TryParse(utcIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : utcIso;
        }
    }
}
