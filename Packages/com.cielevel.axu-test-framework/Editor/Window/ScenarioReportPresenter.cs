using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Budget;
using AxuTestFramework.Core.Report;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Scenario 报告展示辅助（纯函数、可单测）：看板 Scenario 页详情展开用的
    /// 工件路径解析 / 指标数值格式 / findings 人话翻译 / 一句话结论 / 判定色调映射。
    /// </summary>
    public static class ScenarioReportPresenter
    {
        /// <summary>
        /// 报告 artifacts[] 条目（相对报告目录，见 AgentRunnerHost.FormatArtifactRelative）→ 完整物理路径；
        /// 已是绝对路径时原样返回；输入为空返回 null。
        /// </summary>
        public static string ResolveArtifactPath(string reportFullPath, string artifactPath)
        {
            if (string.IsNullOrEmpty(artifactPath))
                return null;

            var normalized = artifactPath.Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
                return Path.GetFullPath(normalized.Replace('/', Path.DirectorySeparatorChar));

            var reportDir = string.IsNullOrEmpty(reportFullPath) ? null : Path.GetDirectoryName(reportFullPath);
            if (string.IsNullOrEmpty(reportDir))
                return normalized;

            return Path.GetFullPath(Path.Combine(reportDir, normalized.Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>是否可作缩略图展示的图片工件（png/jpg/jpeg，大小写不敏感）。</summary>
        public static bool IsImageArtifact(string artifactPath)
        {
            var extension = Path.GetExtension((artifactPath ?? string.Empty).Trim()).ToLowerInvariant();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }

        /// <summary>指标数值的紧凑文本（最多 3 位小数；NaN/Infinity 原样输出）。</summary>
        public static string FormatMetricValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>finding code → 人话短句；未知 code 回退原始 message（再回退 code）。</summary>
        public static string TranslateFinding(Finding finding)
        {
            if (finding == null)
                return string.Empty;

            var code = (finding.Code ?? string.Empty).Trim().ToUpperInvariant();
            switch (code)
            {
                case "CAPABILITY_MISSING": return "能力未注册（suite 引用了未注册的 capability）";
                case "RUNTIME_TIMEOUT": return "运行超时（超出 timeoutSec）";
                case "RUNTIME_EXCEPTION": return "运行中抛出异常";
                case "BUDGET_METRIC_MISSING": return "预算指标缺失（该指标未产出）";
                case "BUDGET_FAIL": return "预算未达标";
                case "BUDGET_WARN": return "预算接近阈值";
                case "BASELINE_REGRESSION": return "指标较基线显著劣化";
                case "BASELINE_DRIFT": return "指标较基线轻微劣化";
                case "BASELINE_LOAD_FAILED": return "基线加载失败（本次未对比）";
                case "COLLECTOR_UNAVAILABLE": return "采集器不可用";
                case "COLLECTOR_UNKNOWN": return "未知采集器（已忽略）";
                case "SOAK_MEMORY_LEAK_SUSPECT": return "疑似内存泄漏（斜率超阈值）";
                case "SOAK_FPS_DECAY": return "帧率随浸泡时间衰减";
                case "SOAK_FPS_DECAY_SKIPPED": return "帧率衰减判定被跳过（样本不足）";
                case "SOAK_PARTIAL_FLUSH_SKIPPED": return "中间报告定期落盘被跳过";
                case "SOAK_CSV_WRITE_FAILED": return "浸泡 CSV 写盘失败";
                case "SCENE_NOT_STABLE": return "场景未达稳定窗口";
                case "LOAD_FAILED":
                case "PERF_SCENE_LOAD_FAILED":
                case "MONKEY_SCENE_LOAD_FAILED": return "场景加载失败";
                case "SMOKE_NO_SCENES": return "冒烟清单为空";
                case "SMOKE_DIRECTORY_UNAVAILABLE": return "冒烟目录不可用";
                case "LOADTIME_NO_SCENES": return "加载时长场景矩阵为空";
                case "STRESS_NO_PHASES": return "未配置压力阶段";
                case "STRESS_UNKNOWN_TOOL": return "压力测试引用了未知工具";
                default:
                    if (code.StartsWith("SOAK_", StringComparison.Ordinal)) return "浸泡运行提示";
                    if (code.StartsWith("STRESS_", StringComparison.Ordinal)) return "压力测试提示";
                    if (code.StartsWith("SMOKE_", StringComparison.Ordinal)) return "冒烟运行提示";
                    return string.IsNullOrEmpty(finding.Message) ? code : finding.Message;
            }
        }

        /// <summary>给一条问题提供可执行的检查方向；未知代码保留原始信息供技术详情查看。</summary>
        public static string FindingAdvice(Finding finding)
        {
            var code = (finding?.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.StartsWith("BUDGET_", StringComparison.Ordinal))
                return "检查指标实际值、预算门槛和采集器配置。";
            if (code.StartsWith("BASELINE_", StringComparison.Ordinal))
                return "检查本次与基线的运行环境及指标变化。";
            if (code.StartsWith("SOAK_", StringComparison.Ordinal))
                return "查看采样曲线与中间报告，确认是否持续劣化。";
            if (code.StartsWith("SMOKE_", StringComparison.Ordinal) || code.Contains("SCENE") || code == "LOAD_FAILED")
                return "检查场景清单、构建配置和加载时的 Console 错误。";
            if (code == "RUNTIME_TIMEOUT")
                return "检查耗时操作与 timeoutSec 设置。";
            if (code == "RUNTIME_EXCEPTION")
                return "展开技术详情查看原始异常，再定位抛出位置。";
            if (code == "CAPABILITY_MISSING")
                return "检查能力注册、程序集引用和 suite 中的 capability 名称。";
            return "展开技术详情查看原始消息、指标和日志。";
        }

        /// <summary>优先选错误，再选警告；不依赖 findings 的原始列表顺序。</summary>
        public static Finding PrimaryIssue(ScenarioReportResult result)
        {
            if (result?.Findings == null)
                return null;
            Finding warning = null;
            foreach (var finding in result.Findings)
            {
                if (finding == null) continue;
                if (finding.Severity == FindingSeverity.Error) return finding;
                if (warning == null && finding.Severity == FindingSeverity.Warning) warning = finding;
            }
            return warning;
        }

        /// <summary>首屏下一步：只消费本轮状态对应的报告；无报告时不猜历史结果。</summary>
        public static string NextStep(AxuScenarioRunStatus status, ScenarioReport report)
        {
            if (status == null)
                return report == null ? "下一步：运行一个场景套件；单元测试结果请到「单元测试」页查看。"
                    : "这是历史场景报告。下一步：运行套件以取得本次结论。";
            if (status.IsRunning)
                return status.IsStale(DateTime.UtcNow)
                    ? "下一步：检查 Editor 与 Agent 状态；这次运行已超过心跳时限。"
                    : "下一步：等待运行结束；结果会自动回显。";
            if (!status.IsCompleted)
                return "下一步：" + ScenarioRunStatusPresenter.ReasonText(status) + " 修复后重新运行。";
            if (report == null)
                return "本次运行已结束，但报告不可读；检查报告路径、磁盘权限及 Console。";
            if (IsFailure(status, report))
            {
                if (ScenarioRunStatusPresenter.IsWarnAsFailVerdict(status) && report.Results != null)
                {
                    foreach (var result in report.Results)
                    {
                        if (result == null || result.Status != "warn") continue;
                        var warning = PrimaryIssue(result);
                        if (warning != null)
                            return $"警告按失败处理。优先查看 {result.ScenarioId}：{TranslateFinding(warning)}。{FindingAdvice(warning)}";
                    }
                    return "警告按失败处理；展开报告检查警告场景与 warnAsFail 设置。";
                }
                if (report?.Results != null)
                {
                    foreach (var result in report.Results)
                    {
                        if (result == null || (result.Status != "fail" && result.Status != "error")) continue;
                        var issue = PrimaryIssue(result);
                        if (issue != null)
                            return $"优先处理 {result.ScenarioId}：{TranslateFinding(issue)}。{FindingAdvice(issue)}";
                        return $"优先检查 {result.ScenarioId}：展开技术详情查看原始消息与运行日志。";
                    }
                }
                return "下一步：打开本次报告，检查失败场景及原始错误。";
            }
            if (string.Equals(status.SuiteId, FirstRunSuiteCatalog.SelfCheckSuiteId, StringComparison.Ordinal))
                return "本次仅验证测试工具链可用。下一步：运行代码单元测试或项目场景套件；这不是项目通过结论。";
            if (status.Summary != null && status.Summary.Warned > 0)
            {
                if (report.Results != null)
                {
                    foreach (var result in report.Results)
                    {
                        if (result == null || result.Status != "warn") continue;
                        var issue = PrimaryIssue(result);
                        if (issue != null)
                            return $"优先查看 {result.ScenarioId}：{TranslateFinding(issue)}。{FindingAdvice(issue)}";
                    }
                }
                return "下一步：查看警告场景，确认是否需要调整预算或修复问题。";
            }
            if (report?.Results != null)
            {
                foreach (var result in report.Results)
                    if (result != null && result.Unbudgeted)
                        return "本次场景通过，但有项目未设预算；需要性能门槛时请补充预算。";
            }
            return "下一步：可查看场景详情或运行其他套件；此结论只覆盖本次场景测试。";
        }

        private static bool IsFailure(AxuScenarioRunStatus status, ScenarioReport report)
        {
            return ScenarioRunStatusPresenter.IsFailureVerdict(status) ||
                   report?.ExitCode == ScenarioExitCode.Failures ||
                   report?.ExitCode == ScenarioExitCode.InfrastructureError;
        }

        /// <summary>
        /// 一句话结论：状态 + 预算/基线/错误/警告计数 + 首要问题（findings 人话翻译）。
        /// 全部通过且无任何 warning/fail 时输出"全部通过，无异常。"。
        /// </summary>
        public static string BuildConclusion(ScenarioReportResult result)
        {
            if (result == null)
                return string.Empty;

            var status = (result.Status ?? string.Empty).Trim().ToLowerInvariant();

            var errors = 0;
            var warnings = 0;
            var primary = PrimaryIssue(result);
            if (result.Findings != null)
            {
                foreach (var finding in result.Findings)
                {
                    if (finding == null)
                        continue;
                    if (finding.Severity == FindingSeverity.Error)
                    {
                        errors++;
                    }
                    else if (finding.Severity == FindingSeverity.Warning)
                    {
                        warnings++;
                    }
                }
            }

            var budgetFail = CountBudget(result.Budgets, "fail");
            var budgetWarn = CountBudget(result.Budgets, "warn");
            var baselineFail = CountBaseline(result.BaselineDiff, "fail");
            var baselineWarn = CountBaseline(result.BaselineDiff, "warn");

            if (status == "pass" && !result.Unbudgeted && errors == 0 && warnings == 0 &&
                budgetFail == 0 && budgetWarn == 0 && baselineFail == 0 && baselineWarn == 0)
            {
                return "结论：全部通过，无异常。";
            }

            var parts = new List<string>
            {
                status == "fail" || status == "error" ? "未通过"
                    : status == "warn" ? "通过但有警告" : "通过"
            };
            if (budgetFail > 0) parts.Add($"预算未达标 {budgetFail} 项");
            if (baselineFail > 0) parts.Add($"较基线劣化超阈值 {baselineFail} 项");
            if (errors > 0) parts.Add($"错误 {errors} 项");
            if (warnings > 0) parts.Add($"警告 {warnings} 项");
            if (budgetWarn > 0) parts.Add($"预算接近阈值 {budgetWarn} 项");
            if (baselineWarn > 0) parts.Add($"较基线轻微劣化 {baselineWarn} 项");
            if (result.Unbudgeted) parts.Add("未设预算");

            var text = "结论：" + string.Join(" · ", parts);
            if (primary != null)
                text += "；首要问题：" + TranslateFinding(primary);
            return text + "。";
        }

        /// <summary>判定串 → USS 色调类（pass=绿 / warn=黄 / fail|error=红 / 其余=灰）。</summary>
        public static string ToneClass(string verdict)
        {
            switch ((verdict ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "pass":
                case "ok":
                    return "ax-tone-ok";
                case "warn":
                case "warning":
                    return "ax-tone-warn";
                case "fail":
                case "error":
                    return "ax-tone-bad";
                default:
                    return "ax-tone-neutral";
            }
        }

        static int CountBudget(IReadOnlyList<BudgetOutcome> outcomes, string verdict)
        {
            var count = 0;
            if (outcomes == null)
                return 0;
            foreach (var outcome in outcomes)
            {
                if (outcome != null && string.Equals(outcome.Verdict, verdict, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        static int CountBaseline(IReadOnlyList<BaselineDiffEntry> entries, string verdict)
        {
            var count = 0;
            if (entries == null)
                return 0;
            foreach (var entry in entries)
            {
                if (entry != null && string.Equals(entry.Verdict, verdict, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }
    }
}
