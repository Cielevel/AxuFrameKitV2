using System;
using System.Globalization;
using AxuTestFramework.Core.Scenarios;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 通用参数（DOC/ScenarioSchema.md §4 保留键）的应用/恢复：
    /// <see cref="AgentEnvironment.Apply"/> 在每个 scenario 开始前应用 timeScale/vsync/targetFrameRate/
    /// qualityLevel/resolution/seed，宿主在 finally 中调 <see cref="AgentEnvironment.Restore"/> 保证
    /// 超时/取消中断路径也可靠还原。解析绝不抛异常：缺键或类型不符 → 对应字段 null（不修改该项）。
    /// </summary>
    public static class AgentEnvironment
    {
        /// <summary>应用通用参数，返回环境快照（供 Restore 还原）。log 可为 null（跳过日志）。</summary>
        public static AgentEnvironmentSnapshot Apply(JObject parameters, int? seedOverride, IAgentLog log)
        {
            var overrides = AgentEnvironmentOverrides.Parse(parameters);
            var snapshot = new AgentEnvironmentSnapshot();

            if (overrides.TimeScale != null)
            {
                snapshot.PrevTimeScale = Time.timeScale;
                var clamped = Mathf.Clamp(overrides.TimeScale.Value, 0f, 100f);
                if (!Mathf.Approximately(clamped, overrides.TimeScale.Value)) LogClamp(log, "timeScale", overrides.TimeScale.Value, clamped);
                Time.timeScale = clamped;
            }

            if (overrides.VSync != null)
            {
                snapshot.PrevVSync = QualitySettings.vSyncCount;
                var clamped = Mathf.Clamp(overrides.VSync.Value, 0, 4);
                if (clamped != overrides.VSync.Value) LogClamp(log, "vsync", overrides.VSync.Value, clamped);
                QualitySettings.vSyncCount = clamped;
            }

            if (overrides.TargetFrameRate != null)
            {
                snapshot.PrevTargetFrameRate = Application.targetFrameRate;
                var clamped = Mathf.Clamp(overrides.TargetFrameRate.Value, -1, 1000);
                if (clamped != overrides.TargetFrameRate.Value) LogClamp(log, "targetFrameRate", overrides.TargetFrameRate.Value, clamped);
                Application.targetFrameRate = clamped;
            }

            if (overrides.QualityLevel != null)
            {
                // Unity 6000.0 无 selectedQualityLevel 属性（6000.1+ 才有），本版本 API 为 Get/SetQualityLevel
                snapshot.PrevQualityLevel = QualitySettings.GetQualityLevel();
                var max = Mathf.Max(0, QualitySettings.names.Length - 1);
                var clamped = Mathf.Clamp(overrides.QualityLevel.Value, 0, max);
                if (clamped != overrides.QualityLevel.Value) LogClamp(log, "qualityLevel", overrides.QualityLevel.Value, clamped);
                QualitySettings.SetQualityLevel(clamped);
            }

            // resolution 仅在 Player 应用（Editor 的 GameView 尺寸不经 Screen.SetResolution 控制）
            if (overrides.Resolution != null && overrides.Resolution.Length >= 2)
            {
                var width = Mathf.Max(1, overrides.Resolution[0]);
                var height = Mathf.Max(1, overrides.Resolution[1]);
                if (Application.isEditor)
                {
                    log?.Info($"resolution {width}x{height} 仅在 Player 生效，Editor 下跳过");
                }
                else
                {
                    Screen.SetResolution(width, height, Screen.fullScreen);
                    log?.Info($"resolution 已应用：{width}x{height}");
                }
            }

            snapshot.AppliedSeed = seedOverride ?? overrides.Seed ?? 0;
            return snapshot;
        }

        /// <summary>还原快照中非 null 的各项；snapshot 为 null 直接返回（幂等，可安全重复调用）。</summary>
        public static void Restore(AgentEnvironmentSnapshot snapshot, IAgentLog log)
        {
            if (snapshot == null) return;

            var restored = false;
            if (snapshot.PrevTimeScale != null)
            {
                Time.timeScale = snapshot.PrevTimeScale.Value;
                restored = true;
            }
            if (snapshot.PrevVSync != null)
            {
                QualitySettings.vSyncCount = snapshot.PrevVSync.Value;
                restored = true;
            }
            if (snapshot.PrevTargetFrameRate != null)
            {
                Application.targetFrameRate = snapshot.PrevTargetFrameRate.Value;
                restored = true;
            }
            if (snapshot.PrevQualityLevel != null)
            {
                // Unity 6000.0 API：SetQualityLevel（selectedQualityLevel 属性为 6000.1+）
                QualitySettings.SetQualityLevel(snapshot.PrevQualityLevel.Value);
                restored = true;
            }

            // resolution 无还原项（快照未记录原分辨率；Editor 下本就不应用，Player 下随下次 Apply 覆盖）
            if (restored) log?.Info("环境通用参数已还原");
        }

        static void LogClamp(IAgentLog log, string key, double original, double clamped)
        {
            log?.Info(
                $"通用参数 {key}={Format(original)} 越界，已 clamp 为 {Format(clamped)}");
        }

        static string Format(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 通用参数解析结果（纯 POCO，供单测）。字段 null = 该项缺失/类型不符/不修改。
    /// </summary>
    public sealed class AgentEnvironmentOverrides
    {
        /// <summary>timeScale（float；有效应用范围 clamp 0..100）。</summary>
        public float? TimeScale;

        /// <summary>vsync（int；有效应用范围 clamp 0..4，0=不等待垂直同步）。</summary>
        public int? VSync;

        /// <summary>targetFrameRate（int；有效应用范围 clamp -1..1000，-1=平台默认）。</summary>
        public int? TargetFrameRate;

        /// <summary>qualityLevel（int；有效应用范围 clamp 0..档位数-1）。</summary>
        public int? QualityLevel;

        /// <summary>resolution（[w,h]，仅 Player 应用；分量下限 1）。</summary>
        public int[] Resolution;

        /// <summary>seed（int；仅生效到快照 AppliedSeed，不在本类应用）。</summary>
        public int? Seed;

        /// <summary>
        /// 从 scenario.params 解析通用参数保留键：timeScale/vsync/targetFrameRate/qualityLevel/
        /// resolution（[w,h] 数组）/seed。缺键或类型不符 → 对应字段 null；绝不抛异常。
        /// </summary>
        public static AgentEnvironmentOverrides Parse(JObject parameters)
        {
            var overrides = new AgentEnvironmentOverrides();
            if (parameters == null) return overrides;

            overrides.TimeScale = ReadFloat(parameters, "timeScale");
            overrides.VSync = ReadInt(parameters, "vsync");
            overrides.TargetFrameRate = ReadInt(parameters, "targetFrameRate");
            overrides.QualityLevel = ReadInt(parameters, "qualityLevel");
            overrides.Resolution = ReadResolution(parameters, "resolution");
            overrides.Seed = ReadInt(parameters, "seed");
            return overrides;
        }

        static float? ReadFloat(JObject parameters, string key)
        {
            var token = parameters[key];
            if (token == null || !IsNumeric(token.Type)) return null;
            try { return (float)token; }
            catch (Exception) { return null; }
        }

        static int? ReadInt(JObject parameters, string key)
        {
            var token = parameters[key];
            if (token == null || !IsNumeric(token.Type)) return null;
            try { return (int)token; }
            catch (Exception) { return null; }
        }

        static int[] ReadResolution(JObject parameters, string key)
        {
            if (!(parameters[key] is JArray array) || array.Count < 2) return null;

            var width = ReadNumericToken(array[0]);
            var height = ReadNumericToken(array[1]);
            if (width == null || height == null) return null;
            return new[] { width.Value, height.Value };
        }

        static int? ReadNumericToken(JToken token)
        {
            if (token == null || !IsNumeric(token.Type)) return null;
            try { return (int)token; }
            catch (Exception) { return null; }
        }

        static bool IsNumeric(JTokenType type)
        {
            return type == JTokenType.Integer || type == JTokenType.Float;
        }
    }

    /// <summary>
    /// 环境快照（Apply 的还原依据）：Prev* 为 null 表示该项未修改、无需还原；
    /// resolution 不记录（Editor 下不应用，Player 下随下次 Apply 覆盖）。
    /// </summary>
    public sealed class AgentEnvironmentSnapshot
    {
        /// <summary>应用前的 Time.timeScale（null=未修改）。</summary>
        public float? PrevTimeScale;

        /// <summary>应用前的 QualitySettings.vSyncCount（null=未修改）。</summary>
        public int? PrevVSync;

        /// <summary>应用前的 Application.targetFrameRate（null=未修改）。</summary>
        public int? PrevTargetFrameRate;

        /// <summary>应用前的 QualitySettings.selectedQualityLevel（null=未修改）。</summary>
        public int? PrevQualityLevel;

        /// <summary>本次生效的随机种子：launch.Seed 优先，其次 params.seed，缺省 0。</summary>
        public int AppliedSeed;
    }
}
