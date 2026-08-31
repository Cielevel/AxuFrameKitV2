using System;
using System.IO;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// Agent 各类文件路径约定：Editor 相对当前目录（=工程根），Player 落 persistentDataPath。
    /// </summary>
    public static class AgentPaths
    {
        static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>当前是否编辑器环境。</summary>
        public static bool IsEditor => Application.isEditor;

        /// <summary>
        /// 默认报告路径：Editor 为 Library/AxuTestReports/scenario/{yyyyMMdd-HHmmss}-{suiteId}.json（相对工程根）；
        /// Player 为 persistentDataPath/AxuTest/Reports/ 同名规则。
        /// </summary>
        public static string DefaultReportPath(string suiteId)
        {
            var fileName = $"{DateTime.Now:yyyyMMdd-HHmmss}-{SanitizeFileName(suiteId)}.json";
            return IsEditor
                ? Path.Combine("Library", "AxuTestReports", "scenario", fileName)
                : Path.Combine(Application.persistentDataPath, "AxuTest", "Reports", fileName);
        }

        /// <summary>运行状态文件路径：Editor 为 Temp/axu_scenario_status.json；Player 在 persistentDataPath/AxuTest/Temp/。</summary>
        public static string StatusFilePath => IsEditor
            ? Path.Combine("Temp", "axu_scenario_status.json")
            : Path.Combine(Application.persistentDataPath, "AxuTest", "Temp", "axu_scenario_status.json");

        /// <summary>Editor 桥文件路径（Temp/axu_pending_suite.json）；仅 Editor 有意义，Player 返回 null。</summary>
        public static string EditorBridgeFilePath => IsEditor
            ? Path.Combine("Temp", "axu_pending_suite.json")
            : null;

        /// <summary>报告同目录下的 artifacts 默认目录。</summary>
        public static string DefaultArtifactsDir(string reportPath)
        {
            var dir = string.IsNullOrEmpty(reportPath) ? null : Path.GetDirectoryName(reportPath);
            return string.IsNullOrEmpty(dir) ? "artifacts" : Path.Combine(dir, "artifacts");
        }

        /// <summary>确保文件的父目录存在（自动创建）。</summary>
        public static void EnsureDirectoryForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        /// <summary>把 suite/scenario id 清洗为安全文件名片段（非法字符替换为下划线）。</summary>
        public static string SanitizeFileName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "unknown";
            var builder = new System.Text.StringBuilder(id.Length);
            foreach (var c in id)
                builder.Append(Array.IndexOf(InvalidFileNameChars, c) >= 0 ? '_' : c);
            return builder.Length == 0 ? "unknown" : builder.ToString();
        }
    }
}
