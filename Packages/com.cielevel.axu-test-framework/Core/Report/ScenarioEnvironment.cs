using System.Collections.Generic;
using Newtonsoft.Json;

namespace AxuTestFramework.Core.Report
{
    /// <summary>运行环境信息（schema environment 节，Player 侧尤其重要：真机矩阵的坐标系）；未采集字段留 null。</summary>
    public class ScenarioEnvironment
    {
        [JsonProperty("unityVersion")] public string UnityVersion;

        [JsonProperty("agentVersion")] public string AgentVersion;

        [JsonProperty("isEditor")] public bool? IsEditor;

        [JsonProperty("developmentBuild")] public bool? DevelopmentBuild;

        /// <summary>设备型号（SystemInfo.deviceModel）。</summary>
        [JsonProperty("deviceModel")] public string DeviceModel;

        /// <summary>操作系统（SystemInfo.operatingSystem）。</summary>
        [JsonProperty("os")] public string Os;

        /// <summary>显卡设备名（SystemInfo.graphicsDeviceName）。</summary>
        [JsonProperty("graphicsDevice")] public string GraphicsDevice;

        /// <summary>图形 API（SystemInfo.graphicsDeviceType）。</summary>
        [JsonProperty("graphicsApi")] public string GraphicsApi;

        /// <summary>系统内存 MB（SystemInfo.systemMemorySize）。</summary>
        [JsonProperty("systemMemoryMB")] public int? SystemMemoryMB;

        /// <summary>画质档序号（QualitySettings.GetQualityLevel()）。</summary>
        [JsonProperty("qualityLevel")] public int? QualityLevel;

        /// <summary>分辨率 [宽, 高]（Screen.width × Screen.height）。</summary>
        [JsonProperty("resolution")] public int[] Resolution;

        /// <summary>构建 GUID（Application.buildGUID）。</summary>
        [JsonProperty("buildGuid")] public string BuildGuid;
    }
}
