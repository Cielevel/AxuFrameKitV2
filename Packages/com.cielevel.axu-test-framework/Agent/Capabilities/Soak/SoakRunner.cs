using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Reports;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Metrics;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace AxuTestFramework.Agent.Capabilities.Soak
{
    /// <summary>
    /// Soak 运行器（方案 §4.5）：长时浸泡采样 → 泄漏判定（memory.allocated 滑窗线性回归斜率）→
    /// FPS 衰减判定（后半程 p50 vs 前半程）→ 定期截图 → partial report 定期 flush（崩溃不丢前段数据，§9-9）。
    /// <para>等待一律走帧轮询真实时钟（<see cref="Time.realtimeSinceStartupAsDouble"/>，与
    /// <see cref="AgentWait"/> 同契约）：本 Editor 环境下 WaitForSecondsRealtime 等待契约失效，禁用。</para>
    /// </summary>
    public class SoakRunner : IScenarioRunner
    {
        /// <summary>滑窗回归最大样本数（约覆盖 20 个采样间隔；不足则全量）。</summary>
        public const int RegressionWindowSamples = 20;

        /// <summary>FPS 衰减判定的每半程最少样本数（不足则跳过判定并记 info finding）。</summary>
        public const int FpsMinSamplesPerHalf = 3;

        readonly ScenarioContext _context;

        public SoakRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;
            var startedUtc = DateTime.UtcNow;
            var startRealtime = Time.realtimeSinceStartupAsDouble;

            // ---- 泄漏/FPS 衰减判定的自有采样序列（与宿主采集器互不干扰）----
            var sampleTimesMin = new List<double>();   // 采样时刻（分钟，自 soak 开始）
            var allocatedMb = new List<double>();
            var reservedMb = new List<double>();
            var monoUsedMb = new List<double>();
            var fpsSamples = new List<double>();

            var artifactsDir = _context != null ? _context.ArtifactsDir : null;
            var sanitizedId = AgentPaths.SanitizeFileName(scenario.Id);

            // ---- partial flush：report 路径取自运行状态文件（宿主写入，runner 只读）----
            var reportMeta = TryReadStatusReportMeta();
            var partialPath = reportMeta != null ? PartialPathFor(reportMeta.Value.ReportPath) : null;

            using (var logWatch = new AgentLogWatch())
            {
                var durationSec = parameters.ClampedDurationMinutes * 60f;
                var sampleInterval = parameters.ClampedSampleIntervalSec;
                var screenshotEverySec = parameters.ClampedScreenshotEveryMinutes * 60f;
                var flushEverySec = parameters.ClampedFlushEveryMinutes * 60f;
                var nextSampleAt = sampleInterval;
                var nextScreenshotAt = screenshotEverySec > 0f ? screenshotEverySec : double.MaxValue;
                var nextFlushAt = flushEverySec;
                var endRealtime = startRealtime + durationSec;

                log?.Info($"soak 开始：duration={parameters.ClampedDurationMinutes:0.##}min，" +
                    $"interval={sampleInterval:0.##}s，leakSlope>{parameters.LeakSlopeMbPerMin:0.##}MB/min");

                while (Time.realtimeSinceStartupAsDouble < endRealtime)
                {
                    if (handle != null && handle.CancelRequested) yield break; // 取消/超时中断：partial 文件保留

                    // ---- 周期截图（先于采样等待判定，避免整段跳过）----
                    var now = Time.realtimeSinceStartupAsDouble - startRealtime;
                    if (now >= nextScreenshotAt && !string.IsNullOrEmpty(artifactsDir))
                    {
                        var fileName = $"{sanitizedId}_soak_{(int)(nextScreenshotAt / 60f):000}m.png";
                        var path = AgentCapture.ResolveArtifactsPath(artifactsDir, fileName);
                        yield return AgentCapture.CaptureScreenshot(path);
                        handle?.Artifacts.Add(path);
                        nextScreenshotAt += screenshotEverySec;
                    }

                    // ---- 周期 partial flush（截图协程已消耗时间，重估当前时刻）----
                    now = Time.realtimeSinceStartupAsDouble - startRealtime;
                    if (now >= nextFlushAt && reportMeta != null)
                    {
                        FlushPartialReport(reportMeta.Value, handle, scenario, startedUtc, logWatch.ErrorCount);
                        nextFlushAt += flushEverySec;
                    }

                    // ---- 到点采样 ----
                    now = Time.realtimeSinceStartupAsDouble - startRealtime;
                    if (now >= nextSampleAt)
                    {
                        TakeSample(startRealtime, sampleTimesMin, allocatedMb, reservedMb, monoUsedMb,
                            fpsSamples, logWatch.ErrorCount, parameters);
                        nextSampleAt += sampleInterval;
                        continue; // 立即重估循环条件（截图/flush 时刻可能同时到期）
                    }

                    yield return null;
                }
            }

            // ---- 判定与收尾 ----
            EvaluateLeak(parameters, handle, sampleTimesMin, allocatedMb, artifactsDir, sanitizedId);
            EvaluateFpsDecay(parameters, handle, fpsSamples);
            WriteRegressionCsv(artifactsDir, sanitizedId, sampleTimesMin, allocatedMb, reservedMb,
                monoUsedMb, fpsSamples, handle);

            if (handle != null) handle.Status = RunStatus.Passed;

            // 正常结束：宿主即将写最终完整报告，删除 partial 文件
            if (partialPath != null)
            {
                TryDeleteQuietly(partialPath);
            }
            else if (reportMeta == null)
            {
                handle?.AddFinding(FindingSeverity.Info, "SOAK_PARTIAL_FLUSH_SKIPPED",
                    "未能从状态文件解析报告路径，本次 soak 未做 partial report 定期 flush");
            }

            log?.Info($"soak 完成：samples={sampleTimesMin.Count}，" +
                $"durationMs={(Time.realtimeSinceStartupAsDouble - startRealtime) * 1000:0}");
        }

        // -------------------------------------------------------------- 采样

        /// <summary>采一个样本：可选 GC.Collect 后取内存三项 + 帧率 + 错误累计，并把曲线写入 metrics（custom gauge 通道）。</summary>
        static void TakeSample(double startRealtime, List<double> timesMin, List<double> allocated,
            List<double> reserved, List<double> monoUsed, List<double> fps, int errorCount, SoakParams parameters)
        {
            if (parameters.GcCollectBeforeSample)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            timesMin.Add((Time.realtimeSinceStartupAsDouble - startRealtime) / 60.0);

            var allocatedNow = Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0);
            var reservedNow = Profiler.GetTotalReservedMemoryLong() / (1024.0 * 1024.0);
            var monoNow = Profiler.GetMonoUsedSizeLong() / (1024.0 * 1024.0);
            allocated.Add(allocatedNow);
            reserved.Add(reservedNow);
            monoUsed.Add(monoNow);

            var dt = Time.unscaledDeltaTime;
            fps.Add(dt > 0f ? 1f / dt : 0f);

            // 错误累计曲线记入 metrics（custom.gauge.*，累计值取 max 即最终总量）
            var session = MetricSession.Current;
            if (session != null)
            {
                session.RecordGauge("soak.memory.allocatedMb", allocatedNow);
                session.RecordGauge("soak.memory.reservedMb", reservedNow);
                session.RecordGauge("soak.memory.monoUsedMb", monoNow);
                session.RecordGauge("soak.fps", fps.Count > 0 ? fps[fps.Count - 1] : 0);
                session.RecordGauge("soak.errors.total", errorCount);
            }
        }

        // -------------------------------------------------------------- 判定

        /// <summary>泄漏判定：allocated 序列滑窗最小二乘斜率（MB/min）超阈值 → warn finding。</summary>
        void EvaluateLeak(SoakParams parameters, ScenarioRunHandle handle, List<double> timesMin,
            List<double> allocated, string artifactsDir, string sanitizedId)
        {
            if (handle == null || timesMin.Count < 2) return;

            var windowStart = Math.Max(0, timesMin.Count - RegressionWindowSamples);
            var count = timesMin.Count - windowStart;
            var windowValues = new double[count];
            var windowTimes = new double[count];
            var baseMinutes = timesMin[windowStart];
            for (var i = 0; i < count; i++)
            {
                windowTimes[i] = (timesMin[windowStart + i] - baseMinutes) * 60.0; // 回归按秒
                windowValues[i] = allocated[windowStart + i];
            }

            var slopeMbPerMin = MetricStats.SlopePerSecond(windowValues, windowTimes) * 60.0;
            if (slopeMbPerMin > parameters.LeakSlopeMbPerMin)
            {
                handle.AddFinding(FindingSeverity.Warning, "SOAK_MEMORY_LEAK_SUSPECT",
                    $"泄漏嫌疑：memory.allocated 最近 {count} 个样本的回归斜率 {slopeMbPerMin:0.##} MB/min " +
                    $"超过阈值 {parameters.LeakSlopeMbPerMin:0.##} MB/min（回归数据见 artifacts CSV）");
            }
        }

        /// <summary>FPS 衰减判定：后半程 p50 比前半程低超阈值百分比 → warn finding。</summary>
        static void EvaluateFpsDecay(SoakParams parameters, ScenarioRunHandle handle, List<double> fps)
        {
            if (handle == null || fps.Count < FpsMinSamplesPerHalf * 2)
            {
                if (handle != null && parameters.FpsDecayPercent > 0f && fps.Count > 0)
                    handle.AddFinding(FindingSeverity.Info, "SOAK_FPS_DECAY_SKIPPED",
                        $"FPS 样本不足（{fps.Count} < {FpsMinSamplesPerHalf * 2}），跳过后半程衰减判定");
                return;
            }

            var half = fps.Count / 2;
            var firstHalf = new double[half];
            var secondHalf = new double[half];
            for (var i = 0; i < half; i++)
            {
                firstHalf[i] = fps[i];
                secondHalf[i] = fps[half + i];
            }

            var firstP50 = MetricStats.Summarize(firstHalf).P50;
            var secondP50 = MetricStats.Summarize(secondHalf).P50;
            if (firstP50 <= 0) return;

            var decayPercent = (firstP50 - secondP50) / firstP50 * 100.0;
            if (decayPercent > parameters.FpsDecayPercent)
            {
                handle.AddFinding(FindingSeverity.Warning, "SOAK_FPS_DECAY",
                    $"FPS 衰减：后半程 p50={secondP50:0.##} 比前半程 p50={firstP50:0.##} 低 {decayPercent:0.##}%，" +
                    $"超过阈值 {parameters.FpsDecayPercent:0.##}%");
            }
        }

        // -------------------------------------------------------------- 产物

        /// <summary>回归数据 CSV 写 artifacts（timeMin,allocatedMb,reservedMb,monoUsedMb,fps,errors 累计）。</summary>
        static void WriteRegressionCsv(string artifactsDir, string sanitizedId, List<double> timesMin,
            List<double> allocated, List<double> reserved, List<double> monoUsed, List<double> fps,
            ScenarioRunHandle handle)
        {
            if (string.IsNullOrEmpty(artifactsDir) || timesMin.Count == 0) return;

            try
            {
                var path = AgentCapture.ResolveArtifactsPath(artifactsDir, sanitizedId + "_soak_memory.csv");
                var builder = new System.Text.StringBuilder(256);
                builder.AppendLine("timeMin,allocatedMb,reservedMb,monoUsedMb,fps");
                var baseMinutes = timesMin[0];
                for (var i = 0; i < timesMin.Count; i++)
                {
                    builder.Append((timesMin[i] - baseMinutes).ToString("0.###", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(allocated[i].ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                        .Append(reserved[i].ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                        .Append(monoUsed[i].ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                        .Append((i < fps.Count ? fps[i] : 0).ToString("0.##", CultureInfo.InvariantCulture))
                        .Append('\n');
                }
                File.WriteAllText(path, builder.ToString());
                handle?.Artifacts.Add(path);
            }
            catch (Exception ex)
            {
                handle?.AddFinding(FindingSeverity.Info, "SOAK_CSV_WRITE_FAILED",
                    "回归数据 CSV 写盘失败（不影响判定）：" + ex.Message);
            }
        }

        // -------------------------------------------------------------- partial report

        /// <summary>状态文件里可用的报告元信息。</summary>
        struct ReportMeta
        {
            public string SuiteId;
            public string ReportPath;
        }

        /// <summary>从运行状态文件（宿主每次 scenario 收尾刷新）只读解析 suiteId/reportPath；失败返回 null。</summary>
        static ReportMeta? TryReadStatusReportMeta()
        {
            try
            {
                var path = AgentPaths.StatusFilePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

                var root = JObject.Parse(File.ReadAllText(path));
                var reportPath = root.Value<string>("reportPath");
                if (string.IsNullOrEmpty(reportPath)) return null;

                return new ReportMeta
                {
                    SuiteId = root.Value<string>("suiteId") ?? string.Empty,
                    ReportPath = reportPath,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>partial 文件路径：与正式报告同目录、同名加 <c>.partial</c> 后缀（扩展名前）。</summary>
        static string PartialPathFor(string reportPath)
        {
            if (string.IsNullOrEmpty(reportPath)) return null;
            var directory = Path.GetDirectoryName(reportPath);
            var fileName = Path.GetFileNameWithoutExtension(reportPath) + ".partial" + Path.GetExtension(reportPath);
            return string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);
        }

        /// <summary>
        /// 把当前部分进度覆盖写为 partial JSON（与最终报告同一 schema，<c>truncated: true</c>）：
        /// 经 <see cref="ScenarioReportWriter.WriteReport"/> 同一序列化路径落盘；写失败仅记一次日志告警。
        /// </summary>
        void FlushPartialReport(ReportMeta meta, ScenarioRunHandle handle, ResolvedScenario scenario,
            DateTime startedUtc, int errorCount)
        {
            var path = PartialPathFor(meta.ReportPath);
            if (path == null) return;

            try
            {
                var result = new ScenarioReportResult
                {
                    ScenarioId = scenario.Id,
                    Capability = SoakCapability.CapabilityId,
                    Status = "running",
                    DurationMs = handle != null ? handle.ElapsedMilliseconds : 0,
                    Metrics = MetricSession.Current != null ? MetricSession.Current.BuildSummary() : null,
                };
                result.Log.ErrorCount = errorCount;
                if (handle != null)
                {
                    foreach (var finding in handle.Findings) result.Findings.Add(finding);
                    foreach (var artifact in handle.Artifacts) result.Artifacts.Add(artifact);
                }

                var report = new ScenarioReport
                {
                    Truncated = true,
                    SuiteId = meta.SuiteId,
                    StartedAt = startedUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                    Results = { result },
                };
                ScenarioReportWriter.WriteReport(report, path);
            }
            catch (Exception ex)
            {
                _context?.Log?.Warning("[AxuTestAgent] soak partial report 写盘失败：" + ex.Message);
            }
        }

        static void TryDeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // 删除失败不致命：宿主最终报告照常覆盖正式路径，残留 partial 不影响解析
            }
        }

        /// <summary>解析 params（解析失败回落默认实例，runner 不因参数类型错误直接崩）。</summary>
        static SoakParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<SoakParams>(scenario.Params) : null;
                return parsed ?? new SoakParams();
            }
            catch (Exception)
            {
                return new SoakParams();
            }
        }
    }
}
