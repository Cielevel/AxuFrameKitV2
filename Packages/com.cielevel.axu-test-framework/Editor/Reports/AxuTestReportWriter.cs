using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(AxuTestFramework.Editor.AxuTestReportWriter))]

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 测试报告落盘器。经程序集级 <c>[assembly: TestRunCallback(typeof(...))]</c> 注册到 Unity Test Framework
    /// （UTF 的 TestRunCallbackListener 会扫描所有引用 UnityEngine.TestRunner 的程序集读取该属性），
    /// 无需外部驱动；域重载开/关两种形态下都会触发（M0 已验证）。
    /// <para>RunFinished 时遍历结果树填 <see cref="AxuTestReport"/>（schema <c>axu-test-report/1</c>）并序列化写
    /// <c>Library/AxuTestReports/{yyyyMMdd-HHmmss}-{mode}.json</c>（axu_test_run 的 out_path 可覆盖），随后把
    /// 完成状态写入 <see cref="AxuTestReportPaths.StatusFilePath"/> 并删除运行上下文文件。</para>
    /// <para>跨域重载设计：运行参数经 <see cref="AxuTestRunContext"/>（Temp 下的 JSON 文件）传递——域重载开启时
    /// PlayMode 的 RunFinished 发生在重载后的新域、静态字段已清空，磁盘文件是唯一可靠的参数载体；
    /// 域重载关闭时同样成立。RunStarted 时恢复 AxuCapture 的 artifacts 目录重定向（静态字段同样跨不过域重载）。</para>
    /// <para>注意：本接口是 UnityEngine.TestRunner 的 ITestRunCallback（参数为 NUnit 原生 ITest/ITestResult），
    /// 与 UnityEditor.TestTools.TestRunner.Api.ICallbacks（参数为 Adaptor）是两套通道。</para>
    /// </summary>
    public sealed class AxuTestReportWriter : ITestRunCallback
    {
        /// <summary>报告器版本（与包版本同步）。</summary>
        public const string ReporterVersion = "0.1.0";

        /// <summary>最近一次成功落盘的报告完整路径；本域内尚未写过报告时为 null。</summary>
        public static string LastReportPath { get; private set; }

        /// <summary>UTF 回调：运行开始。恢复跨域会丢失的运行期静态配置（artifacts 目录重定向、console 环形缓冲运行标记），
        /// 并启动运行心跳（长跑用例期间持续刷新状态文件，使互斥闸不把仍在运行的测试判为陈旧）。</summary>
        public void RunStarted(ITest testsToRun)
        {
            try
            {
                var context = AxuTestRunContext.TryRead();
                if (context != null && !string.IsNullOrEmpty(context.ArtifactsDirectory))
                    AxuCapture.ArtifactsDirectory = context.ArtifactsDirectory;
                AxuConsoleRing.BeginRun();
                AxuTestHeartbeat.Arm();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AxuTestReportWriter] RunStarted 恢复运行配置失败: {ex.Message}");
            }
        }

        /// <summary>UTF 回调：单个节点开始（本报告器不逐条记录，结果树在 RunFinished 一次性收集）。</summary>
        public void TestStarted(ITest test)
        {
        }

        /// <summary>UTF 回调：单个节点结束（本报告器不逐条记录，结果树在 RunFinished 一次性收集）。</summary>
        public void TestFinished(ITestResult result)
        {
        }

        /// <summary>UTF 回调：运行结束。填报告 → 落盘 → 写状态文件 → 删上下文。任何异常都不外抛（只记 Console）。</summary>
        public void RunFinished(ITestResult result)
        {
            AxuTestRunContext context = null;
            try
            {
                context = AxuTestRunContext.TryRead();

                var report = BuildReport(result, context);
                string reportFullPath = null;

                if (context == null || context.Report)
                {
                    reportFullPath = WriteReport(report, context);
                    LastReportPath = reportFullPath;
                }

                WriteCompletedStatus(report, reportFullPath, context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AxuTestReportWriter] 报告落盘失败: {ex}");
                WriteErrorStatus(ex.Message, context);
            }
            finally
            {
                AxuTestHeartbeat.Disarm();
                AxuTestRunContext.Delete();
            }
        }

        /// <summary>遍历结果树构建报告 POCO。</summary>
        private static AxuTestReport BuildReport(ITestResult result, AxuTestRunContext context)
        {
            var report = new AxuTestReport
            {
                StartedAt = context?.StartedAtUtc ?? ToUtcIso(result.StartTime),
                EndedAt = ToUtcIso(result.EndTime),
                Mode = context?.Mode ?? "unknown",
                Filter = context?.Filter,
                Summary = new ReportSummary
                {
                    Total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount,
                    Passed = result.PassCount,
                    Failed = result.FailCount,
                    Skipped = result.SkipCount,
                    Inconclusive = result.InconclusiveCount
                },
                Console = AxuConsoleRing.Snapshot()
            };

            var results = new List<ReportResult>();
            CollectLeafResults(result, results);
            if (results.Count > 0)
                report.Results = results;

            var artifacts = CollectArtifacts();
            if (artifacts.Count > 0)
                report.Artifacts = artifacts;

            report.Environment = new ReportEnvironment
            {
                UnityVersion = Application.unityVersion,
                PipelineVersion = GetPipelineVersion(),
                ReporterVersion = ReporterVersion
            };

            return report;
        }

        /// <summary>深度优先收集叶子测试结果（套件节点不计入）。</summary>
        private static void CollectLeafResults(ITestResult node, List<ReportResult> output)
        {
            if (node == null)
                return;

            if (!node.HasChildren)
            {
                output.Add(new ReportResult
                {
                    FullName = node.FullName,
                    Status = node.ResultState.ToString(),
                    DurationMs = Math.Round(node.Duration * 1000.0, 2),
                    Message = node.Message,
                    StackTrace = node.StackTrace,
                    Assembly = GetAssemblyName(node),
                    Categories = GetCategories(node)
                });
                return;
            }

            foreach (var child in node.Children)
                CollectLeafResults(child, output);
        }

        /// <summary>取结果所属程序集短名（取不到时为 null）。</summary>
        private static string GetAssemblyName(ITestResult result)
        {
            try
            {
                return result.Test?.TypeInfo?.Assembly?.GetName()?.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>取测试类别：沿 Parent 链收集 Category 属性（fixture 级类别会被用例继承）；无类别时为 null。</summary>
        private static List<string> GetCategories(ITestResult result)
        {
            try
            {
                var categories = CollectCategories(result.Test, null);
                if (categories.Count == 0)
                    return null;
                categories.Sort(StringComparer.Ordinal);
                return categories;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<string> CollectCategories(ITest test, List<string> accumulator)
        {
            var categories = accumulator ?? new List<string>();
            while (test != null)
            {
                var values = test.Properties[PropertyNames.Category];
                if (values != null)
                {
                    foreach (var value in values)
                    {
                        var category = value as string;
                        if (!string.IsNullOrEmpty(category) && !categories.Contains(category))
                            categories.Add(category);
                    }
                }
                test = test.Parent;
            }
            return categories;
        }

        /// <summary>把 AxuCapture 收集的产物映射为报告 artifacts 节。</summary>
        private static List<ReportArtifact> CollectArtifacts()
        {
            var captured = AxuCapture.CapturedPaths;
            if (captured == null || captured.Count == 0)
                return new List<ReportArtifact>();

            var artifacts = new List<ReportArtifact>(captured.Count);
            foreach (var path in captured)
            {
                artifacts.Add(new ReportArtifact
                {
                    Test = null,
                    Screenshot = path,
                    Note = null
                });
            }
            return artifacts;
        }

        /// <summary>序列化并写报告文件；返回完整物理路径。文件名冲突时追加 -1/-2 序号。</summary>
        private static string WriteReport(AxuTestReport report, AxuTestRunContext context)
        {
            string fullPath;
            if (context != null && !string.IsNullOrEmpty(context.OutPath))
            {
                var trimmed = context.OutPath.TrimEnd().Replace('/', Path.DirectorySeparatorChar);
                fullPath = AxuTestReportPaths.ToFullPath(context.OutPath);
                if (!trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    // 视为目录：把默认文件名放进去
                    Directory.CreateDirectory(fullPath);
                    fullPath = Path.Combine(fullPath, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{SanitizeMode(report.Mode)}.json");
                }
            }
            else
            {
                var directory = AxuTestReportPaths.ToFullPath(AxuTestReportPaths.ReportsDirectory);
                fullPath = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{SanitizeMode(report.Mode)}.json");
            }

            var suffix = 0;
            while (File.Exists(fullPath))
            {
                suffix++;
                fullPath = Path.Combine(
                    Path.GetDirectoryName(fullPath),
                    Path.GetFileNameWithoutExtension(fullPath) + "-" + suffix + ".json");
            }

            var directoryFinal = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryFinal))
                Directory.CreateDirectory(directoryFinal);

            var json = JsonConvert.SerializeObject(
                report, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(fullPath, json, new UTF8Encoding(false));
            return fullPath;
        }

        /// <summary>运行完成状态写入状态文件（供 axu_test_status / 看板窗口 / EditMode 同步等待读取）。
        /// mode=all 的 PlayMode 段把上下文里的 EditMode 段结论一并带上（跨域只有文件能传）。</summary>
        private static void WriteCompletedStatus(AxuTestReport report, string reportFullPath, AxuTestRunContext context)
        {
            AxuTestRunStatus.Write(new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Completed,
                Mode = report.Mode,
                StartedAtUtc = report.StartedAt,
                HeartbeatUtc = DateTime.UtcNow.ToString("o"),
                EndedAtUtc = report.EndedAt,
                ReportPath = reportFullPath,
                Summary = report.Summary,
                Assemblies = context?.Assemblies,
                PrecedingPhase = context?.PrecedingPhase,
            });
        }

        /// <summary>报告流程异常时的兜底状态（status=error，避免等待方轮询到超时）；
        /// 上下文可用时一并带上 mode 与前置阶段结论（mode=all 的第二段出错时，第一段结果不能丢）。</summary>
        private static void WriteErrorStatus(string message, AxuTestRunContext context = null)
        {
            AxuTestRunStatus.Write(new AxuTestRunStatus
            {
                Status = AxuTestRunStatus.Error,
                Mode = context?.Mode,
                StartedAtUtc = context?.StartedAtUtc,
                EndedAtUtc = DateTime.UtcNow.ToString("o"),
                Message = message,
                PrecedingPhase = context?.PrecedingPhase,
            });
        }

        /// <summary>mode 作文件名片段时的安全化（非字母数字/连字符替换为下划线）。</summary>
        private static string SanitizeMode(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return "unknown";
            var builder = new StringBuilder(mode.Length);
            foreach (var c in mode)
                builder.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return builder.ToString();
        }

        /// <summary>DateTime 转 UTC ISO 8601 字符串。</summary>
        private static string ToUtcIso(DateTime time)
        {
            return time.ToUniversalTime().ToString("o");
        }

        /// <summary>取 Unity.Pipeline 包版本（读其 package.json；未装 pipeline 包或读取失败时为 null）。</summary>
        private static string GetPipelineVersion()
        {
            try
            {
                var packageJsonPath = Path.Combine(
                    AxuTestReportPaths.ProjectRootFullPath, "Packages", "com.unity.pipeline", "package.json");
                if (!File.Exists(packageJsonPath))
                    return null;
                var package = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(packageJsonPath));
                return package != null && package.TryGetValue("version", out var version)
                    ? version?.ToString()
                    : null;
            }
            catch (Exception)
            {
                // 读取失败不影响报告
            }
            return null;
        }
    }
}
