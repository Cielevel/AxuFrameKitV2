using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Unity Pipeline 本地 API 自定义命令（[CliCommand] 经 TypeCache 自动注册，pipeline 包零改动）：
    /// <c>axu_test_run</c>（强制安全程序集过滤的测试运行）、<c>axu_test_report</c>（读报告 JSON 原文）、
    /// <c>axu_test_status</c>（读运行状态文件，PlayMode 异步轮询用）。
    /// 命令参数名保持 snake_case，与 pipeline 内置命令风格并列（示例见包 README）。
    /// </summary>
    public static class AxuTestPipelineCommands
    {
        /// <summary>
        /// 运行 Unity 测试并收结构化报告。assemblyNames 恒为安全清单（剔除 Unity.Pipeline.Tests*，防 token 中毒），
        /// 用户的 assembly/category/testName 过滤与其取交集；PlayMode 强制异步（返回 running 后轮询 axu_test_status），
        /// EditMode 同步等待后直接返回汇总与 reportPath。
        /// </summary>
        [CliCommand("axu_test_run", "Run Unity tests with enforced safe assembly filtering (excludes Unity.Pipeline.Tests*) and JSON report output. PlayMode is always async.", MainThreadRequired = true, Tags = new[] { "tests", "axu" })]
        public static async Task<object> RunTests(
            [CliArg("mode", "Test mode: editor, playmode, all (default: editor; all = EditMode sync then PlayMode async)")] string mode = "editor",
            [CliArg("filter", "Filter value; with filter_type=testName it is a case-insensitive substring match on test full name")] string filter = "",
            [CliArg("filter_type", "Filter type: testName, assembly, category (default: testName)")] string filter_type = "testName",
            [CliArg("category", "Category filter shortcut (equals filter_type=category); comma-separated for multiple")] string category = "",
            [CliArg("assembly", "Assembly filter shortcut (equals filter_type=assembly); partial match against the safe assembly list")] string assembly = "",
            [CliArg("clear_console", "Clear the Editor Console window before the run (default true)")] bool clear_console = true,
            [CliArg("report", "Write the JSON report (schema axu-test-report/1) to Library/AxuTestReports (default true)")] bool report = true,
            [CliArg("out_path", "Optional report file path override (relative to project root or absolute; ending with .json = file, otherwise = directory)")] string out_path = "",
            [CliArg("timeout_ms", "EditMode completion wait budget in milliseconds (default 120000); keep the HTTP request timeout above it")] int timeout_ms = 120000)
        {
            var request = new AxuTestRunRequest
            {
                Mode = mode,
                Filter = filter,
                FilterType = filter_type,
                Category = category,
                Assembly = assembly,
                ClearConsole = clear_console,
                Report = report,
                OutPath = out_path,
                TimeoutMs = timeout_ms
            };

            var outcome = await AxuTestRunService.StartRunAsync(request);
            return outcome;
        }

        /// <summary>
        /// 读取一份测试报告的 JSON 原文。默认返回最近一次的报告；指定 path 时按路径读取。
        /// 超过 256KB 时拒绝返回原文（给出 too_large 信封与文件路径，避免超大响应拖垮调用方）。
        /// </summary>
        [CliCommand("axu_test_report", "Read an AxuTestFramework test report JSON (latest by default, or by path)", MainThreadRequired = false, Tags = new[] { "tests", "axu" })]
        public static object GetReport(
            [CliArg("latest", "Return the most recent report (LastReportPath if present, else newest in Library/AxuTestReports). Default true")] bool latest = true,
            [CliArg("path", "Explicit report file path (relative to project root or absolute); overrides latest")] string path = "")
        {
            try
            {
                var fullPath = ResolveReportPath(latest, path);
                if (fullPath == null)
                {
                    return new { status = "not_found", message = "未找到报告文件：尚未运行过测试，且 Library/AxuTestReports 下没有历史报告。" };
                }

                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                {
                    return new { status = "not_found", path = fullPath, message = "报告文件不存在（可能已被清理）。" };
                }

                if (fileInfo.Length > AxuTestReportPaths.ReportReadLimitBytes)
                {
                    return new
                    {
                        status = "too_large",
                        path = fullPath,
                        sizeBytes = fileInfo.Length,
                        limitBytes = AxuTestReportPaths.ReportReadLimitBytes,
                        message = "报告超过读取上限，请直接读文件或用 out_path/自选工具分片查看。"
                    };
                }

                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                return new { status = "error", message = ex.Message };
            }
        }

        /// <summary>
        /// 查询 axu_test_run 的运行状态（读 Temp/axu_test_run_status.json 原文）。
        /// PlayMode 异步运行的标准轮询入口：status 由 running 变为 completed/error 即终态；
        /// startedAtUtc 可用于判断陈旧状态。
        /// </summary>
        [CliCommand("axu_test_status", "Get axu_test_run status (reads Temp/axu_test_run_status.json; poll this for async PlayMode runs)", MainThreadRequired = false, Tags = new[] { "tests", "axu" })]
        public static string GetRunStatus()
        {
            try
            {
                var fullPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                if (File.Exists(fullPath))
                    return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                return "{\"status\":\"error\",\"message\":" + JsonEscape(ex.Message) + "}";
            }
            return "{\"status\":\"idle\",\"message\":\"No axu test run in progress (no status file).\"}";
        }

        /// <summary>解析报告读取目标：显式 path 优先；否则 LastReportPath；否则报告目录里最新的 .json。</summary>
        private static string ResolveReportPath(bool latest, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return AxuTestReportPaths.ToFullPath(path);

            if (AxuTestReportWriter.LastReportPath != null && File.Exists(AxuTestReportWriter.LastReportPath))
                return AxuTestReportWriter.LastReportPath;

            if (!latest)
                return null;

            var directory = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ReportsDirectory);
            if (!Directory.Exists(directory))
                return null;

            var newest = new DirectoryInfo(directory)
                .EnumerateFiles("*.json")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            return newest?.FullName;
        }

        /// <summary>极简 JSON 字符串转义（状态命令兜底路径用，避免为主流程引入额外依赖面）。</summary>
        private static string JsonEscape(string text)
        {
            if (text == null)
                return "\"\"";
            var builder = new StringBuilder(text.Length + 8).Append('"');
            foreach (var c in text)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (char.IsControl(c))
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            builder.Append(c);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }
    }
}
