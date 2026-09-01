using System.Collections;
using System.IO;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 截图工具（v1 <c>Runtime/Capture/AxuCapture</c> 思路移植，见 CHANGELOG）：
    /// <see cref="ScreenCapture.CaptureScreenshot"/> 为延迟生效（本帧渲染结束后写出），
    /// <see cref="CaptureScreenshot"/> 以帧等待 + 文件存在性校验收口，失败仅告警不抛异常；
    /// Player/Editor 通用（Editor 捕 GameView）。
    /// </summary>
    public static class AgentCapture
    {
        /// <summary>发起截图后等待落盘的帧数（延迟生效的保守等待）。</summary>
        public const int ScreenshotWaitFrames = 3;

        /// <summary>
        /// 截图到 <paramref name="filePath"/>（superSize 为超采样倍数，&lt;1 按 1 处理）：
        /// 确保父目录存在 → CaptureScreenshot → 等待 3 帧 → 校验文件存在，失败经 Debug.LogWarning 提示。
        /// </summary>
        public static IEnumerator CaptureScreenshot(string filePath, int superSize = 1)
        {
            if (string.IsNullOrEmpty(filePath)) yield break;

            AgentPaths.EnsureDirectoryForFile(filePath);
            ScreenCapture.CaptureScreenshot(filePath, Mathf.Max(1, superSize));

            for (var i = 0; i < ScreenshotWaitFrames; i++) yield return null;

            if (!File.Exists(filePath))
                Debug.LogWarning(
                    "[AxuTestAgent] 截图未在 " + ScreenshotWaitFrames + " 帧内落盘：" + filePath +
                    "（GameView 不存在、渲染被禁用或磁盘不可写时会发生；不影响测试继续）");
        }

        /// <summary>
        /// artifacts 目录 + 文件名拼合为完整路径，并确保所在目录存在（随用随建）。
        /// artifactsDir 为空回落 "artifacts"，fileName 为空回落 "screenshot.png"。
        /// </summary>
        public static string ResolveArtifactsPath(string artifactsDir, string fileName)
        {
            if (string.IsNullOrEmpty(artifactsDir)) artifactsDir = "artifacts";
            if (string.IsNullOrEmpty(fileName)) fileName = "screenshot.png";

            var path = Path.Combine(artifactsDir, fileName);
            AgentPaths.EnsureDirectoryForFile(path);
            return path;
        }
    }
}
