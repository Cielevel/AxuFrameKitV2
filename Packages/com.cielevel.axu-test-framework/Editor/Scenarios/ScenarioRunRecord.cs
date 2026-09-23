using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行记录文件（Temp/axu_scenario_run.json）：Editor 侧发起一次 scenario 运行前写入、看门狗收尾时删除。
    /// <para>存在即"有一次运行未收尾"，是启动-收尾契约的枢纽（看板、菜单、pipeline 三条入口共用同一份记录）：
    /// 看门狗据此自动退出 Play Mode（无需用户手动停），看板据此区分"还在跑"与"已结束"。
    /// 与状态文件（<see cref="AxuScenarioRunStatus"/>）的关系：记录是"我发起的那次运行的凭据"（含
    /// <see cref="RunId"/> 与 suite 路径），状态文件是 Agent 写出的运行事实（含进度/终态/报告路径）；
    /// 两者以 <see cref="RunId"/> 对齐，避免上一次运行的状态文件被当作本次结果。</para>
    /// <para>路径可注入（<c>fullPath</c> 参数）以便测试在不触碰真实 Temp 的前提下验证读写与陈旧判定。</para>
    /// </summary>
    public sealed class ScenarioRunRecord
    {
        /// <summary>默认记录文件（相对工程根）。</summary>
        public const string DefaultPath = "Temp/axu_scenario_run.json";

        /// <summary>发起方：看板窗口。</summary>
        public const string SourceDashboard = "dashboard";

        /// <summary>发起方：菜单 Run Suite JSON。</summary>
        public const string SourceMenu = "menu";

        /// <summary>发起方：pipeline 命令 axu_scenario_run。</summary>
        public const string SourcePipeline = "pipeline";

        /// <summary>无记录时的保守陈旧阈值（分钟）：记录残留且无人收尾时据此放行新的运行（与运行状态同口径）。</summary>
        public const int StaleMinutes = AxuRunLiveness.StaleMinutes;

        /// <summary>本轮运行标识（发起方生成）。</summary>
        [JsonProperty("runId")] public string RunId { get; set; }

        /// <summary>suite / scenario 文件完整路径。</summary>
        [JsonProperty("suitePath")] public string SuitePath { get; set; }

        /// <summary>限定运行的 scenario id（空 = 全部）。</summary>
        [JsonProperty("scenarioIds")] public List<string> ScenarioIds { get; set; }

        /// <summary>发起时间（UTC，ISO 8601）。</summary>
        [JsonProperty("startedAtUtc")] public string StartedAtUtc { get; set; }

        /// <summary>发起方（<see cref="SourceDashboard"/> / <see cref="SourceMenu"/> / <see cref="SourcePipeline"/>）。</summary>
        [JsonProperty("source")] public string Source { get; set; }

        /// <summary>Agent 写出终态后是否自动退出 Play Mode（Editor 内三条入口均为 true；保留字段以备手动路径需要停驻观察）。</summary>
        [JsonProperty("autoStopOnDone")] public bool AutoStopOnDone { get; set; } = true;

        /// <summary>默认记录文件完整路径（Temp/axu_scenario_run.json）。</summary>
        public static string DefaultFullPath => AxuTestReportPaths.ToFullPath(DefaultPath);

        /// <summary>新建一条运行记录（生成新的 RunId）。</summary>
        public static ScenarioRunRecord Create(string suitePath, IEnumerable<string> scenarioIds, string source)
        {
            var ids = scenarioIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .ToList();

            return new ScenarioRunRecord
            {
                RunId = Guid.NewGuid().ToString("N"),
                SuitePath = suitePath,
                ScenarioIds = ids != null && ids.Count > 0 ? ids : null,
                StartedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                Source = source,
                AutoStopOnDone = true,
            };
        }

        /// <summary>写记录文件（覆盖写）；IO 异常向上抛出（发起方据此中止运行）。</summary>
        public static void Write(ScenarioRunRecord record, string fullPath = null)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            var target = string.IsNullOrEmpty(fullPath) ? DefaultFullPath : fullPath;
            var directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(target, JsonConvert.SerializeObject(record, Formatting.Indented),
                new System.Text.UTF8Encoding(false));
        }

        /// <summary>读记录文件；不存在/解析失败返回 null。</summary>
        public static ScenarioRunRecord TryRead(string fullPath = null)
        {
            try
            {
                var target = string.IsNullOrEmpty(fullPath) ? DefaultFullPath : fullPath;
                if (!File.Exists(target))
                    return null;
                return JsonConvert.DeserializeObject<ScenarioRunRecord>(File.ReadAllText(target));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>删除记录文件（不存在时静默）。</summary>
        public static void Delete(string fullPath = null)
        {
            try
            {
                var target = string.IsNullOrEmpty(fullPath) ? DefaultFullPath : fullPath;
                if (File.Exists(target))
                    File.Delete(target);
            }
            catch (Exception)
            {
                // 删除失败不影响主流程（下次启动会覆盖写）
            }
        }

        /// <summary>解析发起时间（UTC）；缺失/非法返回 false。</summary>
        public bool TryGetStartedUtc(out DateTime startedUtc)
        {
            return DateTime.TryParse(StartedAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out startedUtc);
        }

        /// <summary>记录是否已越过 <see cref="StaleMinutes"/>（残留判定：发起时间早于阈值即为陈旧）。</summary>
        public bool IsStale(DateTime utcNow)
        {
            return !TryGetStartedUtc(out var started) || utcNow - started > TimeSpan.FromMinutes(StaleMinutes);
        }

        /// <summary>
        /// 状态文件是否属于本记录（<see cref="RunId"/> 相等且非空）。
        /// RunId 为空（旧版 Agent / 非桥文件来源）一律判不匹配——宁可不认领，也不把别人的状态当本次结果。
        /// </summary>
        public bool MatchesStatus(AxuScenarioRunStatus status)
        {
            return status != null
                && !string.IsNullOrEmpty(RunId)
                && string.Equals(RunId, status.RunId, StringComparison.Ordinal);
        }

        /// <summary>
        /// 记录是否仍在途（供互斥闸）：Play 中恒在途（Editor 驱动的运行必然在 Play 内跑）；
        /// 不在 Play 时未越过陈旧阈值即保守按在途处理（看门狗会在下一次轮询内收尾，正常不超过 0.5s）。
        /// </summary>
        public bool IsInFlight(DateTime utcNow, bool isPlaying)
        {
            return isPlaying || !IsStale(utcNow);
        }
    }
}
