using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 环境自检 pipeline 命令：<c>axu_doctor</c>——面向 Agent/CI 的一键体检。
    /// 覆盖：pipeline 描述符、必需/可选包、AXU_TEST_AGENT define、Editor 忙闲、
    /// 在途运行（互斥闸占用预判）、报告存储可写性、suite 盘点。
    /// <para>纪律：纯诊断——除一个自清理的写探针外只读，不改动任何工程状态；
    /// <b>不参与 RunGate 互斥闸</b>（测试/scenario 在途时正是需要体检的时刻，随时可调用）。</para>
    /// <para>顶层 status 语义与 scenario 退出码一致：ok=全过 / warn=仅警告 / error=有失败项。</para>
    /// </summary>
    public static class AxuDoctorCommand
    {
        /// <summary>在途状态的新鲜度阈值：running 超过该时长视为陈旧残留，不判在途（与 .zcode/08 的 15 分钟口径一致）。</summary>
        private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(15);

        /// <summary>检查项登记委托（hint 省略 = 输出不含 hint 字段；委托的可选参数对调用方透明）。</summary>
        private delegate void AddCheck(string id, string status, string message, string hint = null);

        /// <summary>
        /// 执行全量环境自检，返回结构化信封：
        /// {status, summary:{pass,warn,fail}, environment, checks[{id,status,message,hint?}]}。
        /// 单项检查异常只降级该项为 fail，不影响其余检查（体检的意义就是尽可能多拿信息）。
        /// </summary>
        [CliCommand("axu_doctor",
            "Environment health check for the AxuTest toolchain: pipeline descriptor, required/optional packages, AXU_TEST_AGENT define, editor busy state, in-flight runs (mutex-gate precheck), report storage writability, suite inventory. Read-only (plus one self-cleaning write probe); NOT gated by the run mutex - safe to call anytime.",
            MainThreadRequired = true, Tags = new[] { "axu", "doctor", "diagnostics" })]
        public static object Run()
        {
            var checks = new List<object>();
            int pass = 0, warn = 0, fail = 0;

            void Add(string id, string status, string message, string hint = null)
            {
                if (status == "pass") pass++;
                else if (status == "warn") warn++;
                else fail++;
                checks.Add(hint == null
                    ? (object)new { id, status, message }
                    : new { id, status, message, hint });
            }

            // ---- 包枚举（多项检查共用；域重载窗口可能拿不到，拿不到则包类检查整体降级 warn） ----
            PackageInfo[] all = null;
            try { all = PackageInfo.GetAllRegisteredPackages(); }
            catch (Exception ex)
            {
                Add("packages", "warn", $"PackageInfo 枚举暂不可用（{ex.GetType().Name}），跳过包类检查。", "多半处于域重载窗口，稍后重试 axu_doctor。");
            }

            string VersionOf(string packageName)
            {
                return all?.FirstOrDefault(p => p.name == packageName)?.version;
            }

            CheckPipelineDescriptor(Add);
            CheckPackages(Add, VersionOf, all != null);
            CheckAgentDefine(Add);
            CheckEditorState(Add);
            CheckInFlightRuns(Add);
            CheckReportStorage(Add);
            CheckSuiteInventory(Add);

            return new
            {
                status = fail > 0 ? "error" : (warn > 0 ? "warn" : "ok"),
                summary = new { pass, warn, fail },
                environment = new
                {
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    productName = Application.productName,
                    projectRoot = AxuTestReportPaths.ProjectRootFullPath,
                    packageVersion = VersionOf("com.cielevel.axu-test-framework") ?? "unknown",
                    pipelineVersion = VersionOf("com.unity.pipeline") ?? "unknown",
                    checkedAtUtc = DateTime.UtcNow.ToString("o")
                },
                checks
            };
        }

        // ------------------------------------------------------------------
        // 1. pipeline 描述符（Library/Pipeline/.unity-pipeline-port）
        // ------------------------------------------------------------------
        private static void CheckPipelineDescriptor(AddCheck add)
        {
            try
            {
                var descriptorPath = AxuTestReportPaths.ToFullPath("Library/Pipeline/.unity-pipeline-port");
                if (!File.Exists(descriptorPath))
                {
                    add("pipeline.descriptor", "fail",
                        "pipeline 描述符缺失：Library/Pipeline/.unity-pipeline-port 不存在（Editor 启动后应由 pipeline 服务写入）。",
                        "确认 com.unity.pipeline 已安装且 Editor 完成启动；必要时重启 Editor。");
                    return;
                }

                var descriptor = JObject.Parse(File.ReadAllText(descriptorPath));
                var port = descriptor["port"]?.Value<int>() ?? 0;
                var hasToken = !string.IsNullOrEmpty((string)descriptor["evalToken"]);
                var ageMinutes = (DateTime.UtcNow - File.GetLastWriteTimeUtc(descriptorPath)).TotalMinutes;

                if (port <= 0 || !hasToken)
                {
                    add("pipeline.descriptor", "fail",
                        $"描述符内容不完整：port={port}，evalToken {(hasToken ? "在位" : "缺失")}。",
                        "删除该文件并重启 Editor，让 pipeline 服务重新写入。");
                    return;
                }

                // 安全纪律：token 值永不回显，只报存在性
                add("pipeline.descriptor", "pass",
                    $"port={port}，evalToken 在位（值不回显），描述符写入于 {ageMinutes:F1} 分钟前。本命令经 HTTP 到达即证明服务在役、token 有效。");
            }
            catch (Exception ex)
            {
                add("pipeline.descriptor", "fail", $"描述符读取异常：{ex.GetType().Name}: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // 2/3. 包清单：必需（test-framework / newtonsoft-json）与可选（pipeline / inputsystem / ugui）
        // ------------------------------------------------------------------
        private static void CheckPackages(AddCheck add, Func<string, string> versionOf, bool enumerable)
        {
            if (!enumerable)
                return; // 枚举不可用时已在 packages 项报 warn，不重复

            var utf = versionOf("com.unity.test-framework");
            var json = versionOf("com.unity.nuget.newtonsoft-json");
            var missing = new List<string>();
            if (utf == null) missing.Add("com.unity.test-framework");
            if (json == null) missing.Add("com.unity.nuget.newtonsoft-json");

            if (missing.Count > 0)
            {
                add("packages.required", "fail",
                    $"必需包缺失：{string.Join("、", missing)}。",
                    "经 Package Manager 安装后重试（缺包时测试程序集不编译，是特性但 UTF 链路不可用）。");
            }
            else
            {
                add("packages.required", "pass",
                    $"com.unity.test-framework {utf}、com.unity.nuget.newtonsoft-json {json} 在位。");
            }

            var pipe = versionOf("com.unity.pipeline");
            var input = versionOf("com.unity.inputsystem");
            var ugui = versionOf("com.unity.ugui");

            if (pipe == null)
            {
                add("packages.optional", "warn",
                    "未枚举到 com.unity.pipeline——但本命令能执行说明其在役，多半是枚举时机问题。",
                    "稍后重试 axu_doctor；持续复现则检查包解析状态。");
                return;
            }

            if (input == null && ugui == null)
            {
                add("packages.optional", "warn",
                    $"com.unity.pipeline {pipe} 在位；com.unity.inputsystem 与 com.unity.ugui 均缺失——monkey 能力降级为仅记录坐标。",
                    "安装 InputSystem 或 uGUI 以启用 monkey 输入注入 / UGUI 命中检测。");
            }
            else
            {
                var backends = new List<string>();
                if (input != null) backends.Add($"InputSystem {input}");
                if (ugui != null) backends.Add($"uGUI {ugui}");
                add("packages.optional", "pass",
                    $"com.unity.pipeline {pipe} 在位；Agent 输入后端：{string.Join("、", backends)}。");
            }
        }

        // ------------------------------------------------------------------
        // 4. AXU_TEST_AGENT define（scenario Agent 代码的编译开关）
        // ------------------------------------------------------------------
        private static void CheckAgentDefine(AddCheck add)
        {
            try
            {
                if (AgentDefineMenu.HasAgentDefine())
                {
                    add("defines.axu_test_agent", "pass", "AXU_TEST_AGENT 已开启（当前 BuildTarget 符号串）。");
                }
                else
                {
                    add("defines.axu_test_agent", "warn",
                        "AXU_TEST_AGENT 未开启——AgentBootstrap 等 #if 守护代码不参与编译，axu_scenario_* / axu_player_* 运行路径不可用；UTF 测试（axu_test_*）不受影响。",
                        $"菜单 {AgentDefineMenu.MenuPath}，或向当前 BuildTarget 符号串追加 AXU_TEST_AGENT（触发一次重编译，属预期）。");
                }
            }
            catch (Exception ex)
            {
                add("defines.axu_test_agent", "fail", $"define 读取异常：{ex.GetType().Name}: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // 5. Editor 忙闲（Play/编译/资源刷新期间命令响应可能延迟）
        // ------------------------------------------------------------------
        private static void CheckEditorState(AddCheck add)
        {
            try
            {
                var busy = new List<string>();
                if (EditorApplication.isPlayingOrWillChangePlaymode) busy.Add("playing");
                if (EditorApplication.isCompiling) busy.Add("compiling");
                if (EditorApplication.isUpdating) busy.Add("updating");

                if (busy.Count == 0)
                {
                    add("editor.state", "pass",
                        $"Editor 空闲（已运行 {EditorApplication.timeSinceStartup / 60.0:F0} 分钟）。");
                }
                else
                {
                    add("editor.state", "warn",
                        $"Editor 忙：{string.Join("、", busy)}——命令响应可能延迟；Play 长跑期间严禁并发派发其他运行类命令。",
                        "等待回到 EditMode 空闲态再派发 axu_test_run / axu_scenario_run。");
                }
            }
            catch (Exception ex)
            {
                add("editor.state", "fail", $"Editor 状态读取异常：{ex.GetType().Name}: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // 6. 在途运行（互斥闸占用预判：新鲜 running 状态 = 新运行会被拒绝）
        // ------------------------------------------------------------------
        private static void CheckInFlightRuns(AddCheck add)
        {
            try
            {
                var inFlight = new List<string>();

                if (IsFreshRunning(AxuTestReportPaths.ToFullPath(AxuTestReportPaths.StatusFilePath), out var utfStarted))
                    inFlight.Add($"axu_test_run（startedAtUtc {utfStarted:HH:mm:ss}Z）");

                if (IsFreshRunning(ScenarioRunService.StatusFileFullPath, out var scenarioStarted))
                    inFlight.Add($"axu_scenario_run（startedAtUtc {scenarioStarted:HH:mm:ss}Z）");

                if (inFlight.Count == 0)
                {
                    add("runs.in_flight", "pass", "无在途运行（互斥闸空闲，可立即派发新运行）。");
                }
                else
                {
                    add("runs.in_flight", "warn",
                        $"检测到在途运行：{string.Join("、", inFlight)}——互斥闸占用中，新的运行类命令会被拒绝。",
                        "轮询 axu_test_status / axu_scenario_status 至 completed/error；陈旧（>15 分钟）的 running 不视为在途。");
                }
            }
            catch (Exception ex)
            {
                add("runs.in_flight", "fail", $"在途状态读取异常：{ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>状态文件是否为"新鲜 running"：status==running 且 startedAtUtc 未越过陈旧阈值（无法解析时间时保守视为新鲜）。</summary>
        private static bool IsFreshRunning(string statusFileFullPath, out DateTime startedUtc)
        {
            startedUtc = DateTime.MinValue;
            if (string.IsNullOrEmpty(statusFileFullPath) || !File.Exists(statusFileFullPath))
                return false;

            var json = JObject.Parse(File.ReadAllText(statusFileFullPath));
            if ((string)json["status"] != "running")
                return false;

            if (DateTime.TryParse((string)json["startedAtUtc"], null,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                startedUtc = parsed;
                return DateTime.UtcNow - parsed <= StaleThreshold;
            }
            return true;
        }

        // ------------------------------------------------------------------
        // 7. 报告存储（Library/AxuTestReports 可写性探针 + 存量盘点）
        // ------------------------------------------------------------------
        private static void CheckReportStorage(AddCheck add)
        {
            try
            {
                var reportsDir = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ReportsDirectory);
                Directory.CreateDirectory(reportsDir);
                var probe = Path.Combine(reportsDir, ".doctor-probe-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);

                var utfReports = Directory.EnumerateFiles(reportsDir, "*.json", SearchOption.TopDirectoryOnly).Count();

                var scenarioDir = AxuTestReportPaths.ToFullPath(AxuTrendPaths.ScenarioReportRoot);
                var scenarioReports = Directory.Exists(scenarioDir)
                    ? Directory.EnumerateFiles(scenarioDir, "*.json", SearchOption.AllDirectories)
                        .Count(f => !f.EndsWith(".partial.json", StringComparison.OrdinalIgnoreCase))
                    : 0;

                var trendPath = AxuTestReportPaths.ToFullPath(AxuTrendPaths.TrendCsvPath);
                var trendInfo = File.Exists(trendPath)
                    ? $"趋势 CSV 存在（{new FileInfo(trendPath).Length / 1024.0:F1} KB）"
                    : "趋势 CSV 尚未生成";

                add("reports.storage", "pass",
                    $"可写（探针已自清理）。存量：UTF 报告 {utfReports} 份、scenario 报告 {scenarioReports} 份、{trendInfo}。");
            }
            catch (Exception ex)
            {
                add("reports.storage", "fail",
                    $"Library/AxuTestReports 不可写：{ex.GetType().Name}: {ex.Message}。",
                    "检查目录权限与磁盘空间；报告/截图/趋势全部依赖该目录。");
            }
        }

        // ------------------------------------------------------------------
        // 8. suite 盘点（Assets 下 *.suite.json / *.scenario.json，口径与 axu_scenario_list 一致）
        // ------------------------------------------------------------------
        private static void CheckSuiteInventory(AddCheck add)
        {
            try
            {
                var suites = Directory.EnumerateFiles(Application.dataPath, "*.suite.json", SearchOption.AllDirectories).Count();
                var scenarios = Directory.EnumerateFiles(Application.dataPath, "*.scenario.json", SearchOption.AllDirectories).Count();

                add("suites.inventory", "pass",
                    $"Assets 下 suite {suites} 个、独立 scenario {scenarios} 个。",
                    suites == 0 && scenarios == 0
                        ? "尚无 suite——可经菜单 AxuTestFramework/Create Suite JSON 生成模板。"
                        : null);
            }
            catch (Exception ex)
            {
                add("suites.inventory", "fail", $"suite 盘点异常：{ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
