using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuTestFramework.Agent.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Editor 内驱动 Agent Runner 跑 suite 的可编程服务（Phase 4：菜单与 pipeline 命令共用）。
    /// <para>运行链路（与 <see cref="ScenarioRunMenu"/> 手动路径一致，桥文件契约见其 XML 注释）：
    /// 写桥文件 Temp/axu_pending_suite.json → 进 Play Mode → Agent（#if AXU_TEST_AGENT 的
    /// AgentBootstrap/AgentRunnerHost）读取并执行 → 运行状态写 Temp/axu_scenario_status.json。</para>
    /// <para>完成回调（域重载安全）：进 Play 会触发域重载，任何内存态等待者都会被销毁——改为
    /// [InitializeOnLoad] 的看门狗轮询"命令运行标记文件"（<see cref="WatchFilePath"/>，仅 pipeline
    /// 命令路径写入）与状态文件；terminal（completed/error）时先把报告指标追加进趋势 CSV，再退出
    /// Play Mode。手动菜单路径不写标记文件，行为不变（跑完停在 Play，由用户退出）。</para>
    /// </summary>
    [InitializeOnLoad]
    public static class ScenarioRunService
    {
        /// <summary>命令驱动运行的标记文件（Temp/axu_scenario_watch.json）：存在即看门狗接管完成收尾。</summary>
        public const string WatchFilePath = "Temp/axu_scenario_watch.json";

        /// <summary>scenario 运行状态文件完整路径（Temp/axu_scenario_status.json，Agent 侧 ScenarioStatusFile 写入）。</summary>
        public static string StatusFileFullPath => AxuTestReportPaths.ToFullPath(AgentPaths.StatusFilePath);

        /// <summary>桥文件完整路径（Temp/axu_pending_suite.json，Agent 侧读取后立即删除）。</summary>
        public static string BridgeFileFullPath => AxuTestReportPaths.ToFullPath(AgentPaths.EditorBridgeFilePath);

        /// <summary>running 状态的陈旧阈值（分钟）：与 axu_test_run 的语义一致。</summary>
        public const int RunningStatusStaleMinutes = 15;

        static DateTime _nextPollUtc = DateTime.MinValue;

        static ScenarioRunService()
        {
            EditorApplication.update += PollWatch;
        }

        /// <summary>
        /// 启动一次 Editor 内 PlayMode 运行（pipeline 命令路径）：预检 → 清理陈旧状态 → 写桥文件与
        /// 运行标记 → 写初始 running 状态 → 进 Play。看门狗在 terminal 时自动收尾（趋势入库 + 退 Play）。
        /// 必须在 Unity 主线程调用。
        /// </summary>
        public static bool StartEditorPlayRun(string suiteAbsolutePath, IEnumerable<string> scenarioIds, out string error)
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

            try
            {
                // 陈旧状态清理：上一轮异常中断留下的 running/completed 会让轮询方误判
                var statusPath = StatusFileFullPath;
                if (File.Exists(statusPath))
                    File.Delete(statusPath);

                WriteBridgeFile(suiteFullPath, scenarioIds);
                WriteText(WatchFilePath, JsonConvert.SerializeObject(new
                {
                    autoStopOnDone = true,
                    suitePath = suiteFullPath,
                    startedAtUtc = DateTime.UtcNow.ToString("o")
                }, Formatting.Indented));

                // 初始 running 状态（字段与 Agent 侧 ScenarioStatusFile 同构）：填补"进 Play 到 Agent 写状态"的空窗
                var initialStatus = new JObject
                {
                    ["status"] = "running",
                    ["suiteId"] = Path.GetFileNameWithoutExtension(suiteFullPath),
                    ["startedAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                    ["endedAtUtc"] = null,
                    ["completedCount"] = 0,
                    ["totalCount"] = 0,
                    ["reportPath"] = string.Empty,
                    ["summary"] = null,
                    ["exitCode"] = null
                };
                WriteText(AgentPaths.StatusFilePath, initialStatus.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                error = $"启动前文件准备失败: {ex.Message}";
                return false;
            }

            EditorApplication.isPlaying = true;
            Debug.Log($"[AxuTestFramework] scenario 运行已启动（Play Mode）：suite {suiteFullPath}——轮询 axu_scenario_status 至 completed/error。");
            return true;
        }

        /// <summary>
        /// 写桥文件（与 Agent 侧 ReadEditorBridge 的契约：suitePath / scenarioIds / quitOnDone=false）。
        /// 菜单与命令路径共用；必须在进 Play 前调用。
        /// </summary>
        public static void WriteBridgeFile(string suiteAbsolutePath, IEnumerable<string> scenarioIds)
        {
            var payload = new JObject
            {
                ["suitePath"] = suiteAbsolutePath,
                ["quitOnDone"] = false // Editor 源下一律不退出（Agent 侧同样强制）
            };

            var ids = scenarioIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (ids != null && ids.Count > 0)
                payload["scenarioIds"] = new JArray(ids);

            WriteText(AgentPaths.EditorBridgeFilePath, payload.ToString(Formatting.Indented));
        }

        /// <summary>是否有 scenario 运行在途（状态文件 running 且未越过陈旧阈值；供互斥闸检查）。</summary>
        public static bool IsScenarioRunInFlight()
        {
            var status = TryReadStatus();
            if (status == null || (string)status["status"] != "running")
                return false;

            if (DateTime.TryParse((string)status["startedAtUtc"], null, System.Globalization.DateTimeStyles.RoundtripKind, out var startedUtc))
            {
                if (DateTime.UtcNow - startedUtc.ToUniversalTime() > TimeSpan.FromMinutes(RunningStatusStaleMinutes))
                    return false;
            }
            return true;
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
        /// 轮询回调（EditorApplication.update 节流 0.5s）：标记文件存在且在 Play 中读到 terminal 状态 →
        /// 趋势入库 → 删标记 → 退 Play；标记存在但已不在 Play（手动停止/崩溃中断）→ 删标记并补写 aborted 状态。
        /// </summary>
        static void PollWatch()
        {
            if (DateTime.UtcNow < _nextPollUtc)
                return;
            _nextPollUtc = DateTime.UtcNow.AddMilliseconds(500);

            var watchFullPath = AxuTestReportPaths.ToFullPath(WatchFilePath);
            if (!File.Exists(watchFullPath))
                return;

            if (EditorApplication.isPlaying)
            {
                var status = TryReadStatus();
                var state = status?["status"]?.ToString();
                if (state != "completed" && state != "error")
                    return;

                // 先入库再退出（退 Play 触发域重载，之后的代码不可靠）
                var reportPath = status?["reportPath"]?.ToString();
                if (state == "completed" && !string.IsNullOrEmpty(reportPath))
                {
                    if (AxuScenarioTrendUtil.TryAppendReport(reportPath, out var suiteId, out var trendError))
                        Debug.Log($"[AxuTestFramework] 趋势已入库: {AxuScenarioTrendUtil.TrendCsvFullPath}（suite {suiteId}）");
                    else
                        Debug.LogWarning($"[AxuTestFramework] 趋势入库失败（不影响报告）: {trendError}");
                }

                DeleteIfExists(watchFullPath);
                EditorApplication.isPlaying = false;
                Debug.Log($"[AxuTestFramework] scenario 运行 {state}，已自动退出 Play Mode。");
            }
            else
            {
                // 标记在而 Play 不在：运行被手动停止或 Editor 异常中断
                DeleteIfExists(watchFullPath);
                MarkAborted();
            }
        }

        /// <summary>补写 aborted 状态（仅当状态文件仍为 running 时，避免覆盖 Agent 已写入的终态）。</summary>
        static void MarkAborted()
        {
            try
            {
                var status = TryReadStatus();
                if (status == null || (string)status["status"] != "running")
                    return;

                status["status"] = "error";
                status["endedAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                status["message"] = "Play Mode 在运行结束前退出（手动停止或 Editor 中断），本次 scenario 运行未完成。";
                WriteText(AgentPaths.StatusFilePath, status.ToString(Formatting.Indented));
                Debug.LogWarning("[AxuTestFramework] scenario 运行被中断（Play Mode 提前退出）。");
            }
            catch (Exception)
            {
                // 尽力而为
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

        static void DeleteIfExists(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception)
            {
                // 尽力而为
            }
        }
    }
}
