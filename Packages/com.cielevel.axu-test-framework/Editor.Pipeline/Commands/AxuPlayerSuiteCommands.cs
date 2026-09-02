using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Player 词汇族 pipeline 命令（方案 §3.5 / §3.3，Phase 4）：
    /// <c>axu_player_suite_build</c>（构建 Development Player，构建期临时确保 AXU_TEST_AGENT define）与
    /// <c>axu_player_suite_run</c>（以独立进程跑 Player suite：§3.3 参数 → 等待退出码 → 报告落
    /// Library/AxuTestReports/scenario/ 带时间戳子目录 → 趋势入库）。
    /// <para>并发纪律：与 axu_test_run / axu_scenario_run 共享 <see cref="AxuTestRunService.RunGate"/>
    /// 静态互斥闸（在途运行/构建期间拒绝新的驱动请求）。</para>
    /// </summary>
    public static class AxuPlayerSuiteCommands
    {
        /// <summary>Player 构建输出默认目录（相对工程根；macOS 实际产物为其下 &lt;产品名&gt;.app）。</summary>
        public const string DefaultBuildDirectory = "Builds/AxuScenarioPlayer";

        /// <summary>Player 运行默认目录（axu_player_suite_run 的 player 参数缺省值）。</summary>
        public const string DefaultPlayerDirectory = "Builds/AxuScenarioPlayer";

        static bool _buildBusy;
        static bool _playerRunBusy;

        // ------------------------------------------------------------------ axu_player_suite_build

        /// <summary>
        /// 构建 Development Player 到 <see cref="DefaultBuildDirectory"/>（增量 BuildPipeline.BuildPlayer，同步）。
        /// 构建期临时确保当前 BuildTargetGroup 开启 <see cref="AgentDefineMenu.DefineName"/>
        /// （AXU_TEST_AGENT，经 PlayerSettings API——ProjectSettings 直改不会被编译管线感知），
        /// 构完在 finally 中还原原符号串。场景清单 = Build Settings（EditorBuildSettings.scenes，enabled 项）。
        /// </summary>
        [CliCommand("axu_player_suite_build", "Build a Development Player to Builds/AxuScenarioPlayer with AXU_TEST_AGENT temporarily ensured (define restored after; scenes from Build Settings)", MainThreadRequired = true, Tags = new[] { "tests", "axu", "player" })]
        public static object BuildPlayerSuite(
            [CliArg("output", "Build output path override (relative to project root or absolute; macOS convention: <dir>/<product>.app). Default Builds/AxuScenarioPlayer/<product>.app")] string output = "")
        {
            var startedUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            lock (AxuTestRunService.RunGate)
            {
                var busyReason = CheckMutualExclusion();
                if (busyReason != null)
                    return BuildError(startedUtc, busyReason);
                _buildBusy = true;
            }

            try
            {
                if (EditorApplication.isPlaying)
                    return BuildError(startedUtc, "Editor 处于 Play Mode，拒绝构建。请先退出 Play。");
                if (EditorApplication.isCompiling)
                    return BuildError(startedUtc, "Editor 正在编译脚本，拒绝构建。请稍后重试。");

                var scenes = EditorBuildSettings.scenes
                    .Where(s => s != null && s.enabled && !string.IsNullOrEmpty(s.path))
                    .Select(s => s.path)
                    .ToArray();
                if (scenes.Length == 0)
                    return BuildError(startedUtc, "Build Settings 场景清单为空：Player suite 需要至少一个启用场景（场景类能力运行前置）。");

                var productName = string.IsNullOrWhiteSpace(PlayerSettings.productName) ? "Player" : PlayerSettings.productName;
                var location = string.IsNullOrWhiteSpace(output)
                    ? Path.Combine(DefaultBuildDirectory, productName + ".app")
                    : output;
                var locationFullPath = AxuTestReportPaths.ToFullPath(location);
                var locationDirectory = Path.GetDirectoryName(locationFullPath);
                if (!string.IsNullOrEmpty(locationDirectory) && !Directory.Exists(locationDirectory))
                    Directory.CreateDirectory(locationDirectory);

                // ---- 构建期临时确保 AXU_TEST_AGENT（try/finally 可靠还原；define 菜单同款 API 路径） ----
                var named = AgentDefineMenu.GetActiveNamedBuildTarget();
                var originalDefines = PlayerSettings.GetScriptingDefineSymbols(named);
                var defineAdded = !AgentDefineMenu.HasAgentDefine();
                if (defineAdded)
                {
                    PlayerSettings.SetScriptingDefineSymbols(named, originalDefines + ";" + AgentDefineMenu.DefineName);
                    UnityEngine.Debug.Log($"[AxuTestFramework] 构建期临时开启 {AgentDefineMenu.DefineName}（原符号串已记录，构完还原）: \"{originalDefines}\"");
                }

                BuildReport report;
                try
                {
                    var options = new BuildPlayerOptions
                    {
                        scenes = scenes,
                        locationPathName = locationFullPath,
                        target = EditorUserBuildSettings.activeBuildTarget,
                        options = BuildOptions.Development
                    };
                    report = BuildPipeline.BuildPlayer(options);
                }
                finally
                {
                    if (defineAdded)
                        PlayerSettings.SetScriptingDefineSymbols(named, originalDefines);
                }

                stopwatch.Stop();
                var result = report.summary.result;
                var success = result == BuildResult.Succeeded;

                return new
                {
                    success,
                    status = success ? "completed" : "error",
                    buildResult = result.ToString(),
                    outputPath = report.summary.outputPath,
                    requestedPath = locationFullPath,
                    sceneCount = scenes.Length,
                    totalSizeBytes = report.summary.totalSize,
                    durationMs = stopwatch.ElapsedMilliseconds,
                    startedAtUtc = startedUtc.ToString("o"),
                    endedAtUtc = DateTime.UtcNow.ToString("o"),
                    message = success
                        ? $"Player 构建完成（{scenes.Length} 场景，{stopwatch.Elapsed.TotalSeconds:0.#}s）: {report.summary.outputPath}。可用 axu_player_suite_run 启动运行。"
                        : $"Player 构建失败（{result}），详见 Editor Console / 构建日志。"
                };
            }
            catch (Exception ex)
            {
                return BuildError(startedUtc, $"构建异常: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                lock (AxuTestRunService.RunGate)
                {
                    _buildBusy = false;
                }
            }
        }

        // ------------------------------------------------------------------ axu_player_suite_run

        /// <summary>
        /// 以独立进程运行 Player suite：组装 §3.3 参数（-axuSuite 绝对路径 / -axuQuitOnDone / -axuReport
        /// 指向 Library/AxuTestReports/scenario/player-{时间戳}-{suite}/report.json；可选 -axuScenario /
        /// -axuWarnAsFail / -axuSeed 透传）→ 等待进程退出（退出码 = 判定）→ 读回报告并 TrendStore.Append
        /// 入库趋势 → 返回 exitCode + reportPath。macOS 的 .app 自动拼 Contents/MacOS/&lt;产品名&gt; 可执行。
        /// </summary>
        [CliCommand("axu_player_suite_run", "Run a suite in the built Player process (-axuSuite/-axuQuitOnDone/-axuReport per plan 3.3), wait for exit code, pull the report into Library/AxuTestReports/scenario/ and append trend CSV", MainThreadRequired = true, Tags = new[] { "tests", "axu", "player" })]
        public static async Task<object> RunPlayerSuite(
            [CliArg("suite", "Suite or standalone scenario JSON path (relative to project root or absolute; required)")] string suite = "",
            [CliArg("player", "Player path (relative to project root or absolute; macOS .app bundle or plain executable; directory = search first .app inside). Default Builds/AxuScenarioPlayer")] string player = "",
            [CliArg("scenario", "Optional: only run these scenario ids (-axuScenario, comma-separated for multiple)")] string scenario = "",
            [CliArg("seed", "Optional random seed override (-axuSeed); omit to use scenario defaults")] int seed = int.MinValue,
            [CliArg("warn_as_fail", "Treat warn verdicts as failures (-axuWarnAsFail, default false; suite settings warnAsFail still applies on top)")] bool warn_as_fail = false,
            [CliArg("timeout_ms", "Wait budget for the player process in milliseconds (default 3600000 = 1h; 0 = wait indefinitely); on timeout the process is killed")] int timeout_ms = 3600000)
        {
            var startedUtc = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            lock (AxuTestRunService.RunGate)
            {
                var busyReason = CheckMutualExclusion();
                if (busyReason != null)
                    return PlayerError(startedUtc, busyReason);
                _playerRunBusy = true;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(suite))
                    return PlayerError(startedUtc, "缺少 suite 参数：需给出 suite 或单 scenario JSON 路径（相对工程根或绝对）。");

                var suiteFullPath = AxuTestReportPaths.ToFullPath(suite);
                if (!File.Exists(suiteFullPath))
                    return PlayerError(startedUtc, $"suite/scenario JSON 不存在: {suiteFullPath}");

                var executable = ResolvePlayerExecutable(string.IsNullOrWhiteSpace(player) ? DefaultPlayerDirectory : player);
                if (executable == null)
                    return PlayerError(startedUtc,
                        $"未找到可执行的 Player（查找于 {AxuTestReportPaths.ToFullPath(string.IsNullOrWhiteSpace(player) ? DefaultPlayerDirectory : player)}）。" +
                        "请先执行 axu_player_suite_build，或用 player 参数显式指定 .app / 可执行文件路径。");

                // ---- 报告落点：Library/AxuTestReports/scenario/player-{时间戳}-{suite 名}/report.json ----
                var suiteStem = Path.GetFileNameWithoutExtension(suiteFullPath);
                var reportDirectory = Path.Combine(
                    AxuScenarioTrendUtil.ScenarioReportRootFullPath,
                    $"player-{DateTime.Now:yyyyMMdd-HHmmss}-{suiteStem}");
                var reportFullPath = Path.Combine(reportDirectory, "report.json");
                Directory.CreateDirectory(reportDirectory);

                // ---- §3.3 参数组装 ----
                var arguments = new System.Text.StringBuilder();
                arguments.Append("-axuSuite ").Append(Quote(suiteFullPath));
                arguments.Append(" -axuQuitOnDone");
                arguments.Append(" -axuReport ").Append(Quote(reportFullPath));
                if (!string.IsNullOrWhiteSpace(scenario))
                {
                    foreach (var id in scenario.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
                        arguments.Append(" -axuScenario ").Append(Quote(id));
                }
                if (warn_as_fail)
                    arguments.Append(" -axuWarnAsFail");
                if (seed != int.MinValue)
                    arguments.Append(" -axuSeed ").Append(seed);

                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(executable) ?? "."
                };

                int exitCode;
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return PlayerError(startedUtc, $"Player 进程启动失败: {executable}");

                    // 当前 API 兼容级别无 Process.WaitForExitAsync：轮询 HasExited（200ms 步长），
                    // 超时 Kill（本 Profile 无整树重载；Player 子进程随父进程收尾）
                    var deadline = timeout_ms > 0 ? DateTime.UtcNow.AddMilliseconds(timeout_ms) : DateTime.MaxValue;
                    while (!process.HasExited)
                    {
                        if (DateTime.UtcNow >= deadline)
                        {
                            try { process.Kill(); } catch (Exception) { /* 已退出 */ }
                            return PlayerError(startedUtc,
                                $"Player 运行超时（>{timeout_ms}ms），进程已终止。部分报告（若已 flush）在: {reportFullPath}");
                        }
                        await Task.Delay(200).ConfigureAwait(true);
                    }
                    exitCode = process.ExitCode;
                }

                stopwatch.Stop();

                // ---- 报告回读 + 趋势入库 ----
                string trendError = null;
                string suiteId = null;
                if (File.Exists(reportFullPath))
                {
                    if (!AxuScenarioTrendUtil.TryAppendReport(reportFullPath, out suiteId, out trendError))
                        UnityEngine.Debug.LogWarning($"[AxuTestFramework] Player 报告趋势入库失败: {trendError}");
                }
                else
                {
                    trendError = "Player 未产出报告文件（进程可能异常退出）。";
                }

                return new
                {
                    success = exitCode == 0,
                    status = "completed",
                    exitCode,
                    reportPath = File.Exists(reportFullPath) ? reportFullPath : null,
                    suiteId,
                    playerPath = executable,
                    trendAppended = trendError == null,
                    trendError,
                    durationMs = stopwatch.ElapsedMilliseconds,
                    startedAtUtc = startedUtc.ToString("o"),
                    endedAtUtc = DateTime.UtcNow.ToString("o"),
                    message = $"Player 退出码 {exitCode}" +
                              (File.Exists(reportFullPath) ? $"；报告: {reportFullPath}" : "；无报告产出") +
                              (trendError == null ? "；趋势已入库（axu_trend_summary 可查）。" : $"；{trendError}")
                };
            }
            catch (Exception ex)
            {
                return PlayerError(startedUtc, $"Player 运行异常: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                lock (AxuTestRunService.RunGate)
                {
                    _playerRunBusy = false;
                }
            }
        }

        // ------------------------------------------------------------------ 内部

        /// <summary>互斥检查（与 axu_scenario_run / axu_test_run 共闸；_buildBusy/_playerRunBusy 本类自查）。</summary>
        static string CheckMutualExclusion()
        {
            if (_buildBusy)
                return "已有 axu_player_suite_build 在途。";
            if (_playerRunBusy)
                return "已有 axu_player_suite_run 在途。";
            if (ScenarioRunService.IsScenarioRunInFlight())
                return "已有 scenario 运行在途（Temp/axu_scenario_status.json 标记 running），拒绝并发驱动；running 超 15 分钟视为陈旧自动放行，确认无运行可删该文件手动解锁。";
            if (AxuTestRunService.IsAxuRunInFlight())
                return "已有 axu 测试运行在途（Temp/axu_test_run_status.json 标记 running），拒绝并发驱动；running 超 15 分钟视为陈旧自动放行，确认无运行可删该文件手动解锁。";
            if (File.Exists(AxuTestReportPaths.ToFullPath("Temp/pipeline_test_request.json")))
                return "pipeline 自带 run_tests 的异步运行在途（Temp/pipeline_test_request.json 存在），拒绝并发驱动。";
            return null;
        }

        /// <summary>
        /// 解析 Player 可执行文件路径：macOS .app → Contents/MacOS/&lt;产品名&gt;（缺失时取 MacOS 目录首个文件）；
        /// 目录 → 取其下首个 .app 再走上述规则；可执行文件本体直接返回。
        /// </summary>
        static string ResolvePlayerExecutable(string playerPath)
        {
            var fullPath = AxuTestReportPaths.ToFullPath(playerPath);

            if (File.Exists(fullPath))
                return fullPath;

            var appPath = fullPath;
            if (Directory.Exists(appPath) && !appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                appPath = Directory.EnumerateDirectories(appPath, "*.app", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
                if (appPath == null)
                    return null;
            }

            if (!Directory.Exists(appPath) || !appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                return null;

            var productName = string.IsNullOrWhiteSpace(PlayerSettings.productName) ? null : PlayerSettings.productName;
            var macOsDirectory = Path.Combine(appPath, "Contents", "MacOS");
            if (!Directory.Exists(macOsDirectory))
                return null;

            if (!string.IsNullOrEmpty(productName))
            {
                var candidate = Path.Combine(macOsDirectory, productName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return Directory.EnumerateFiles(macOsDirectory).FirstOrDefault();
        }

        static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        static object BuildError(DateTime startedUtc, string message)
        {
            return new { success = false, status = "error", startedAtUtc = startedUtc.ToString("o"), error = message, message };
        }

        static object PlayerError(DateTime startedUtc, string message)
        {
            return new { success = false, status = "error", exitCode = -1, startedAtUtc = startedUtc.ToString("o"), error = message, message };
        }
    }
}
