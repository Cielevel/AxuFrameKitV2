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
            Finding primary = null;
            if (result.Findings != null)
            {
                foreach (var finding in result.Findings)
                {
                    if (finding == null)
                        continue;
                    if (finding.Severity == FindingSeverity.Error)
                    {
                        errors++;
                        if (primary == null) primary = finding;
                    }
                    else if (finding.Severity == FindingSeverity.Warning)
                    {
                        warnings++;
                        if (primary == null) primary = finding;
                    }
                }
            }

            var budgetFail = CountBudget(result.Budgets, "fail");
            var budgetWarn = CountBudget(result.Budgets, "warn");
            var baselineFail = CountBaseline(result.BaselineDiff, "fail");
            var baselineWarn = CountBaseline(result.BaselineDiff, "warn");

            if (status == "pass" && errors == 0 && warnings == 0 &&
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
