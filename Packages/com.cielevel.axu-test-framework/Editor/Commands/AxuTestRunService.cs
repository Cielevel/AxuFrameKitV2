using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// axu_test_run 的编排核心（pipeline 命令与看板窗口共用的内部服务，不经 HTTP）。
    /// <para>安全规则（硬性，源自 M0"token 中毒"事故）：构建 UTF Filter 时 <c>assemblyNames</c> 恒为
    /// <see cref="AxuTestAssemblyScanner.GetSafeTestAssemblies"/> 的清单（已剔除 Unity.Pipeline.Tests*），
    /// 绝不发出无程序集范围的裸跑；用户给定的 assembly / category / testName 过滤与安全清单取交集，
    /// "选定测试"通道（testNames 精确全名）经 UTF 的 AND 语义同样被安全清单收敛。</para>
    /// <para>编排规则：PlayMode 一律异步（启动后立即返回 running，供轮询状态文件）；EditMode 同步等待
    /// （轮询状态文件至 completed/error，步长 250ms）后返回完整结果；mode=all 先同步跑 EditMode 再异步启动
    /// PlayMode。运行参数经 Temp 上下文文件跨域重载传递给报告器（见 <see cref="AxuTestRunContext"/>）。</para>
    /// <para>线程约定：调用方必须在主线程调用 <see cref="StartRunAsync"/>；本方法首帧同步段完成所有 Unity 主线程
    /// API 调用（枚举/预检/Execute），其后的 await 沿调用方捕获的 UnitySynchronizationContext 回主线程，
    /// "all"模式的第二次 Execute 亦在主线程恢复段执行。</para>
    /// </summary>
    public static class AxuTestRunService
    {
        /// <summary>EditMode 完成等待轮询步长（毫秒）。</summary>
        public const int PollIntervalMs = 250;

        /// <summary>测试树预检超时（毫秒，RetrieveTestList 桥接等待）。</summary>
        public const int TestListTimeoutMs = 15000;

        /// <summary>running 状态文件的陈旧阈值（分钟）：上次运行异常中断留下的 running 状态超过该时长后不再视为在途。</summary>
        public const int RunningStatusStaleMinutes = 15;

        /// <summary>互斥闸：axu_test_run 命令与看板窗口之间的同进程互斥（pipeline 自带 run_tests 无法纳入本闸，
        /// 其并发由 UTF 自身单运行约束兜底，见包 README 的并发说明）。</summary>
        public static readonly object RunGate = new object();

        private static bool _runInFlight;

        /// <summary>
        /// 启动一次测试运行。必须在 Unity 主线程调用。返回的 Outcome：
        /// EditMode（与 all 的 EditMode 段）等待至完成；PlayMode 段启动即返回。
        /// </summary>
        public static async Task<AxuTestRunOutcome> StartRunAsync(AxuTestRunRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var startedUtc = DateTime.UtcNow;

            // ---- 互斥与在途检查（同步段，主线程） ----
            lock (RunGate)
            {
                if (_runInFlight || IsAxuRunInFlight())
                {
                    return ErrorOutcome(request, "已有 axu 测试运行在途（状态文件标记 running）。" +
                        "请先经 axu_test_status 观察或等其完成；running 超过 15 分钟视为陈旧会自动放行，" +
                        "确认无实际运行时可删除 Temp/axu_test_run_status.json 手动解锁。");
                }
                if (ScenarioRunService.IsScenarioRunInFlight())
                {
                    return ErrorOutcome(request, "scenario 套件运行在途（经 axu_scenario_status 观察），" +
                        "同一 Editor 禁止并发驱动 UTF 运行；确认无实际运行时可删除 Temp/axu_scenario_status.json 手动解锁。");
                }
                if (File.Exists(AxuTestReportPaths.ToFullPath("Temp/pipeline_test_request.json")))
                {
                    return ErrorOutcome(request, "pipeline 自带 run_tests 的异步运行在途（Temp/pipeline_test_request.json 存在），" +
                        "两者共用 UTF 的单运行通道，拒绝并发启动；确认无实际运行时可删除该文件手动解锁。");
                }
                _runInFlight = true;
            }

            try
            {
                // ---- 解析模式 ----
                var mode = (request.Mode ?? "editor").Trim().ToLowerInvariant();
                if (mode != "editor" && mode != "playmode" && mode != "all")
                    return ErrorOutcome(request, $"无效 mode '{request.Mode}'，可选 editor / playmode / all。");

                var firstPhase = mode == "playmode" ? TestMode.PlayMode : TestMode.EditMode;
                var requestedMode = mode == "all" ? "editor" : mode;

                // ---- 安全程序集清单与用户过滤求交 ----
                var safeAssemblies = AxuTestAssemblyScanner.GetSafeTestAssemblies(out var excluded);
                var effectiveAssemblies = ResolveAssemblies(safeAssemblies, request, out var assemblyError);
                if (assemblyError != null)
                    return ErrorOutcome(request, assemblyError, safeAssemblies, excluded);

                var categoryNames = ResolveCategories(request, out var categoryError);
                if (categoryError != null)
                    return ErrorOutcome(request, categoryError, safeAssemblies, excluded);

                var groupNames = ResolveTestNamePattern(request, out var filterTypeError);
                if (filterTypeError != null)
                    return ErrorOutcome(request, filterTypeError, safeAssemblies, excluded);

                var testNames = ResolveTestNames(request, out var testNamesError);
                if (testNamesError != null)
                    return ErrorOutcome(request, testNamesError, safeAssemblies, excluded);

                // 选定通道优先：给定了精确名单时 testName 子串过滤不参与（两组全名条件叠加必 0 匹配）
                if (testNames != null)
                    groupNames = null;

                // ---- 测试树预检：既防止 0 匹配的无效运行（PlayMode 空跑要浪费一次域重载），也给出运行规模 ----
                var matched = await CountMatchedTestsAsync(firstPhase, effectiveAssemblies, categoryNames, groupNames, testNames);
                if (matched == 0)
                {
                    return ErrorOutcome(request,
                        "过滤条件未匹配到任何测试（已按安全程序集清单过滤）。请检查 filter/category/assembly/test_names 参数；" +
                        "选定运行时确认勾选用例属于当前模式（EditMode/PlayMode 树不同）。",
                        safeAssemblies, excluded, effectiveAssemblies);
                }

                // ---- 运行前准备：console / artifacts / 上下文 / 状态 ----
                if (request.ClearConsole)
                    ClearConsoleWindow();
                AxuConsoleRing.BeginRun();

                var reportDirectory = ResolveReportDirectory(request.OutPath);
                var artifactsDirectory = Path.Combine(reportDirectory, "artifacts");

                var context = new AxuTestRunContext
                {
                    Mode = requestedMode,
                    Filter = BuildFilterInfo(request, testNames),
                    Report = request.Report,
                    OutPath = request.OutPath,
                    ArtifactsDirectory = artifactsDirectory,
                    StartedAtUtc = startedUtc.ToString("o"),
                    Assemblies = effectiveAssemblies,
                    ExcludedAssemblies = excluded
                };

                WriteRunContextAndStatus(context);

                // ---- 启动运行（主线程） ----
                ExecuteRun(firstPhase, effectiveAssemblies, categoryNames, groupNames, testNames);

                if (mode == "playmode")
                {
                    return new AxuTestRunOutcome
                    {
                        Success = true,
                        Status = "playmode_running",
                        Mode = request.Mode,
                        Assemblies = effectiveAssemblies,
                        ExcludedAssemblies = excluded,
                        Filter = context.Filter,
                        StatusPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath),
                        ReportDirectory = reportDirectory,
                        Message = $"PlayMode 运行已启动（预检 {matched} 个用例，程序集: {string.Join(", ", effectiveAssemblies)}）。" +
                                  "轮询 axu_test_status 至 completed；报告将落盘并写入状态文件。"
                    };
                }

                // ---- EditMode（或 all 的 EditMode 段）：轮询状态文件等待完成 ----
                var outcome = await WaitForCompletionAsync(request, context, effectiveAssemblies, excluded, startedUtc);
                if (mode == "editor")
                    return outcome;

                // ---- all：EditMode 已完成，接着异步启动 PlayMode 段 ----
                AxuConsoleRing.BeginRun();
                context.Mode = "playmode";
                context.StartedAtUtc = DateTime.UtcNow.ToString("o");
                WriteRunContextAndStatus(context);
                ExecuteRun(TestMode.PlayMode, effectiveAssemblies, categoryNames, groupNames, testNames);

                outcome.Status = "playmode_running";
                outcome.StatusPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath);
                outcome.Message = $"EditMode 段完成: {outcome.Summary?.Passed ?? 0}/{outcome.Summary?.Total ?? 0} 通过。" +
                                  "PlayMode 段已异步启动——轮询 axu_test_status 至 completed。";
                return outcome;
            }
            catch (Exception ex)
            {
                AxuTestRunStatus.Write(new AxuTestRunStatus
                {
                    Status = AxuTestRunStatus.Error,
                    Mode = request.Mode,
                    StartedAtUtc = startedUtc.ToString("o"),
                    EndedAtUtc = DateTime.UtcNow.ToString("o"),
                    Message = ex.Message
                });
                return ErrorOutcome(request, $"运行启动/等待失败: {ex.Message}");
            }
            finally
            {
                lock (RunGate)
                {
                    _runInFlight = false;
                }
            }
        }

        /// <summary>
        /// 是否有 axu 运行在途（状态文件标记 running 且未越过 <see cref="RunningStatusStaleMinutes"/> 陈旧阈值；
        /// 异常中断留下的陈旧 running 状态不阻塞后续运行，会被新运行覆盖）。
        /// </summary>
        public static bool IsAxuRunInFlight()
        {
            var status = AxuTestRunStatus.TryRead();
            if (status == null || status.Status != AxuTestRunStatus.Running)
                return false;

            if (DateTime.TryParse(status.StartedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var startedUtc))
            {
                if (DateTime.UtcNow - startedUtc.ToUniversalTime() > TimeSpan.FromMinutes(RunningStatusStaleMinutes))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 等待当前运行完成：轮询状态文件（步长 <see cref="PollIntervalMs"/>），超时（timeoutMs）返回 timeout 结果。
        /// 仅供 PlayMode 启动方（外部轮询 axu_test_status）之外的同步等待方使用。
        /// </summary>
        public static async Task<AxuTestRunStatus> WaitUntilCompletedAsync(int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                var status = AxuTestRunStatus.TryRead();
                if (status != null && status.Status != AxuTestRunStatus.Running)
                    return status;
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);
            }
            return null;
        }

        // ------------------------------------------------------------------ 内部实现

        /// <summary>按 filter_type / assembly 便捷参数求出实际运行的程序集清单（始终 ⊆ 安全清单）。</summary>
        private static List<string> ResolveAssemblies(List<string> safeAssemblies, AxuTestRunRequest request, out string error)
        {
            error = null;
            var explicitAssembly = !string.IsNullOrWhiteSpace(request.Assembly)
                ? request.Assembly.Trim()
                : (string.Equals(request.FilterType, "assembly", StringComparison.OrdinalIgnoreCase)
                    ? (request.Filter ?? "").Trim()
                    : "");

            if (string.IsNullOrEmpty(explicitAssembly))
                return safeAssemblies;

            var matched = safeAssemblies
                .Where(a => a.IndexOf(explicitAssembly, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (matched.Count == 0)
            {
                error = $"程序集过滤 '{explicitAssembly}' 在安全测试程序集清单中无匹配。" +
                        $"可用清单: {string.Join(", ", safeAssemblies)}";
                return null;
            }
            return matched;
        }

        /// <summary>解析类别过滤（category 便捷参数优先；多个值以逗号分隔）。值按正则元字符转义后交给 UTF。</summary>
        private static string[] ResolveCategories(AxuTestRunRequest request, out string error)
        {
            error = null;
            var raw = !string.IsNullOrWhiteSpace(request.Category)
                ? request.Category
                : (string.Equals(request.FilterType, "category", StringComparison.OrdinalIgnoreCase)
                    ? (request.Filter ?? "")
                    : "");

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var categories = raw
                .Split(',')
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Select(Regex.Escape)
                .ToArray();
            if (categories.Length == 0)
            {
                error = "category 过滤值为空。";
                return null;
            }
            return categories;
        }

        /// <summary>把 testName 过滤编译为锚定的正则（groupNames 通道，UTF 侧按正则全名匹配，等价大小写敏感包含匹配）。</summary>
        private static string[] ResolveTestNamePattern(AxuTestRunRequest request, out string error)
        {
            error = null;
            var filterType = (request.FilterType ?? "testName").Trim().ToLowerInvariant();
            if (filterType != "testname")
            {
                if (filterType != "assembly" && filterType != "category")
                {
                    error = $"无效 filter_type '{request.FilterType}'，可选 testName / assembly / category。";
                    return null;
                }
                return null; // assembly/category 已由对应通道处理
            }

            var filter = (request.Filter ?? "").Trim();
            if (filter.Length == 0)
                return null;

            // (?i) 内联开关：大小写不敏感的包含匹配；正则元字符经 Escape 还原为字面量
            return new[] { "(?i)^.*" + Regex.Escape(filter) + ".*$" };
        }

        /// <summary>解析"选定测试"精确全名清单（去空白去重）。非空即启用 testNames 通道；
        /// 安全性无需在此校验——UTF Filter 中 assemblyNames（安全清单）与 testNames 为 AND 语义，
        /// 不在安全程序集内的名单自动无效（预检按 0 匹配报错）。</summary>
        private static List<string> ResolveTestNames(AxuTestRunRequest request, out string error)
        {
            error = null;
            var raw = request.TestNames;
            if (raw == null || raw.Count == 0)
                return null;

            var names = raw
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct()
                .ToList();
            if (names.Count == 0)
            {
                error = "test_names 清单全为空白，未指定任何有效用例。";
                return null;
            }
            return names;
        }

        /// <summary>枚举过滤描述（写进上下文与返回值；选定运行记录用例数，明细以报告 results 为准）。</summary>
        private static FilterInfo BuildFilterInfo(AxuTestRunRequest request, List<string> testNames = null)
        {
            if (testNames != null && testNames.Count > 0)
                return new FilterInfo { Type = "testNames", Value = $"{testNames.Count} selected tests" };

            var raw = !string.IsNullOrWhiteSpace(request.Category)
                ? request.Category
                : (string.Equals(request.FilterType, "category", StringComparison.OrdinalIgnoreCase)
                    ? (request.Filter ?? "")
                    : "");

            if (!string.IsNullOrWhiteSpace(raw))
                return new FilterInfo { Type = "category", Value = raw };

            var explicitAssembly = !string.IsNullOrWhiteSpace(request.Assembly)
                ? request.Assembly.Trim()
                : (string.Equals(request.FilterType, "assembly", StringComparison.OrdinalIgnoreCase)
                    ? (request.Filter ?? "").Trim()
                    : "");
            if (!string.IsNullOrEmpty(explicitAssembly))
                return new FilterInfo { Type = "assembly", Value = explicitAssembly };

            var filter = (request.Filter ?? "").Trim();
            return string.IsNullOrEmpty(filter)
                ? new FilterInfo { Type = "none", Value = null }
                : new FilterInfo { Type = "testName", Value = filter };
        }

        /// <summary>RetrieveTestList 桥接（主线程回调经 TCS 转 Task，带超时防挂）。</summary>
        private static async Task<int> CountMatchedTestsAsync(
            TestMode testMode, List<string> assemblies, string[] categoryNames, string[] groupNames, List<string> testNames)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var tcs = new TaskCompletionSource<ITestAdaptor>();

            api.RetrieveTestList(testMode, root => tcs.TrySetResult(root));

            // TestRunnerApi 为无状态门面（RetrieveTestList 的 TestListJob 只持有 provider 与回调，不回引实例），
            // 用后即毁防泄漏；必须在 await（ConfigureAwait(false)）之前的主线程同步段销毁。
            UnityEngine.Object.DestroyImmediate(api);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TestListTimeoutMs)).ConfigureAwait(false);
            if (completed != tcs.Task)
                return -1; // 预检超时：不阻断运行（仅失去 0 匹配保护与规模估计）

            var root = tcs.Task.Result;
            var count = 0;
            CountMatches(root, assemblies, categoryNames, groupNames, testNames, ref count);
            return count;
        }

        /// <summary>按与 UTF Filter 相同语义（AND 各组；类别含祖先继承）统计匹配的叶子用例数。</summary>
        private static void CountMatches(
            ITestAdaptor node, List<string> assemblies, string[] categoryNames, string[] groupNames,
            List<string> testNames, ref int count)
        {
            if (node == null)
                return;

            if (!node.HasChildren && !node.IsSuite)
            {
                if (IsMatch(node, assemblies, categoryNames, groupNames, testNames))
                    count++;
                return;
            }

            if (node.Children == null)
                return;
            foreach (var child in node.Children)
                CountMatches(child, assemblies, categoryNames, groupNames, testNames, ref count);
        }

        private static bool IsMatch(
            ITestAdaptor leaf, List<string> assemblies, string[] categoryNames, string[] groupNames, List<string> testNames)
        {
            var assemblyName = leaf.TypeInfo?.Assembly?.GetName()?.Name;
            if (!string.IsNullOrEmpty(assemblyName) && !assemblies.Contains(assemblyName))
                return false;

            if (categoryNames != null && categoryNames.Length > 0)
            {
                var categories = leaf.Categories ?? Array.Empty<string>();
                var matched = categoryNames.Any(c =>
                    categories.Any(cat => Regex.IsMatch(cat, c, RegexOptions.None)));
                if (!matched)
                    return false;
            }

            if (groupNames != null && groupNames.Length > 0)
            {
                if (groupNames.All(g => leaf.FullName == null || !Regex.IsMatch(leaf.FullName, g)))
                    return false;
            }

            if (testNames != null && testNames.Count > 0 &&
                (leaf.FullName == null || !testNames.Contains(leaf.FullName)))
                return false;

            return true;
        }

        /// <summary>写运行上下文与 running 状态文件（主线程同步段）。</summary>
        private static void WriteRunContextAndStatus(AxuTestRunContext context)
        {
            AxuTestRunContext.Write(context);
            AxuTestRunStatus.Write(new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Running,
                Mode = context.Mode,
                StartedAtUtc = context.StartedAtUtc,
                Assemblies = context.Assemblies
            });

            AxuCapture.ArtifactsDirectory = context.ArtifactsDirectory;
        }

        /// <summary>清空 Editor Console 窗口（经内部 LogEntries.Clear；取不到类型/方法时静默跳过——非关键路径）。</summary>
        private static void ClearConsoleWindow()
        {
            try
            {
                var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                    return;
                var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                clearMethod?.Invoke(null, null);
            }
            catch (Exception)
            {
                // 清空失败不影响测试运行
            }
        }

        /// <summary>
        /// 由 out_path 解析报告所在目录：.json 结尾视为文件（取其目录），否则视为目录本身；
        /// 空（或 null）为默认报告目录。
        /// </summary>
        private static string ResolveReportDirectory(string outPath)
        {
            if (string.IsNullOrWhiteSpace(outPath))
                return AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ReportsDirectory);

            var full = AxuTestReportPaths.ToFullPath(outPath);
            var normalized = outPath.TrimEnd().Replace('/', Path.DirectorySeparatorChar);
            return normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(full)
                : full;
        }

        /// <summary>构建 UTF Filter 并立即启动运行（主线程）。testNames 为"选定测试"精确全名通道，
        /// 与 assemblyNames（安全清单）AND 收敛；选定运行时调用方保证 groupNames 已置空。</summary>
        private static void ExecuteRun(
            TestMode testMode, List<string> assemblies, string[] categoryNames, string[] groupNames, List<string> testNames)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = testMode,
                assemblyNames = assemblies.ToArray(),
                categoryNames = categoryNames,
                groupNames = groupNames,
                testNames = testNames?.ToArray()
            };
            api.Execute(new ExecutionSettings(filter));

            // Execute 经 ScheduleJob 新建 TestJobRunner/TestJobData，不回引本实例；用后即毁防泄漏。
            UnityEngine.Object.DestroyImmediate(api);
        }

        /// <summary>EditMode 同步等待：轮询状态文件至 completed/error，超时返回 timeout 结果。</summary>
        private static async Task<AxuTestRunOutcome> WaitForCompletionAsync(
            AxuTestRunRequest request,
            AxuTestRunContext context,
            List<string> effectiveAssemblies,
            List<string> excluded,
            DateTime startedUtc)
        {
            var final = await WaitUntilCompletedAsync(request.TimeoutMs);

            var outcome = new AxuTestRunOutcome
            {
                Mode = request.Mode,
                Assemblies = effectiveAssemblies,
                ExcludedAssemblies = excluded,
                Filter = context.Filter,
                StatusPath = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath),
                ReportDirectory = ResolveReportDirectory(request.OutPath)
            };

            if (final == null)
            {
                outcome.Success = false;
                outcome.Status = "timeout";
                outcome.Error = $"等待 EditMode 运行完成超时（{request.TimeoutMs}ms）。" +
                                "请检查 Editor 是否卡在编译/弹窗，或调大 timeout_ms 后重试。";
                return outcome;
            }

            if (final.Status == AxuTestRunStatus.Error)
            {
                outcome.Success = false;
                outcome.Status = "error";
                outcome.Error = final.Message ?? "运行结束但报告流程出错，详见 Editor Console。";
                return outcome;
            }

            outcome.Success = true;
            outcome.Status = "completed";
            outcome.Summary = final.Summary;
            outcome.ReportPath = final.ReportPath;
            outcome.AllPassed = final.Summary != null &&
                                final.Summary.Total > 0 &&
                                final.Summary.Failed == 0 &&
                                final.Summary.Inconclusive == 0;
            outcome.DurationMs = (DateTime.UtcNow - startedUtc).TotalMilliseconds;
            outcome.Message = final.Summary == null
                ? "运行完成（无汇总数据）。"
                : $"{final.Summary.Passed}/{final.Summary.Total} 通过, {final.Summary.Failed} 失败, " +
                  $"{final.Summary.Skipped} 跳过" +
                  (string.IsNullOrEmpty(final.ReportPath) ? "" : $"。报告: {final.ReportPath}");
            return outcome;
        }

        /// <summary>构造失败结果（携带安全清单审计信息）。</summary>
        private static AxuTestRunOutcome ErrorOutcome(
            AxuTestRunRequest request,
            string message,
            List<string> safeAssemblies = null,
            List<string> excluded = null,
            List<string> effectiveAssemblies = null)
        {
            return new AxuTestRunOutcome
            {
                Success = false,
                Status = "error",
                Mode = request?.Mode,
                Assemblies = effectiveAssemblies ?? safeAssemblies,
                ExcludedAssemblies = excluded,
                Error = message,
                Message = message
            };
        }
    }
}
