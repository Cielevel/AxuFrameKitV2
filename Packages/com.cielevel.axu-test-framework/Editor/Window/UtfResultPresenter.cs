using System;

namespace AxuTestFramework.Editor
{
    /// <summary>UTF 用例的首屏说明；保留原始 Message/StackTrace 供详情展开。</summary>
    public static class UtfResultPresenter
    {
        public static string Summary(AxuTestReport report, bool historical)
        {
            if (report?.Summary == null)
                return "单元测试：尚无可用结果。";
            var count = report.Summary;
            var verdict = count.Total == 0 ? "没有执行测试"
                : count.Failed > 0 || count.Inconclusive > 0 ? "有失败或未确定项"
                : count.Skipped > 0 ? "已完成（有跳过项）" : "本次范围全部通过";
            var scope = report.Mode == "playmode" ? "PlayMode" : report.Mode == "editor" ? "EditMode" : report.Mode;
            var filter = FilterScope(report.Filter);
            var consoleErrors = report.Console?.ErrorCount ?? 0;
            return $"单元测试 · {scope} · {filter} · {verdict}" +
                   (historical ? "（历史报告）" : string.Empty) + "\n" +
                   $"{count.Passed}/{count.Total} 通过 · {count.Failed} 失败 · " +
                   $"{count.Inconclusive} 未确定 · {count.Skipped} 跳过" +
                   (consoleErrors > 0
                       ? $"\nConsole 错误日志 {consoleErrors} 条（可能含测试预期日志，请查看报告确认）"
                       : string.Empty);
        }

        private static string FilterScope(FilterInfo filter)
        {
            if (filter == null || string.IsNullOrEmpty(filter.Value))
                return "当前模式的全部用例";
            switch (filter.Type)
            {
                case "category": return "类别 " + filter.Value;
                case "assembly": return "程序集 " + filter.Value;
                case "testName": return "名称含 " + filter.Value;
                case "testNames": return "选定用例 " + filter.Value;
                default: return filter.Value;
            }
        }

        public static string IssueText(ReportResult result)
        {
            if (result == null)
                return string.Empty;
            var status = result.Status ?? string.Empty;
            if (string.IsNullOrWhiteSpace(status))
                return "结果状态缺失；请检查原始报告。";
            if (status.StartsWith("Passed", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (status.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase))
                return "此用例已跳过；检查跳过原因后再决定是否启用。";
            var message = FirstLine(result.Message);
            var reason = status.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0
                ? "执行时发生错误" : status.IndexOf("Inconclusive", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "结果未确定" : "用例未通过";
            return string.IsNullOrEmpty(message) ? reason : reason + "：" + message;
        }

        public static string Advice(ReportResult result)
        {
            var message = result?.Message ?? string.Empty;
            if (message.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0)
                return "检查空引用对象的初始化与生命周期，再查看下方堆栈定位代码行。";
            if (message.IndexOf("Expected", StringComparison.OrdinalIgnoreCase) >= 0)
                return "对照预期值与实际值，检查输入数据和断言条件。";
            if (message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return "检查等待条件、异步任务与超时设置。";
            return "展开技术详情查看原始消息和堆栈，再定位对应测试。";
        }

        private static string FirstLine(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;
            var line = message.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
            return line.Length <= 160 ? line : line.Substring(0, 157) + "…";
        }
    }
}
