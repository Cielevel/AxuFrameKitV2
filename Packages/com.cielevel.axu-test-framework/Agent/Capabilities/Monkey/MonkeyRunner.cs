using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using AxuTestFramework.Agent.Metrics;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Monkey
{
    /// <summary>
    /// Monkey 运行器（方案 §4.7，v1）：可选加载目标场景 → 显式 seed 的 <see cref="System.Random"/>
    /// 生成随机点按序列（间隔带 ±50% 抖动、坐标取 region 归一化区域）→ 每操作记 JSONL 流水
    /// （artifacts/monkey-actions.jsonl）→ <see cref="AgentLogWatch"/> 监错（错误即截图，
    /// stopOnError 时终止）→ 卡死检测（<see cref="Time.frameCount"/> 在 hangThresholdSec 真实墙钟内
    /// 无推进 → 截图 + Error finding + 终止）→ 结束产出 monkey.* metrics。
    /// </summary>
    public class MonkeyRunner : IScenarioRunner
    {
        readonly ScenarioContext _context;

        public MonkeyRunner(ScenarioContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public IEnumerator Run(ResolvedScenario scenario, ScenarioRunHandle handle)
        {
            var parameters = ParseParams(scenario);
            var log = _context != null ? _context.Log : null;

            // seed 解析：launch.Seed（context.Seed）优先，其次 params.seed（缺省 0）——同 seed 必复现
            var effectiveSeed = _context != null && _context.Seed.HasValue ? _context.Seed.Value : parameters.Seed;
            var random = new System.Random(effectiveSeed);
            var region = parameters.Region != null ? parameters.Region.Clamped : (0f, 0f, 1f, 1f);

            // ---- 可选：加载目标场景（同 perf）----
            if (!string.IsNullOrWhiteSpace(parameters.Scene))
            {
                var loadResult = new AgentSceneLoadResult();
                yield return AgentScenes.LoadAsync(parameters.Scene, loadResult);
                if (!loadResult.Loaded)
                {
                    if (handle != null)
                    {
                        handle.Status = RunStatus.Error;
                        handle.AddFinding(FindingSeverity.Error, "MONKEY_SCENE_LOAD_FAILED",
                            $"场景加载失败：{parameters.Scene}（{loadResult.Error}）");
                    }
                    yield break;
                }
                log?.Info($"场景已加载：{loadResult.ResolvedPath}");
            }

            log?.Info($"monkey 开始：duration={parameters.ClampedDurationSec:0.##}s，" +
                $"rate={parameters.ClampedActionsPerSecond:0.##}/s，seed={effectiveSeed}，" +
                $"region=[{region.Item1:0.##},{region.Item2:0.##},{region.Item3:0.##},{region.Item4:0.##}]");

            // ---- JSONL 流水（StreamWriter 常开 + 逐条 Flush，异常进程退出也保住已发生的操作）----
            var artifactsDir = _context != null ? _context.ArtifactsDir : null;
            var journalPath = AgentCapture.ResolveArtifactsPath(artifactsDir,
                AgentPaths.SanitizeFileName(scenario.Id) + "-monkey-actions.jsonl");
            StreamWriter journal = null;
            try
            {
                AgentPaths.EnsureDirectoryForFile(journalPath);
                journal = new StreamWriter(journalPath, append: false) { AutoFlush = true };
            }
            catch (Exception e)
            {
                log?.Warning($"monkey 流水文件创建失败（{e.Message}），本次运行不记流水");
            }

            // ---- 降级一次性提示 → Finding(info) ----
            if (!AgentInput.InputSystemTapAvailable)
            {
                AgentInput.InfoFindingSink = message => handle?.AddFinding(FindingSeverity.Info,
                    "MONKEY_INPUT_FALLBACK", message);
            }

            long actionsTotal = 0;
            var errorsTotal = 0;
            var terminated = false;
            var terminationCode = "";
            var terminationMessage = "";
            var startRealtime = Time.realtimeSinceStartupAsDouble;

            using (var watch = new AgentLogWatch())
            {
                var deadline = startRealtime + parameters.ClampedDurationSec;
                var meanInterval = 1.0 / parameters.ClampedActionsPerSecond;

                // 卡死检测基线：最近一次 frameCount 推进时的墙钟
                var lastFrameCount = Time.frameCount;
                var lastFrameAdvanceRealtime = startRealtime;

                // 下一次操作的墙钟（±50% 均匀抖动）
                var nextActionRealtime = startRealtime + NextInterval(random, meanInterval);

                while (!terminated && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    if (handle != null && handle.CancelRequested)
                    {
                        journal?.Dispose();
                        AgentInput.InfoFindingSink = null;
                        yield break;
                    }

                    var now = Time.realtimeSinceStartupAsDouble;

                    // ---- 卡死检测：frameCount 在阈值墙钟内无推进 → 截图 + Error + 终止 ----
                    if (Time.frameCount != lastFrameCount)
                    {
                        lastFrameCount = Time.frameCount;
                        lastFrameAdvanceRealtime = now;
                    }
                    else if (now - lastFrameAdvanceRealtime >= parameters.ClampedHangThresholdSec)
                    {
                        terminated = true;
                        terminationCode = "MONKEY_HANG_DETECTED";
                        terminationMessage =
                            $"疑似卡死：{parameters.ClampedHangThresholdSec:0.##}s 真实墙钟内无帧推进" +
                            $"（frameCount 停留在 {lastFrameCount}，位于第 {actionsTotal} 次操作后）";
                        break;
                    }

                    // ---- 错误监视（AgentLogWatch 线程安全读数）----
                    if (watch.ErrorCount > errorsTotal)
                    {
                        errorsTotal = watch.ErrorCount;
                        if (parameters.StopOnError)
                        {
                            terminated = true;
                            terminationCode = "MONKEY_ERROR_DETECTED";
                            var snapshot = watch.RecentErrors;
                            var latest = snapshot.Count > 0 ? snapshot[snapshot.Count - 1] : "";
                            terminationMessage = $"检测到错误日志（累计 {errorsTotal}），stopOnError 终止：{latest}";
                            break;
                        }
                    }

                    // ---- 到点注入点按 ----
                    if (now >= nextActionRealtime)
                    {
                        nextActionRealtime = now + NextInterval(random, meanInterval);

                        var nx = region.Item1 + random.NextDouble() * region.Item3;
                        var ny = region.Item2 + random.NextDouble() * region.Item4;
                        var position = new Vector2((float)nx * Screen.width, (float)ny * Screen.height);

                        string hitName = null;
                        try
                        {
                            hitName = AgentInput.InjectTap(position);
                        }
                        catch (Exception e)
                        {
                            log?.Warning($"monkey 注入异常（忽略继续）：{e.Message}");
                        }

                        actionsTotal++;
                        WriteJournalEntry(journal, actionsTotal,
                            Time.realtimeSinceStartupAsDouble - startRealtime, position, hitName);
                    }

                    yield return null; // 逐帧轮询：真实墙钟驱动，看门狗/取消可逐帧介入
                }

                if (errorsTotal < watch.ErrorCount) errorsTotal = watch.ErrorCount; // 非即停路径收尾对账
            }

            // ---- 终止原因：截图 + Finding ----
            if (terminated && handle != null)
            {
                handle.Status = RunStatus.Error;
                handle.AddFinding(FindingSeverity.Error, terminationCode, terminationMessage);

                var errorShot = AgentCapture.ResolveArtifactsPath(artifactsDir,
                    AgentPaths.SanitizeFileName(scenario.Id) + "_monkey-terminate.png");
                yield return AgentCapture.CaptureScreenshot(errorShot);
                handle.Artifacts.Add(errorShot);
            }

            if (journal != null) journal.Dispose();

            // ---- 收尾 metrics（Gauge：结束一次性终值）----
            var durationSec = Time.realtimeSinceStartupAsDouble - startRealtime;
            AxuMetrics.Gauge("monkey.actionsTotal", actionsTotal);
            AxuMetrics.Gauge("monkey.errorsTotal", errorsTotal);
            AxuMetrics.Gauge("monkey.durationSec", durationSec);

            if (!string.IsNullOrEmpty(journalPath))
            {
                handle?.Artifacts.Add(journalPath);
                log?.Info($"monkey 流水：{journalPath}");
            }
            log?.Info($"monkey 结束：actions={actionsTotal}，errors={errorsTotal}，duration={durationSec:0.##}s" +
                (terminated ? $"（提前终止：{terminationCode}）" : ""));

            AgentInput.InfoFindingSink = null; // 静态回调成对清理

            if (handle != null && handle.Status == RunStatus.Running) handle.Status = RunStatus.Passed;
        }

        /// <summary>下一次操作间隔：均值 meanInterval 的 ±50% 均匀抖动（&gt;=0.02s 下限防密集成灾）。</summary>
        static double NextInterval(System.Random random, double meanInterval)
        {
            return Math.Max(0.02, meanInterval * (0.5 + random.NextDouble()));
        }

        /// <summary>写一条 JSONL 流水：{"seq":n,"t":sec,"pos":[x,y],"hit":"对象名"|null}。</summary>
        static void WriteJournalEntry(StreamWriter journal, long seq, double t, Vector2 pos, string hitName)
        {
            if (journal == null) return;
            try
            {
                journal.WriteLine(JsonConvert.SerializeObject(new JournalEntry
                {
                    Seq = seq,
                    T = Math.Round(t, 3),
                    Pos = new[] { Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y) },
                    Hit = hitName,
                }));
            }
            catch (Exception)
            {
                // 流水写失败不影响测试继续（journal 由 AutoFlush 尽力保住前文）
            }
        }

        /// <summary>解析 params（解析失败回落默认实例，runner 不因参数类型错误直接崩）。</summary>
        static MonkeyParams ParseParams(ResolvedScenario scenario)
        {
            try
            {
                var parsed = scenario.Params != null ? AxuJson.ToObject<MonkeyParams>(scenario.Params) : null;
                return parsed ?? new MonkeyParams();
            }
            catch (Exception)
            {
                return new MonkeyParams();
            }
        }

        /// <summary>JSONL 流水条目（Newtonsoft 序列化，camelCase 字段直出）。</summary>
        class JournalEntry
        {
            [JsonProperty("seq")] public long Seq;
            [JsonProperty("t")] public double T;
            [JsonProperty("pos")] public int[] Pos;
            [JsonProperty("hit")] public string Hit;
        }
    }
}
