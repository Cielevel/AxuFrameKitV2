using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Editor 内驱动 Agent Runner 跑 suite 的可编程服务（看板 / 菜单 / pipeline 命令共用同一条启动-收尾契约）。
    /// <para>运行链路（桥文件契约见 <see cref="WriteBridgeFile"/>）：
    /// 写运行记录 <see cref="ScenarioRunRecord.DefaultPath"/> + 桥文件 Temp/axu_pending_suite.json →
    /// 进 Play Mode → Agent（#if AXU_TEST_AGENT 的 AgentBootstrap/AgentRunnerHost）读取并执行 →
    /// 运行状态写 Temp/axu_scenario_status.json（含 <c>runId</c>/<c>heartbeatUtc</c>）。</para>
    /// <para>收尾契约（本类 <see cref="WatchFilePath"/> 看门狗，[InitializeOnLoad] 域重载安全）：
    /// ① Agent 写出终态（completed/error）→ 追加趋势 CSV → 删运行记录 → 自动退出 Play Mode；
    /// ② 用户手动停止（Play 在终态前退出）→ 补写 <see cref="AxuScenarioRunStatus.Aborted"/> 终态（带 reason）→ 删记录
    /// —— 界面因此显示"已中断"，而不是把上一次的报告当本次结果；
    /// ③ 记录存在但状态文件缺失/不属于本轮 → 同样按中断收尾（Agent 未接手，例如 define 未开）。</para>
    /// <para>三条入口的差别只在 <see cref="ScenarioRunRecord.Source"/>（用于诊断与日志），行为完全一致：
    /// 都在终态后自动退出 Play，都不需要用户手动退出。</para>
    /// </summary>
    [InitializeOnLoad]
    public static class ScenarioRunService
    {
        /// <summary>看门狗轮询间隔（毫秒）。</summary>
        public const int PollIntervalMs = 500;

        /// <summary>运行记录文件路径（Temp/axu_scenario_run.json；相对工程根）。</summary>
        public const string WatchFilePath = ScenarioRunRecord.DefaultPath;

        /// <summary>scenario 运行状态文件完整路径（Temp/axu_scenario_status.json，Agent 侧 ScenarioStatusFile 写入）。</summary>
        public static string StatusFileFullPath => AxuTestReportPaths.ToFullPath(AgentPaths.StatusFilePath);

        /// <summary>桥文件完整路径（Temp/axu_pending_suite.json，Agent 侧读取后立即删除）。</summary>
        public static string BridgeFileFullPath => AxuTestReportPaths.ToFullPath(AgentPaths.EditorBridgeFilePath);

        /// <summary>running 状态的陈旧阈值（分钟）：与 axu_test_run 的语义一致（按最后活动时间判定，非启动时间）。</summary>
        public const int RunningStatusStaleMinutes = AxuScenarioRunStatus.StaleMinutes;

        static DateTime _nextPollUtc = DateTime.MinValue;

        static ScenarioRunService()
        {
            EditorApplication.update += PollWatch;
        }

        /// <summary>
        /// 启动一次 Editor 内 PlayMode 运行（看板/菜单/pipeline 共用）：预检（Play 中 / define / 文件存在性 /
        /// 文件结构）→ 清陈旧状态 → 写运行记录 + 桥文件 + 初始 running 状态 → 进 Play。
        /// 看门狗在终态时自动收尾（趋势入库 + 删记录 + 退 Play）。必须在 Unity 主线程调用。
        /// </summary>
        /// <param name="source">发起方（<see cref="ScenarioRunRecord.SourceDashboard"/> 等，仅用于诊断/展示）。</param>
        public static bool StartEditorPlayRun(string suiteAbsolutePath, IEnumerable<string> scenarioIds,
            out string error, string source = ScenarioRunRecord.SourcePipeline)
        {
            error = null;

            if (EditorApplication.isPlaying)
            {
                error = "当前已处于 Play Mode。桥文件需在进入 Play 前写入才会被 Agent 读取——请先退出 Play 再发起运行。";
                return false;
            }

            if (!AgentDefineMenu.HasAgentDefine())
            {
                error = $"当前 BuildTargetGroup（{AgentDefineMenu.GetActiveNamedBuildTarget().TargetName}）的脚本符号" +
                        $"未开启 {AgentDefineMenu.DefineName}，Agent 入口不会参与编译，进 Play 也无法执行 suite。" +
                        "请先执行菜单 AxuTestFramework/Enable Test Agent 并等待重编译完成。";
                return false;
            }

            var suiteFullPath = AxuTestReportPaths.ToFullPath(suiteAbsolutePath ?? string.Empty);
            if (!File.Exists(suiteFullPath))
            {
                error = $"suite/scenario JSON 不存在: {suiteFullPath}";
                return false;
            }

            // 结构预检：文件坏（schema/空 scenarios/ref 缺字段）在进 Play 之前就报错，不把用户丢进必然失败的 Play。
            // 未解析的纯 ref 条目放行——代码态 scenario 由游戏运行时注册，Editor 侧无从预知。
            if (!TryPrevalidateSuiteFile(suiteFullPath, out var suiteId, out error))
                return false;

            ScenarioRunRecord record;
            try
            {
                record = ScenarioRunRecord.Create(suiteFullPath, scenarioIds, source);
            }
            catch (Exception ex)
            {
                error = $"运行记录构造失败: {ex.Message}";
                return false;
            }

            try
            {
                // 陈旧状态清理：上一轮异常中断留下的 running/completed 会让轮询方误判
                var statusPath = StatusFileFullPath;
                if (File.Exists(statusPath))
                    File.Delete(statusPath);

                // 顺序要紧：运行记录与初始状态先就位，桥文件最后写——桥文件是"进 Play 就会被 Agent 消费"
                // 的触发器，若在它之后抛异常会留下孤儿桥文件，让下一次（哪怕是用户手按的）Play 静默执行本 suite。
                ScenarioRunRecord.Write(record);

                // 初始 running 状态（字段与 Agent 侧 ScenarioStatusFile 同构）：填补"进 Play 到 Agent 写状态"的空窗
                var initialStatus = new JObject
                {
                    ["status"] = AxuScenarioRunStatus.Running,
                    ["suiteId"] = suiteId,
                    ["runId"] = record.RunId,
                    ["startedAtUtc"] = record.StartedAtUtc,
                    ["heartbeatUtc"] = record.StartedAtUtc,
                    ["endedAtUtc"] = null,
                    ["completedCount"] = 0,
                    ["totalCount"] = 0,
                    ["reportPath"] = string.Empty,
                    ["summary"] = null,
                    ["exitCode"] = null
                };
                WriteText(AgentPaths.StatusFilePath, initialStatus.ToString(Newtonsoft.Json.Formatting.Indented));

                WriteBridgeFile(suiteFullPath, scenarioIds, record.RunId);
            }
            catch (Exception ex)
            {
                ScenarioRunRecord.Delete();
                DeleteIfExists(BridgeFileFullPath);
                error = $"启动前文件准备失败: {ex.Message}";
                return false;
            }

            EditorApplication.isPlaying = true;
            Debug.Log($"[AxuTestFramework] scenario 运行已启动（{source}，Play Mode）：suite {suiteFullPath}，" +
                      "runId " + record.RunId + "——Agent 写出终态后自动退出 Play 并回显本次报告。");
            return true;
        }

        /// <summary>
        /// 写桥文件（与 Agent 侧 ReadEditorBridge 的契约：suitePath / scenarioIds / runId / quitOnDone=false）。
        /// 三条入口共用；必须在进 Play 前调用。
        /// </summary>
        public static void WriteBridgeFile(string suiteAbsolutePath, IEnumerable<string> scenarioIds, string runId = null)
        {
            var payload = new JObject
            {
                ["suitePath"] = suiteAbsolutePath,
                ["quitOnDone"] = false // Editor 源下一律不退出（Agent 侧同样强制）
            };

            if (!string.IsNullOrEmpty(runId))
                payload["runId"] = runId;

            var ids = scenarioIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (ids != null && ids.Count > 0)
                payload["scenarioIds"] = new JArray(ids);

            WriteText(AgentPaths.EditorBridgeFilePath, payload.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>
        /// 文件结构预检（进 Play 前）：schema 不符、scenarios 为空、内联定义非法、ref 缺字段等一律拦下；
        /// 纯 ref 指向运行时注册 scenario 的情形放行。单 scenario 文件按 axu-scenario/1 校验。
        /// <paramref name="suiteId"/> 回传文件声明的 id（初始状态文件用它与 Agent 写的 suite.Id 保持一致，
        /// 避免"进 Play 前后看板标题跳名"）。
        /// </summary>
        public static bool TryPrevalidateSuiteFile(string suiteFullPath, out string suiteId, out string error)
        {
            suiteId = null;
            error = null;
            try
            {
                SuiteDef suite;
                try
                {
                    suite = AxuJson.FromFile<SuiteDef>(suiteFullPath, SchemaNames.Suite);
                }
                catch (AxuSchemaException ex) when (ex.ActualSchema == SchemaNames.Scenario)
                {
                    // 单 scenario 文件：与 Agent 侧 LoadSuiteFromPath 的兼容分支同构
                    var scenario = AxuJson.FromFile<ScenarioDef>(suiteFullPath, SchemaNames.Scenario);
                    var scenarioErrors = scenario.Validate();
                    if (scenarioErrors.Count > 0)
                    {
                        error = $"scenario 校验失败：{suiteFullPath}；{string.Join("；", scenarioErrors.ToArray())}";
                        return false;
                    }
                    suiteId = scenario.Id;
                    return true;
                }

                var structural = new List<string>();
                var unresolvedRefs = new List<string>();
                suite.Validate(null, structural, unresolvedRefs);
                if (structural.Count > 0)
                {
                    error = $"suite 校验失败：{suiteFullPath}；{string.Join("；", structural.ToArray())}";
                    return false;
                }
                suiteId = suite.Id;
                return true;
            }
            catch (AxuSchemaException ex)
            {
                error = $"suite/scenario schema 不符：{suiteFullPath}；{ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                error = $"suite/scenario 解析失败：{suiteFullPath}；{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 文件结构预检（仅要错误信息的重载）。<paramref name="suiteId"/> 由主重载回传，此重载忽略。
        /// </summary>
        public static bool TryPrevalidateSuiteFile(string suiteFullPath, out string error)
        {
            return TryPrevalidateSuiteFile(suiteFullPath, out _, out error);
        }

        /// <summary>
        /// 是否有 scenario 运行在途（供互斥闸）：状态文件 running 且未陈旧（按最后活动时间判活），
        /// 或存在未收尾的运行记录。20 分钟以上的长跑只要持续心跳即视为在途。
        /// </summary>
        public static bool IsScenarioRunInFlight()
        {
            var now = DateTime.UtcNow;
            if (AxuScenarioRunStatus.IsInFlight(AxuScenarioRunStatus.TryRead(), now))
                return true;

            var record = ScenarioRunRecord.TryRead();
            return record != null && record.IsInFlight(now, EditorApplication.isPlaying);
        }

        /// <summary>读 scenario 运行状态文件原文；不存在返回 null。</summary>
        public static string TryReadStatusText()
        {
            var path = StatusFileFullPath;
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>读 scenario 运行状态文件为 JObject；不存在/解析失败返回 null。</summary>
        public static JObject TryReadStatus()
        {
            try
            {
                var text = TryReadStatusText();
                return text == null ? null : JObject.Parse(text);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ 看门狗（完成收尾，域重载安全）

        /// <summary>
        /// 轮询回调（EditorApplication.update 节流 0.5s，域重载后由静态构造重新挂载）：
        /// 运行记录存在且在 Play 中读到本轮终态 → 趋势入库 → 删记录 → 退 Play；
        /// Play 中但心跳已停（Agent 未接手/异常）→ 写 aborted 并退 Play（不把用户无限留在 Play）；
        /// 记录存在但已不在 Play（手动停止/崩溃中断）→ 补写 aborted 终态 → 删记录。
        /// </summary>
        static void PollWatch()
        {
            if (DateTime.UtcNow < _nextPollUtc)
                return;
            _nextPollUtc = DateTime.UtcNow.AddMilliseconds(PollIntervalMs);

            var record = ScenarioRunRecord.TryRead();
            if (record == null)
                return;

            var status = AxuScenarioRunStatus.TryRead();
            var matched = record.MatchesStatus(status);
            var now = DateTime.UtcNow;

            if (EditorApplication.isPlaying)
            {
                // 状态尚不属于本轮（Agent 还没接手写状态）→ 继续等
                if (!matched)
                    return;

                if (status.IsTerminal)
                {
                    FinishRecord(record, status);
                    return;
                }

                if (status.IsStale(now))
                {
                    // 心跳停了：Agent 未接手或中途异常。继续等只会让人以为"还在跑"，收尾并如实报中断。
                    MarkAborted(record,
                        $"运行心跳已停止超过 {AxuScenarioRunStatus.StaleMinutes} 分钟（Agent 未接手或异常中断），本次运行未完成。",
                        AxuScenarioRunStatus.ReasonInterrupted, exitPlay: true);
                }
                return;
            }

            // 已不在 Play：运行被手动停止或 Editor 异常中断
            if (record.IsStale(now))
            {
                // 陈旧残留（跨会话遗留）：清掉即可，不动状态文件（可能是别的运行留下的）
                ScenarioRunRecord.Delete();
                Debug.LogWarning("[AxuTestFramework] 清理陈旧的 scenario 运行记录（" + ScenarioRunRecord.DefaultPath + "）。");
                return;
            }

            // 刚发起 <StartGraceSeconds 的运行：可能仍在"进 Play"的转换窗口（域重载/场景装载）内，
            // 此时 isPlaying 尚未生效，不能据此判定被中断
            if (record.TryGetStartedUtc(out var startedAtUtc) &&
                now - startedAtUtc < TimeSpan.FromSeconds(AxuRunLiveness.StartGraceSeconds))
                return;

            if (!matched)
            {
                // Agent 未接手（define 未开/未写状态）或状态属于上一轮：补一条 aborted 终态，避免界面回显旧报告
                MarkAborted(record, status == null ? "未检测到运行状态" : "状态文件不属于本轮运行",
                    AxuScenarioRunStatus.ReasonAgentNotRun);
            }
            else if (!status.IsTerminal)
            {
                MarkAborted(record, "Play Mode 在运行结束前退出（手动停止或 Editor 中断），本次 scenario 运行未完成。",
                    AxuScenarioRunStatus.ReasonInterrupted);
            }
            else
            {
                // 终态已写出但 Play 已退出（手动停止正好发生在收尾前后）：补做趋势入库
                FinishRecord(record, status, exitPlay: false);
            }
        }

        /// <summary>
        /// 终态收尾：趋势入库（completed 且有报告）→ 删运行记录 →（需要时）退出 Play。
        /// 退 Play 会触发域重载，之后的代码不可靠，故退 Play 放在最后。
        /// </summary>
        static void FinishRecord(ScenarioRunRecord record, AxuScenarioRunStatus status, bool exitPlay = true)
        {
            if (status.IsCompleted && !string.IsNullOrEmpty(status.ReportPath))
            {
                if (AxuScenarioTrendUtil.TryAppendReport(status.ReportPath, out var suiteId, out var trendError))
                    Debug.Log($"[AxuTestFramework] 趋势已入库: {AxuScenarioTrendUtil.TrendCsvFullPath}（suite {suiteId}）");
                else
                    Debug.LogWarning($"[AxuTestFramework] 趋势入库失败（不影响报告）: {trendError}");
            }

            ScenarioRunRecord.Delete();

            if (exitPlay && EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log($"[AxuTestFramework] scenario 运行 {status.Status}（runId {record.RunId}），已自动退出 Play Mode。");
            }
            else
            {
                Debug.Log($"[AxuTestFramework] scenario 运行 {status.Status}（runId {record.RunId}）已收尾。");
            }
        }

        /// <summary>把当前状态改写为 aborted 终态（仅在状态仍属于本轮或缺失时；保留已完成终态不被覆盖）。
        /// <paramref name="exitPlay"/>：Play 中判中断时一并退出 Play（退 Play 触发域重载，故放在最后）。</summary>
        static void MarkAborted(ScenarioRunRecord record, string message, string reason, bool exitPlay = false)
        {
            try
            {
                var status = AxuScenarioRunStatus.TryRead();
                if (status != null && status.IsTerminal && record.MatchesStatus(status))
                    return; // Agent 已写出终态：中断判定不覆盖真实结论

                var terminal = new JObject
                {
                    ["status"] = AxuScenarioRunStatus.Aborted,
                    ["suiteId"] = status?.SuiteId ?? Path.GetFileNameWithoutExtension(record.SuitePath ?? string.Empty),
                    ["runId"] = record.RunId,
                    ["startedAtUtc"] = status?.StartedAtUtc ?? record.StartedAtUtc,
                    ["heartbeatUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                    ["endedAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                    ["completedCount"] = status?.CompletedCount ?? 0,
                    ["totalCount"] = status?.TotalCount ?? 0,
                    ["reportPath"] = string.Empty, // 关键：清空报告路径，界面据此不回显上一次的报告
                    ["summary"] = null,
                    ["exitCode"] = null,
                    ["reason"] = reason,
                    ["message"] = message
                };
                WriteText(AgentPaths.StatusFilePath, terminal.ToString(Newtonsoft.Json.Formatting.Indented));
                Debug.LogWarning($"[AxuTestFramework] scenario 运行已中断（{reason}）：{message}");
            }
            catch (Exception ex)
            {
                // 尽力而为：状态写不出也不拦记录清理
                Debug.LogWarning("[AxuTestFramework] 写中断状态失败: " + ex.Message);
            }
            finally
            {
                ScenarioRunRecord.Delete();
            }

            if (exitPlay && EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[AxuTestFramework] 已自动退出 Play Mode（本次运行无有效结果）。");
            }
        }

        /// <summary>写相对工程根的文本文件（UTF-8 无 BOM，目录随建）。</summary>
        static void WriteText(string relativePath, string content)
        {
            var fullPath = AxuTestReportPaths.ToFullPath(relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(fullPath, content, new System.Text.UTF8Encoding(false));
        }

        /// <summary>删除文件（不存在/占用时静默——清理路径不抛）。</summary>
        static void DeleteIfExists(string fullPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception)
            {
                // 尽力而为
            }
        }
    }
}
