using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// scenario 词汇族 pipeline 命令（方案 §3.5，Phase 4）：
    /// <c>axu_scenario_list</c>（能力 + 工程 suite/scenario 文件枚举）、<c>axu_scenario_run</c>（Editor 内
    /// PlayMode 驱动 Agent Runner，异步启动）、<c>axu_scenario_status</c>（读运行状态文件）。
    /// <para>并发纪律：与 axu_test_run 共享 <see cref="AxuTestRunService.RunGate"/> 静态互斥闸，
    /// 禁止并发驱动同一 Editor（axu_test_run 在途 / scenario 运行在途 / pipeline 自带 run_tests 在途均拒绝启动）。</para>
    /// <para>状态协议：复用 axu_test_run 的 Temp 状态 JSON 形态（running/completed/error + reportPath，
    /// 域重载安全）——scenario 侧状态文件为 Temp/axu_scenario_status.json（Agent 侧 ScenarioStatusFile 写入，
    /// 含 completedCount/totalCount/summary/exitCode 扩展字段）。axu_scenario_run 启动后立即返回 running
    /// （进 Play 的域重载会销毁任何进程内等待者，与 axu_test_run PlayMode 同理）；完成收尾（趋势入库 +
    /// 自动退 Play）由 ScenarioRunService 的 [InitializeOnLoad] 看门狗执行。</para>
    /// </summary>
    public static class AxuScenarioPipelineCommands
    {
        /// <summary>scenario 运行在途标志（_runGate 内读写；进程内防重入，跨域重载由状态文件兜底）。</summary>
        static bool _scenarioRunBusy;

        /// <summary>Core 侧已知的内建能力清单（BuiltInCapabilities 的 Editor 可达静态描述：
        /// 注册表本体为运行时态，EditMode 下为空，故此处按包内建清单枚举；游戏自注册能力以 suite 文件为准）。</summary>
        static readonly (string Id, string Name, string Description)[] BuiltInCapabilities =
        {
            ("echo", "Echo 自检", "链路自检能力：延时后通过并产出耗时指标，用于验证 Agent/报告/预算链路"),
            ("perf", "性能基准", "场景/玩法帧率与帧时间采样（fps/frameTime collectors），配预算判定"),
            ("loadtime", "加载时长", "场景加载矩阵：逐场景进入并采集加载耗时指标"),
            ("smoke", "冒烟矩阵", "全场景可进、无报错、可截图留证（场景清单 list/buildSettings/directory）"),
        };

        // ------------------------------------------------------------------ axu_scenario_list

        /// <summary>
        /// 枚举可用的测试能力与工程内 suite/scenario 文件。能力部分为包内建静态清单（注册表为运行时态，
        /// Editor 进程内不可靠）；文件部分扫描 Assets 下 *.suite.json / *.scenario.json（路径 + id +
        /// scenario 数摘要，解析失败以 error 字段给出原因）。
        /// </summary>
        [CliCommand("axu_scenario_list", "List AxuTestFramework built-in capabilities and suite/scenario JSON files under Assets (path + scenario count summary)", MainThreadRequired = false, Tags = new[] { "tests", "axu", "scenario" })]
        public static object ListScenarios()
        {
            try
            {
                var capabilities = BuiltInCapabilities
                    .Select(c => new { id = c.Id, name = c.Name, description = c.Description })
                    .ToArray();

                var files = new List<object>();
                var projectRoot = AxuTestReportPaths.ProjectRootFullPath;
                foreach (var fullPath in EnumerateScenarioFiles(Application.dataPath))
                {
                    var entry = new Dictionary<string, object>
                    {
                        ["path"] = Path.GetRelativePath(projectRoot, fullPath).Replace('\\', '/'),
                        ["absolutePath"] = fullPath,
                    };

                    try
                    {
                        var schema = (string)JObject.Parse(File.ReadAllText(fullPath))["schema"];
                        if (schema == AxuTestFramework.Core.SchemaNames.Scenario)
                        {
                            var scenario = AxuTestFramework.Core.Scenarios.SuiteLoader.LoadScenario(fullPath);
                            entry["kind"] = "scenario";
                            entry["id"] = scenario.Id;
                            entry["scenarioCount"] = 1;
                            entry["capability"] = scenario.Capability;
                        }
                        else
                        {
                            // 默认按 suite 解析（LoadSuite 对 schema 不符会抛错进 catch）
                            var suite = AxuTestFramework.Core.Scenarios.SuiteLoader.LoadSuite(fullPath);
                            var resolved = AxuTestFramework.Core.Scenarios.SuiteLoader.Resolve(
                                suite, null, Path.GetDirectoryName(fullPath));
                            entry["kind"] = "suite";
                            entry["id"] = suite.Id;
                            entry["scenarioCount"] = resolved.Count;
                            entry["scenarioIds"] = resolved.Select(r => r.Id).ToArray();
                        }
                    }
                    catch (Exception ex)
                    {
                        entry["kind"] = "unknown";
                        entry["error"] = ex.Message;
                    }

                    files.Add(entry);
                }

                return new
                {
                    status = "ok",
                    capabilities,
                    files,
                    fileCount = files.Count
                };
            }
            catch (Exception ex)
            {
                return new { status = "error", message = ex.Message };
            }
        }

        /// <summary>扫描目录下 *.suite.json 与 *.scenario.json（按文件名排序，稳定输出）。</summary>
        static IEnumerable<string> EnumerateScenarioFiles(string root)
        {
            if (!Directory.Exists(root))
                return Enumerable.Empty<string>();

            return Directory.EnumerateFiles(root, "*.suite.json", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(root, "*.scenario.json", SearchOption.AllDirectories))
                .OrderBy(p => p, StringComparer.Ordinal);
        }

        // ------------------------------------------------------------------ axu_scenario_run

        /// <summary>
        /// 在 Editor 内以 PlayMode 驱动 Agent Runner 跑指定 suite/scenario JSON。异步形态（与 axu_test_run
        /// PlayMode 一致）：启动即返回 running，调用方轮询 <c>axu_scenario_status</c> 至 completed/error；
        /// 完成时看门狗自动退出 Play 并把报告指标写入趋势 CSV（Library/AxuTestReports/trend/trend.csv），
        /// reportPath/exitCode/summary 见状态文件。
        /// </summary>
        [CliCommand("axu_scenario_run", "Run a suite/scenario JSON in Editor PlayMode via the AxuTest agent runner (async; poll axu_scenario_status; trend CSV appended on completion)", MainThreadRequired = true, Tags = new[] { "tests", "axu", "scenario" })]
        public static object RunScenarioSuite(
            [CliArg("suite", "Suite or standalone scenario JSON path (relative to project root or absolute; required)")] string suite = "",
            [CliArg("scenario", "Optional: only run these scenario ids inside the suite (comma-separated)")] string scenario = "")
        {
            var startedUtc = DateTime.UtcNow;

            lock (AxuTestRunService.RunGate)
            {
                var busyReason = CheckMutualExclusion();
                if (busyReason != null)
                    return ErrorEnvelope(startedUtc, busyReason);
                _scenarioRunBusy = true;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(suite))
                    return ErrorEnvelope(startedUtc, "缺少 suite 参数：需给出 suite 或单 scenario JSON 路径（相对工程根或绝对）。");

                var scenarioIds = string.IsNullOrWhiteSpace(scenario)
                    ? null
                    : scenario.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                if (!ScenarioRunService.StartEditorPlayRun(suite, scenarioIds, out var error))
                    return ErrorEnvelope(startedUtc, error);

                return new
                {
                    success = true,
                    status = "running",
                    suitePath = AxuTestReportPaths.ToFullPath(suite),
                    scenarioIds = scenarioIds?.ToArray() ?? new string[0],
                    statusPath = ScenarioRunService.StatusFileFullPath,
                    startedAtUtc = startedUtc.ToString("o"),
                    message = "已进入 Play Mode 驱动 Agent Runner——轮询 axu_scenario_status 至 completed/error。" +
                              "完成时自动退出 Play 并把报告指标追加进趋势 CSV（axu_trend_summary 可查）。"
                };
            }
            catch (Exception ex)
            {
                return ErrorEnvelope(startedUtc, $"启动失败: {ex.Message}");
            }
            finally
            {
                lock (AxuTestRunService.RunGate)
                {
                    _scenarioRunBusy = false;
                }
            }
        }

        // ------------------------------------------------------------------ axu_scenario_status

        /// <summary>
        /// 查询 scenario 运行状态（读 Temp/axu_scenario_status.json 原文，字段与 Agent 侧 ScenarioStatusFile 同构）：
        /// status = running（含 completedCount/totalCount 进度）/ completed / error；completed 时携带
        /// reportPath / summary / exitCode。无状态文件时返回 idle 信封。
        /// </summary>
        [CliCommand("axu_scenario_status", "Get axu_scenario_run status (reads Temp/axu_scenario_status.json; poll this for async PlayMode scenario runs)", MainThreadRequired = false, Tags = new[] { "tests", "axu", "scenario" })]
        public static string GetScenarioStatus()
        {
            try
            {
                var text = ScenarioRunService.TryReadStatusText();
                if (text != null)
                    return text;
            }
            catch (Exception ex)
            {
                return "{\"status\":\"error\",\"message\":" + JsonEscape(ex.Message) + "}";
            }
            return "{\"status\":\"idle\",\"message\":\"No scenario run in progress (no status file).\"}";
        }

        // ------------------------------------------------------------------ 内部

        /// <summary>
        /// 互斥闸检查（RunGate 内调用）：任一在途即返回拒绝原因，否则 null。
        /// 覆盖：axu_test_run（IsAxuRunInFlight + pipeline run_tests 请求文件）与本族 scenario/player 运行。
        /// </summary>
        static string CheckMutualExclusion()
        {
            if (_scenarioRunBusy)
                return "已有 axu_scenario_run 在途。";
            if (ScenarioRunService.IsScenarioRunInFlight())
                return "已有 scenario 运行在途（Temp/axu_scenario_status.json 标记 running）。请先经 axu_scenario_status 观察或等其完成。";
            if (AxuTestRunService.IsAxuRunInFlight())
                return "已有 axu 测试运行在途（Temp/axu_test_run_status.json 标记 running）。请先经 axu_test_status 观察或等其完成。";
            if (File.Exists(AxuTestReportPaths.ToFullPath("Temp/pipeline_test_request.json")))
                return "pipeline 自带 run_tests 的异步运行在途（Temp/pipeline_test_request.json 存在），拒绝并发驱动。";
            return null;
        }

        static object ErrorEnvelope(DateTime startedUtc, string message)
        {
            return new
            {
                success = false,
                status = "error",
                startedAtUtc = startedUtc.ToString("o"),
                error = message,
                message
            };
        }

        /// <summary>极简 JSON 字符串转义（状态命令兜底路径，与 AxuTestPipelineCommands 同款）。</summary>
        static string JsonEscape(string text)
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
