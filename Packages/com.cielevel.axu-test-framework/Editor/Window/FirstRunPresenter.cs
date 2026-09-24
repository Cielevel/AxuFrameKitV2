using System.Collections.Generic;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 首次运行引导的纯文案映射（不碰 Unity API，单测直测）：
    /// <see cref="RunReadiness"/> → 运行按钮文案/悬浮说明/就绪横幅；
    /// <see cref="AxuScenarioRunStatus"/> → 自检结果一句话。
    /// <para>文案面向不了解本项目的用户：不出现内部术语（runId/桥文件等）；
    /// 未就绪的按钮文案不出现运行类动词，避免"按钮写着运行却点了没反应"的体验；
    /// 自检结论必须自带限定（只证明工具链可用，不代表项目质量）。</para>
    /// </summary>
    public static class FirstRunPresenter
    {
        /// <summary>
        /// 自检免责说明（引导区常驻小字）：只验证工具链本身可用，不代表项目功能正常、
        /// 也不代表项目全部测试通过。
        /// </summary>
        public static string SelfCheckDisclaimer { get; } =
            "示例自检只验证 AxuTestFramework 测试工具链本身能否在本工程正常运行（使用内置示例内容，不依赖项目场景与代码）；" +
            "结果不代表项目功能正常，也不代表项目的单元测试与场景测试全部通过。";

        /// <summary>
        /// 运行按钮文案：就绪时含"运行示例测试"；未就绪不得出现运行类动词（置灰由 UI 决定，文案先行收敛）。
        /// </summary>
        public static string RunButtonLabel(RunReadiness readiness)
        {
            return readiness != null && readiness.IsReady ? "运行示例测试" : "尚未就绪";
        }

        /// <summary>
        /// 运行按钮悬浮说明：未就绪时合并各阻断项的影响说明与修复指引；就绪时给操作说明。
        /// </summary>
        public static string RunButtonTooltip(RunReadiness readiness)
        {
            if (readiness == null || readiness.IsReady)
            {
                return "点击后自动进入 Play Mode 运行内置示例套件，结束后自动退出并在这里显示结果；" +
                       "运行期间请勿手动停止 Play（会被记为中断）。";
            }

            var lines = new List<string>();
            foreach (var issue in readiness.Issues)
            {
                if (issue == null || !issue.IsBlocking)
                    continue;
                lines.Add(string.IsNullOrEmpty(issue.FixHint)
                    ? "· " + issue.Message
                    : "· " + issue.Message + "\n  " + issue.FixHint);
            }

            return lines.Count > 0 ? string.Join("\n", lines.ToArray()) : "当前不满足运行条件。";
        }

        /// <summary>
        /// 就绪横幅文案：就绪返回 null（UI 据此隐藏横幅）；未就绪时逐条列出阻断项（含修复指引），
        /// 非阻断提示（如编译中）附在末尾。
        /// </summary>
        public static string ReadinessBannerText(RunReadiness readiness)
        {
            if (readiness == null || readiness.IsReady)
                return null;

            var lines = new List<string>();
            foreach (var issue in readiness.Issues)
            {
                if (issue == null)
                    continue;

                if (issue.IsBlocking)
                {
                    lines.Add("· " + issue.Message +
                              (string.IsNullOrEmpty(issue.FixHint) ? "" : "\n  修复：" + issue.FixHint));
                }
                else
                {
                    lines.Add("· 提示：" + issue.Message);
                }
            }

            return string.Join("\n", lines.ToArray());
        }

        /// <summary>
        /// 自检结果一句话（总览/引导区展示）：
        /// completed 且全过 → 含"测试工具链可用"并必须带"不代表项目全部测试通过"一类限定；
        /// completed 有失败 → 不出现"可用/通过"语义，说明失败更可能来自被测内容；
        /// aborted → 已中断说明；error/其他 → 错误说明；null → 未运行提示。
        /// </summary>
        public static string DescribeSelfCheckOutcome(AxuScenarioRunStatus status)
        {
            if (status == null)
                return "示例自检还没有运行过——点击「运行示例测试」发起一次，结果会显示在这里。";

            if (status.IsRunning)
                return "示例自检正在运行（结束后会自动退出 Play Mode 并显示结果）。";

            if (status.IsCompleted)
            {
                if (!ScenarioRunStatusPresenter.IsFailureVerdict(status))
                {
                    var warned = status.Summary != null && status.Summary.Warned > 0;
                    return "示例自检完成：测试工具链可用。" +
                           (warned ? "（结果带警告项，不影响本次结论）" : "") +
                           "注意：这只说明工具链本身能正常工作，不代表项目功能正常，也不代表项目全部测试通过。";
                }

                return "示例自检运行完成，但结果中有失败项：测试流程本身能执行，失败更可能来自被测内容或工程环境。" +
                       "请展开结果查看失败详情；若确认与被测内容无关，可重启 Unity 后再试一次。";
            }

            switch ((status.Status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case AxuScenarioRunStatus.Aborted:
                    return "示例自检被中断（例如手动停止了 Play Mode），本次没有产生结果，可重新发起。";
                case AxuScenarioRunStatus.Error:
                    return "示例自检未能正常结束（框架内部出错）：请查看 Console 窗口的报错信息，重启 Unity 后重试。";
                default:
                    return "示例自检状态异常（" + status.Status + "），可重新发起一次。";
            }
        }
    }
}
