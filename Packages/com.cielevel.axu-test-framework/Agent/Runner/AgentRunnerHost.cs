using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AxuTestFramework.Agent.Bootstrap;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Reports;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Budget;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AxuTestFramework.Agent.Runner
{
    /// <summary>
    /// 场景测试运行宿主（核心状态机）：
    /// 启动来源（Editor 桥文件 &gt; 命令行参数，皆无则静默待命）→ 加载 suite（兼容单 scenario 文件）
    /// → 逐条顺序执行（能力分发、真实时间超时看门狗、异常兜底）→ 汇总报告/状态文件/退出码。
    /// </summary>
    public class AgentRunnerHost : MonoBehaviour
    {
        /// <summary>宿主 GameObject 名（DontDestroyOnLoad + DontSave）。</summary>
        public const string HostObjectName = "AxuTestAgentHost";

        /// <summary>scenarioTimeoutSec 未配置时的默认看门狗超时（秒）。</summary>
        public const double DefaultScenarioTimeoutSec = 600.0;

#if AXU_TEST_AGENT
        bool _isRunning;
        // Unity 6 已移除 Application.exitCode 属性：退出码经 Application.Quit(int) 在退出时携带（见 MaybeQuit）
        int _exitCode;
#endif

        /// <summary>是否正在运行 suite。</summary>
        public bool IsRunning
        {
            get
            {
#if AXU_TEST_AGENT
                return _isRunning;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 从启动来源解析并开始运行：先读 Editor 桥文件（读到立即删除），否则解析命令行；
        /// 两者皆无则静默待命（不报错）。防重入只看 <see cref="_isRunning"/>——
        /// 宿主 GO 是 DontDestroyOnLoad + HideFlags.DontSave，在 Editor 下可能跨 Play 泄漏为编辑态对象，
        /// 实例字段会带着陈旧值存活；任何"已检查过"类的守卫都会把新 Play 的启动静默吞掉。
        /// </summary>
        public void StartFromSources()
        {
#if AXU_TEST_AGENT
            if (_isRunning) return;

            BuiltInCapabilities.RegisterAll();

            var launch = ReadEditorBridge();
            if (launch == null)
            {
                var cli = ScenarioCli.Parse(Environment.GetCommandLineArgs());
                if (!string.IsNullOrEmpty(cli.SuitePath)) launch = FromCliOptions(cli);
            }

            if (launch == null) return; // 无启动来源：静默待命
            StartWithLaunchOptions(launch);
#endif
        }

        /// <summary>以给定启动参数开始一次运行（AxuTestAgent.RunSuite API 路径）；define 关闭时返回 false。</summary>
        public bool StartWithLaunchOptions(ScenarioLaunchOptions launch)
        {
#if AXU_TEST_AGENT
            if (launch == null || string.IsNullOrEmpty(launch.SuitePath))
            {
                Debug.LogWarning("[AxuTestAgent] 启动参数缺少 SuitePath，忽略");
                return false;
            }

            if (_isRunning)
            {
                Debug.LogWarning("[AxuTestAgent] 已有 suite 在运行，忽略新的启动请求");
                return false;
            }

            StartCoroutine(RunAll(launch));
            return true;
#else
            return false; // define 未开启：不产生激活行为
#endif
        }

        /// <summary>获取或创建常驻宿主（名 AxuTestAgentHost，DontDestroyOnLoad + DontSave）。</summary>
        public static AgentRunnerHost EnsureHost()
        {
            var existing = GameObject.Find(HostObjectName);
            if (existing != null)
            {
                var host = existing.GetComponent<AgentRunnerHost>();
                if (host != null) return host;
            }

            var go = new GameObject(HostObjectName);
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            return go.AddComponent<AgentRunnerHost>();
        }

#if AXU_TEST_AGENT
        /// <summary>读取 Editor 桥文件 Temp/axu_pending_suite.json；读到后立即删除再返回启动参数（仅 Editor 生效）。</summary>
        static ScenarioLaunchOptions ReadEditorBridge()
        {
            var bridgePath = AgentPaths.EditorBridgeFilePath;
            if (string.IsNullOrEmpty(bridgePath) || !File.Exists(bridgePath)) return null;

            string json;
            try
            {
                json = File.ReadAllText(bridgePath);
                File.Delete(bridgePath); // 立即删除，防止重复消费
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AxuTestAgent] 桥文件读取/删除失败：" + ex.Message);
                return null;
            }

            try
            {
                var root = JObject.Parse(json);
                var suitePath = root["suitePath"] != null ? root["suitePath"].ToString() : null;
                if (string.IsNullOrWhiteSpace(suitePath)) return null;

                var launch = new ScenarioLaunchOptions { SuitePath = suitePath, Source = "editor-bridge" };

                if (root["scenarioIds"] is JArray ids)
                {
                    foreach (var id in ids)
                    {
                        if (!string.IsNullOrWhiteSpace(id != null ? id.ToString() : null))
                            launch.ScenarioIds.Add(id.ToString());
                    }
                }

                var quitOnDone = root["quitOnDone"] != null && root["quitOnDone"].Type == JTokenType.Boolean
                    ? (bool)root["quitOnDone"]
                    : false;
                launch.QuitOnDone = Application.isEditor ? false : quitOnDone; // Editor 源下一律不退出

                // 基线保存目录（Phase 2）：与 CLI -axuBaselineSave 等价的 Editor 通道
                if (root["baselineSavePath"] != null)
                {
                    var save = root["baselineSavePath"].ToString();
                    if (!string.IsNullOrWhiteSpace(save)) launch.BaselineSavePath = save;
                }
                return launch;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AxuTestAgent] 桥文件解析失败：" + ex.Message);
                return null;
            }
        }

        /// <summary>CLI 解析结果 → 合并启动参数。</summary>
        static ScenarioLaunchOptions FromCliOptions(ScenarioCliOptions cli)
        {
            return new ScenarioLaunchOptions
            {
                SuitePath = cli.SuitePath,
                ScenarioIds = cli.ScenarioIds != null ? new List<string>(cli.ScenarioIds) : new List<string>(),
                ReportPath = cli.ReportPath,
                ArtifactsDir = cli.ArtifactsDir,
                QuitOnDone = cli.QuitOnDone ?? false,
                WarnAsFail = cli.WarnAsFail ?? false,
                Seed = cli.Seed,
                OverlayHint = cli.OverlayHint,
                BaselineSavePath = cli.BaselineSavePath,
                Source = "cli",
            };
        }

        /// <summary>加载 suite；schema 为单 scenario 时包装成单条 suite。</summary>
        static SuiteDef LoadSuiteFromPath(string path)
        {
            try
            {
                return SuiteLoader.LoadSuite(path);
            }
            catch (AxuSchemaException ex) when (ex.ActualSchema == SchemaNames.Scenario)
            {
                var scenario = SuiteLoader.LoadScenario(path);
                return new SuiteDef
                {
                    Id = scenario.Id,
                    Scenarios = new List<SuiteScenarioRef> { new SuiteScenarioRef { Inline = scenario } },
                };
            }
        }

        /// <summary>主流程：加载 → 逐条执行 → 汇总报告/退出码/状态文件 →（Player 且 QuitOnDone）退出。</summary>
        IEnumerator RunAll(ScenarioLaunchOptions launch)
        {
            _isRunning = true;
            var suiteIdFallback = AgentPaths.SanitizeFileName(SuiteIdFallbackFromPath(launch.SuitePath));
            var reportPath = string.IsNullOrEmpty(launch.ReportPath)
                ? AgentPaths.DefaultReportPath(suiteIdFallback)
                : launch.ReportPath;
            var startedUtc = DateTime.UtcNow;

            try
            {
                // ---- 阶段 A：加载与解析（无协程让步，可安全 try/catch）----
                SuiteDef suite;
                List<ResolvedScenario> resolved;
                try
                {
                    suite = LoadSuiteFromPath(launch.SuitePath);
                    resolved = SuiteLoader.Resolve(suite, launch.ScenarioIds, ResolveSuiteDirectory(launch.SuitePath));
                }
                catch (Exception ex)
                {
                    FailInfrastructure(suiteIdFallback, startedUtc, reportPath, "suite 加载/解析失败：" + ex.Message);
                    yield break;
                }

                var settings = suite.Settings ?? new SuiteSettings();
                var warnAsFail = settings.WarnAsFail || launch.WarnAsFail;
                var stopOnFailure = settings.StopOnFailure;
                var timeoutSec = settings.ScenarioTimeoutSec ?? DefaultScenarioTimeoutSec;

                ScenarioStatusFile.WriteRunning(suite.Id, startedUtc, 0, resolved.Count, reportPath);

                // artifacts 目录（Phase 3）：launch 未指定时落到报告同目录 artifacts/，供 runner 截图等产出
                var artifactsDir = string.IsNullOrEmpty(launch.ArtifactsDir)
                    ? AgentPaths.DefaultArtifactsDir(reportPath)
                    : launch.ArtifactsDir;

                // ---- 阶段 B：逐条顺序执行（RunSingle 内部已兜底一切 runner 异常）----
                var results = new List<ScenarioReportResult>();
                var failureSeen = false;
                foreach (var scenario in resolved)
                {
                    if (stopOnFailure && failureSeen)
                    {
                        AgentLog.Instance.Warning($"stopOnFailure 生效：跳过剩余 scenario（下一个为 {scenario.Id}）");
                        break;
                    }

                    var handle = new ScenarioRunHandle(scenario.Id);
                    var result = new ScenarioReportResult
                    {
                        ScenarioId = scenario.Id,
                        Capability = scenario.Capability,
                        Unbudgeted = scenario.Budgets == null || scenario.Budgets.Count == 0,
                    };

                    // 通用参数（DOC §4 保留键）在度量会话之前应用；finally 保证超时/取消中断也还原。
                    // 用 try/finally 而非 try/catch：C# 迭代器不允许 yield 位于带 catch 的 try 内。
                    var envSnapshot = AgentEnvironment.Apply(scenario.Params, launch.Seed, AgentLog.Instance);
                    try
                    {
                        var metricsSession = StartMetricSession(scenario, handle, artifactsDir, envSnapshot.AppliedSeed);
                        var scenarioTimeoutSec = ResolveScenarioTimeoutSec(scenario.Params, timeoutSec);
                        yield return StartCoroutine(RunSingle(scenario, handle, scenarioTimeoutSec, metricsSession, artifactsDir, envSnapshot.AppliedSeed));
                        FinalizeMetrics(scenario, handle, result, metricsSession);
                    }
                    finally
                    {
                        AgentEnvironment.Restore(envSnapshot, AgentLog.Instance);
                    }

                    // runner 产出的工件：转相对报告目录路径写入报告 artifacts（转不动的保留原路径）
                    foreach (var artifact in handle.Artifacts)
                    {
                        var formatted = FormatArtifactRelative(artifact, reportPath);
                        if (!string.IsNullOrEmpty(formatted)) result.Artifacts.Add(formatted);
                    }

                    result.Status = MapStatus(handle.Status);
                    result.DurationMs = handle.ElapsedMilliseconds;
                    result.Findings.AddRange(handle.Findings);
                    results.Add(result);
                    if (result.Status == "fail" || result.Status == "error") failureSeen = true;

                    AgentLog.Instance.Info(
                        $"scenario \"{scenario.Id}\" 完成：{result.Status}（{result.DurationMs}ms，findings={result.Findings.Count}）");
                    ScenarioStatusFile.WriteRunning(suite.Id, startedUtc, results.Count, resolved.Count, reportPath);
                }

                // ---- 阶段 C：汇总、报告、退出码（无协程让步，可安全 try/catch）----
                var summary = new ReportSummary();
                foreach (var result in results)
                {
                    summary.Total++;
                    if (result.Status == "pass") summary.Passed++;
                    else if (result.Status == "warn") summary.Warned++;
                    else summary.Failed++;
                }

                ScenarioExitCode exitCode;
                if (summary.Failed > 0) exitCode = ScenarioExitCode.Failures;
                else if (summary.Warned > 0) exitCode = warnAsFail ? ScenarioExitCode.Failures : ScenarioExitCode.WarningsOnly;
                else exitCode = ScenarioExitCode.Success;

                var report = new ScenarioReport
                {
                    SuiteId = suite.Id,
                    StartedAt = FormatUtc(startedUtc),
                    EndedAt = FormatUtc(DateTime.UtcNow),
                    Environment = ScenarioReportWriter.CollectEnvironment(),
                    Summary = summary,
                    ExitCode = exitCode,
                    Results = results,
                };

                string finalPath;
                try
                {
                    finalPath = ScenarioReportWriter.WriteReport(report, reportPath);
                }
                catch (Exception ex)
                {
                    FailInfrastructure(suite.Id, startedUtc, reportPath, "报告写盘失败：" + ex.Message);
                    yield break;
                }

                if (!string.IsNullOrEmpty(launch.BaselineSavePath))
                {
                    try
                    {
                        SaveBaselines(results, launch.BaselineSavePath);
                    }
                    catch (Exception ex)
                    {
                        AgentLog.Instance.Warning("基线保存失败（不影响报告）：" + ex.Message);
                    }
                }

                _exitCode = (int)exitCode; // Player 退出码经 Quit(int) 携带；Editor 无进程退出码语义
                ScenarioStatusFile.WriteCompleted(suite.Id, startedUtc, results.Count, resolved.Count, finalPath, summary, (int)exitCode);
                AgentLog.Instance.Info(
                    $"suite \"{suite.Id}\" 结束：{summary.Passed} pass / {summary.Warned} warn / {summary.Failed} fail；退出码 {(int)exitCode}；报告 {finalPath}");
            }
            finally
            {
                _isRunning = false;
                // QuitOnDone 退出码=判定；Editor 内永不 Quit
                if (launch.QuitOnDone && !Application.isEditor) Application.Quit(_exitCode);
            }
        }

        /// <summary>
        /// 执行单个 scenario：能力分发 → Runner 协程 + 超时看门狗 + 异常兜底（度量节拍由 Supervise 驱动）。
        /// artifactsDir/seed 透传进 ScenarioContext（Phase 3：runner 截图落点与可复现种子）。
        /// </summary>
        IEnumerator RunSingle(ResolvedScenario scenario, ScenarioRunHandle handle, double timeoutSec,
            MetricSession metricsSession, string artifactsDir, int? seed)
        {
            if (!AxuCapabilityRegistry.TryGet(scenario.Capability, out var capability))
            {
                handle.Status = RunStatus.Error;
                handle.AddFinding(FindingSeverity.Error, "CAPABILITY_MISSING",
                    $"capability \"{scenario.Capability}\" 未注册（scenario \"{scenario.Id}\"）");
                yield break;
            }

            IScenarioRunner runner = null;
            try
            {
                runner = capability.CreateRunner(new ScenarioContext(handle, AgentLog.Instance, artifactsDir, seed));
            }
            catch (Exception ex)
            {
                Fault(handle, "RUNTIME_EXCEPTION", ex);
            }

            IEnumerator execution = null;
            if (runner != null)
            {
                try
                {
                    execution = runner.Run(scenario, handle);
                }
                catch (Exception ex)
                {
                    Fault(handle, "RUNTIME_EXCEPTION", ex);
                }
            }

            if (runner == null || execution == null) yield break;
            // 显式 StartCoroutine：保证 Supervise 的每步 yield（含 WaitForSecondsRealtime 段）真正进入
            // Unity 协程调度——裸 yield return IEnumerator 的展开语义不可靠，实测等待会被吞掉。
            yield return StartCoroutine(Supervise(execution, handle, timeoutSec, metricsSession, ResolveSampleIntervalMs(scenario)));
        }

        /// <summary>
        /// 监督执行循环：逐步推进 Runner 迭代器；嵌套迭代器（如 AgentWait 分段等待）手动代步，
        /// 其产出的等待指令交给 Unity（≤0.25s 段），每段结束复查超时/取消——保证看门狗按真实时间介入。
        /// 度量节拍（Phase 2）：外层/嵌套两处循环每帧开头驱动 MetricSession.OnFrame（同帧去重），
        /// 并按 sampleIntervalMs 累计驱动 OnInterval——采样节拍由宿主逐帧推进，不依赖 YieldInstruction 计时。
        /// </summary>
        IEnumerator Supervise(IEnumerator execution, ScenarioRunHandle handle, double timeoutSec,
            MetricSession metricsSession, double sampleIntervalMs)
        {
            var stop = false;
            var lastMetricsTime = -1d; // 同帧去重：外层/嵌套循环都可能在同一帧走到度量推进
            var intervalAccumulator = 0.0;

            void AdvanceMetrics()
            {
                if (metricsSession == null) return;
                var now = Time.realtimeSinceStartupAsDouble;
                if (now <= lastMetricsTime) return;
                lastMetricsTime = now;

                metricsSession.OnFrame(now, Time.unscaledDeltaTime);
                intervalAccumulator += Time.unscaledDeltaTime;
                if (intervalAccumulator * 1000.0 >= sampleIntervalMs)
                {
                    intervalAccumulator = 0.0;
                    metricsSession.OnInterval(now);
                }
            }

            while (!stop)
            {
                AdvanceMetrics();

                if (handle.CancelRequested || IsTimedOut(handle, timeoutSec))
                {
                    if (!handle.CancelRequested) Timeout(handle, timeoutSec);
                    stop = true;
                    break;
                }

                bool moved;
                try
                {
                    moved = execution.MoveNext();
                }
                catch (Exception ex)
                {
                    Fault(handle, "RUNTIME_EXCEPTION", ex);
                    stop = true;
                    break;
                }

                if (!moved)
                {
                    stop = true;
                    break;
                }

                var current = execution.Current;
                if (current is IEnumerator nested)
                {
                    // 嵌套迭代器：手动代步（不递归），段间复查看门狗
                    while (true)
                    {
                        AdvanceMetrics();

                        if (handle.CancelRequested || IsTimedOut(handle, timeoutSec))
                        {
                            if (!handle.CancelRequested) Timeout(handle, timeoutSec);
                            stop = true;
                            break;
                        }

                        bool nestedMoved;
                        try
                        {
                            nestedMoved = nested.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            Fault(handle, "RUNTIME_EXCEPTION", ex);
                            stop = true;
                            break;
                        }

                        if (!nestedMoved) break;

                        var nestedCurrent = nested.Current;
                        if (nestedCurrent != null && !(nestedCurrent is IEnumerator))
                            yield return nestedCurrent; // 等待指令交给 Unity（WaitForSecondsRealtime 段）
                        else
                            yield return null;
                    }
                }
                else if (current != null)
                {
                    yield return current; // 其余指令交 Unity（注：长等待指令期间看门狗不介入）
                }
                else
                {
                    yield return null; // 帧推进
                }
            }

            // 自然完成且 Runner 未定稿状态：无错即 Passed
            if (!handle.CancelRequested && handle.Status == RunStatus.Running)
                handle.Status = RunStatus.Passed;
        }

        /// <summary>看门狗超时：标记取消并置 Error + RUNTIME_TIMEOUT 结论。</summary>
        static void Timeout(ScenarioRunHandle handle, double timeoutSec)
        {
            handle.MarkCancelled($"看门狗超时（>{timeoutSec:0.#}s）");
            handle.Status = RunStatus.Error;
            handle.AddFinding(FindingSeverity.Error, "RUNTIME_TIMEOUT",
                $"scenario 执行超过 {timeoutSec:0.#}s，已被看门狗中断");
        }

        /// <summary>执行期异常兜底：置 Error + 结论（含异常消息）。</summary>
        static void Fault(ScenarioRunHandle handle, string code, Exception ex)
        {
            handle.Status = RunStatus.Error;
            handle.AddFinding(FindingSeverity.Error, code, $"{ex.GetType().Name}: {ex.Message}");
        }

        static bool IsTimedOut(ScenarioRunHandle handle, double timeoutSec)
        {
            return timeoutSec > 0 && handle.ElapsedSeconds > timeoutSec;
        }

        // ---------------------------------------------------------------- 度量层（Phase 2）

        /// <summary>params.collectors 未指定时的缺省采集器清单（DOC §4）。</summary>
        internal static readonly string[] DefaultCollectorIds = { "fps", "frameTime", "memory", "rendering", "gc" };

        /// <summary>sampleIntervalMs 缺省值（DOC §4）。</summary>
        internal const double DefaultSampleIntervalMs = 250.0;

        /// <summary>suite/scenario 文件所在目录（scenario.baseline 相对路径的解析基准）。</summary>
        static string ResolveSuiteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            return string.IsNullOrEmpty(dir) ? null : dir;
        }

        /// <summary>创建并启动度量会话（采集器清单取自 params；未知 id 记 COLLECTOR_UNKNOWN Finding）。
        /// artifactsDir/seed 填入会话侧 ScenarioContext（Phase 3）。</summary>
        static MetricSession StartMetricSession(ResolvedScenario scenario, ScenarioRunHandle handle,
            string artifactsDir, int? seed)
        {
            MetricSession session = null;
            try
            {
                session = MetricSession.Begin(ParseCollectorIds(scenario.Params), Time.realtimeSinceStartupAsDouble);
                foreach (var missing in session.MissingCollectorIds)
                {
                    handle.AddFinding(FindingSeverity.Warning, "COLLECTOR_UNKNOWN",
                        $"采集器 \"{missing}\" 未注册（scenario \"{scenario.Id}\"）");
                }
                session.StartCollectors(new ScenarioContext(handle, AgentLog.Instance, artifactsDir, seed));
            }
            catch (Exception ex)
            {
                handle.AddFinding(FindingSeverity.Error, "RUNTIME_EXCEPTION",
                    $"度量会话创建失败：{ex.GetType().Name}: {ex.Message}");
                MetricSession.EndCurrent();
                session = null;
            }
            return session;
        }

        /// <summary>解析 scenario 级超时覆盖（params.timeoutSec：double&gt;0 有效，否则回落 suite/默认值）。</summary>
        static double ResolveScenarioTimeoutSec(JObject parameters, double fallback)
        {
            var token = parameters != null ? parameters["timeoutSec"] : null;
            if (token == null) return fallback;

            try
            {
                var value = token.Value<double>();
                return value > 0 ? value : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        /// <summary>runner 工件完整路径 → 相对报告目录路径（报告目录内用正斜杠；转不出的保留原路径）。</summary>
        static string FormatArtifactRelative(string artifactFullPath, string reportPath)
        {
            if (string.IsNullOrEmpty(artifactFullPath)) return null;

            var reportDir = string.IsNullOrEmpty(reportPath)
                ? null
                : Path.GetDirectoryName(Path.GetFullPath(reportPath));
            if (string.IsNullOrEmpty(reportDir)) return artifactFullPath;

            try
            {
                var relative = Path.GetRelativePath(reportDir, Path.GetFullPath(artifactFullPath));
                if (string.IsNullOrEmpty(relative) || relative.StartsWith("..")) return artifactFullPath;
                return relative.Replace('\\', '/');
            }
            catch (Exception)
            {
                return artifactFullPath;
            }
        }

        /// <summary>解析 params.collectors（JArray→string[]；缺省全 5 个；custom 打点为会话内建通道恒可用）。</summary>
        static List<string> ParseCollectorIds(JObject parameters)
        {
            var ids = new List<string>();
            if (parameters != null && parameters["collectors"] is JArray array)
            {
                foreach (var token in array)
                {
                    var id = token != null ? token.ToString() : null;
                    if (!string.IsNullOrWhiteSpace(id)) ids.Add(id.Trim());
                }
            }
            if (ids.Count == 0) ids.AddRange(DefaultCollectorIds);
            ids.Add(MetricSession.CustomCollectorId);
            return ids;
        }

        /// <summary>解析 params.sampleIntervalMs（缺省 250；非法/非正值回落默认）。</summary>
        static double ResolveSampleIntervalMs(ResolvedScenario scenario)
        {
            var token = scenario.Params != null ? scenario.Params["sampleIntervalMs"] : null;
            if (token == null) return DefaultSampleIntervalMs;

            try
            {
                var value = token.Value<double>();
                return value > 0 ? value : DefaultSampleIntervalMs;
            }
            catch (Exception)
            {
                return DefaultSampleIntervalMs;
            }
        }

        /// <summary>
        /// runner 结束后的度量收尾：Complete → 采集器 Finding → BuildSummary → 预算判定 → 基线对比。
        /// status 只升级不覆盖错误态：budget/baseline fail → Failed（Pass/Warn 时），warn → Warn（Pass 时）。
        /// </summary>
        static void FinalizeMetrics(ResolvedScenario scenario, ScenarioRunHandle handle,
            ScenarioReportResult result, MetricSession session)
        {
            if (session == null) return;

            try
            {
                // 收尾补采：不足一个采样窗的短 scenario（如 @fast 200ms < 250ms 间隔）也保证 ≥1 个样本，
                // 否则预算会因指标缺失被误判 fail
                session.OnInterval(Time.realtimeSinceStartupAsDouble);
                session.CompleteCollectors(Time.realtimeSinceStartupAsDouble);
                session.CollectFindings(handle);
                result.Metrics = session.BuildSummary();
            }
            catch (Exception ex)
            {
                AgentLog.Instance.Warning($"scenario \"{scenario.Id}\" 度量汇总异常：{ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                MetricSession.EndCurrent();
            }

            // ---- 预算判定 ----
            if (scenario.Budgets != null && scenario.Budgets.Count > 0)
            {
                List<BudgetOutcome> outcomes;
                try
                {
                    outcomes = BudgetEvaluator.Evaluate(result.Metrics, scenario.Budgets);
                }
                catch (Exception ex)
                {
                    handle.AddFinding(FindingSeverity.Error, "RUNTIME_EXCEPTION",
                        $"预算判定异常：{ex.GetType().Name}: {ex.Message}");
                    outcomes = null;
                }

                if (outcomes != null)
                {
                    result.Budgets = outcomes;
                    foreach (var outcome in outcomes)
                    {
                        if (outcome == null) continue;

                        if (outcome.Missing)
                        {
                            handle.AddFinding(FindingSeverity.Error, "BUDGET_METRIC_MISSING",
                                $"预算指标 \"{outcome.Metric}\" 未产出（collector 或 stat 缺失），按 fail 判定");
                            UpgradeStatus(handle, RunStatus.Failed);
                        }
                        else if (outcome.Verdict == "fail")
                        {
                            handle.AddFinding(FindingSeverity.Error, "BUDGET_FAIL",
                                $"{outcome.Metric}={FormatValue(outcome.Actual)} 违反预算 {outcome.Op} {FormatThreshold(outcome)}");
                            UpgradeStatus(handle, RunStatus.Failed);
                        }
                        else if (outcome.Verdict == "warn")
                        {
                            handle.AddFinding(FindingSeverity.Warning, "BUDGET_WARN",
                                $"{outcome.Metric}={FormatValue(outcome.Actual)} 违反预算 {outcome.Op} {FormatThreshold(outcome)}（severity=warn）");
                            UpgradeStatus(handle, RunStatus.Warned);
                        }
                    }
                }
            }

            // ---- 基线对比（scenario.baseline 相对 suite/scenario 文件目录）----
            if (!string.IsNullOrEmpty(scenario.Baseline) && !string.IsNullOrEmpty(scenario.SuiteDirectory))
            {
                var baselinePath = Path.Combine(scenario.SuiteDirectory, scenario.Baseline);
                if (!File.Exists(baselinePath))
                {
                    // 首跑/基线未生成属正常状态：静默跳过，不产出 Finding、不影响判定
                    return;
                }
                try
                {
                    var baseline = BaselineFile.Load(baselinePath);
                    var entries = BaselineDiff.Compare(baseline.Metrics, BaselineDiff.Flatten(result.Metrics));
                    if (entries.Count > 0) result.BaselineDiff = entries;

                    foreach (var entry in entries)
                    {
                        if (entry.Verdict == "fail")
                        {
                            handle.AddFinding(FindingSeverity.Error, "BASELINE_REGRESSION",
                                $"{entry.Metric} 较基线劣化：base={FormatValue(entry.Base)} → current={FormatValue(entry.Current)}");
                            UpgradeStatus(handle, RunStatus.Failed);
                        }
                        else if (entry.Verdict == "warn")
                        {
                            handle.AddFinding(FindingSeverity.Warning, "BASELINE_DRIFT",
                                $"{entry.Metric} 较基线漂移：base={FormatValue(entry.Base)} → current={FormatValue(entry.Current)}");
                            UpgradeStatus(handle, RunStatus.Warned);
                        }
                    }
                }
                catch (Exception ex)
                {
                    handle.AddFinding(FindingSeverity.Warning, "BASELINE_LOAD_FAILED",
                        $"基线加载/对比失败：{scenario.Baseline}（{ex.Message}）");
                }
            }
        }

        /// <summary>status 只升不降：warn 仅从 Pass 升；fail 从 Pass/Warn 升；Error/Cancelled 等错误态不覆盖。</summary>
        static void UpgradeStatus(ScenarioRunHandle handle, RunStatus minimum)
        {
            if (minimum == RunStatus.Warned)
            {
                if (handle.Status == RunStatus.Passed) handle.Status = RunStatus.Warned;
            }
            else if (minimum == RunStatus.Failed)
            {
                if (handle.Status == RunStatus.Passed || handle.Status == RunStatus.Warned)
                    handle.Status = RunStatus.Failed;
            }
        }

        /// <summary>
        /// -axuBaselineSave 收尾：每个 result 的 metrics 扁平化为两段键，
        /// 写 &lt;目录&gt;/&lt;scenarioId&gt;.baseline.json（schema axu-baseline/1；scenario.baseline 字段即指向该文件）。
        /// </summary>
        static void SaveBaselines(List<ScenarioReportResult> results, string baselineSavePath)
        {
            var dir = ResolveBaselineDirectory(baselineSavePath);
            foreach (var result in results)
            {
                if (result == null || result.Metrics == null || string.IsNullOrEmpty(result.ScenarioId)) continue;

                var file = Path.Combine(dir, AgentPaths.SanitizeFileName(result.ScenarioId) + ".baseline.json");
                BaselineFile.Save(file, new BaselineFile
                {
                    ScenarioId = result.ScenarioId,
                    Metrics = BaselineDiff.Flatten(result.Metrics),
                });
                AgentLog.Instance.Info($"基线已保存：{file}");
            }
        }

        /// <summary>把 -axuBaselineSave 值解析为输出目录：.json 结尾视为文件路径（取其目录分量），
        /// 其余一律视为目录（不存在则创建——基线输出目录随用随建，不能因目录未建而误判为文件路径）。</summary>
        static string ResolveBaselineDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return ".";

            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var fileDir = Path.GetDirectoryName(path);
                return string.IsNullOrEmpty(fileDir) ? "." : fileDir;
            }

            try { Directory.CreateDirectory(path); }
            catch (Exception ex)
            {
                AgentLog.Instance.Warning($"基线输出目录创建失败（{path}）：{ex.Message}");
            }
            return path;
        }

        static string FormatValue(double? value)
        {
            return value != null ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "(missing)";
        }

        static string FormatValue(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string FormatThreshold(BudgetOutcome outcome)
        {
            if (outcome.Threshold != null)
                return outcome.Threshold.Value.ToString("0.######", CultureInfo.InvariantCulture);
            return outcome.Op == "range" ? "[valueMin..valueMax]" : "(missing)";
        }

        /// <summary>基础设施错误：写 error 报告 + 状态文件 + 退出码 3。</summary>
        void FailInfrastructure(string suiteId, DateTime startedUtc, string reportPath, string message)
        {
            AgentLog.Instance.Error(message);

            var report = new ScenarioReport
            {
                SuiteId = suiteId,
                StartedAt = FormatUtc(startedUtc),
                EndedAt = FormatUtc(DateTime.UtcNow),
                Environment = ScenarioReportWriter.CollectEnvironment(),
                Summary = new ReportSummary(),
                ExitCode = ScenarioExitCode.InfrastructureError,
                Results = new List<ScenarioReportResult>(),
            };

            string finalPath = null;
            try
            {
                finalPath = ScenarioReportWriter.WriteReport(report, reportPath);
            }
            catch (Exception ex)
            {
                AgentLog.Instance.Error("错误报告写盘失败：" + ex.Message);
            }

            _exitCode = (int)ScenarioExitCode.InfrastructureError; // QuitOnDone 退出时携带；报告/状态文件为消费端事实来源
            ScenarioStatusFile.WriteError(suiteId, startedUtc, finalPath);
        }

        /// <summary>RunStatus → 报告 status 串（pass/warn/fail/error；Running/Cancelled 归 error）。</summary>
        static string MapStatus(RunStatus status)
        {
            switch (status)
            {
                case RunStatus.Passed: return "pass";
                case RunStatus.Warned: return "warn";
                case RunStatus.Failed: return "fail";
                default: return "error";
            }
        }

        static string SuiteIdFallbackFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "unknown-suite";
            var stem = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrEmpty(stem) ? "unknown-suite" : stem;
        }

        static string FormatUtc(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }
#endif
    }
}
