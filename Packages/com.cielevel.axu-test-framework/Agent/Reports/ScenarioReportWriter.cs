using System;
using System.IO;
using System.Text;
using AxuTestFramework.Agent.Tools;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;
using UnityEngine;

namespace AxuTestFramework.Agent.Reports
{
    /// <summary>运行环境采集与报告 JSON 落盘（自动建目录，UTF-8 无 BOM，返回最终路径）。</summary>
    public static class ScenarioReportWriter
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>采集 axu-scenario-report/1 的 environment 节（未采集字段留 null）。</summary>
        public static ScenarioEnvironment CollectEnvironment()
        {
            return new ScenarioEnvironment
            {
                UnityVersion = Application.unityVersion,
                AgentVersion = AgentInfo.Version,
                IsEditor = Application.isEditor,
                DevelopmentBuild = Debug.isDebugBuild,
                DeviceModel = SystemInfo.deviceModel,
                Os = SystemInfo.operatingSystem,
                GraphicsDevice = SystemInfo.graphicsDeviceName,
                GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                SystemMemoryMB = SystemInfo.systemMemorySize,
                QualityLevel = QualitySettings.GetQualityLevel(),
                Resolution = new[] { Screen.width, Screen.height },
                BuildGuid = Application.buildGUID,
            };
        }

        /// <summary>把报告序列化写盘（自动建目录），返回最终写入路径。</summary>
        public static string WriteReport(ScenarioReport report, string path)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            AgentPaths.EnsureDirectoryForFile(path);
            File.WriteAllText(path, AxuJson.ToJson(report), Utf8NoBom);
            return path;
        }
    }
}
