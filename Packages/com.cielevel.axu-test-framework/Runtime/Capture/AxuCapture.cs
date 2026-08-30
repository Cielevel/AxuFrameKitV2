using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AxuTestFramework
{
    /// <summary>
    /// PlayMode 截图工具：把 GameView 截图写入报告 artifacts 目录
    /// （默认工程根下 Library/AxuTestReports/artifacts/，与 M2 报告落盘目录约定一致）。
    /// EditMode 下调用会抛 <see cref="InvalidOperationException"/>（EditMode 无 GameView 渲染输出）。
    /// </summary>
    public static class AxuCapture
    {
        /// <summary>默认 artifacts 目录（相对工程根），与报告落盘目录约定保持一致。</summary>
        public const string DefaultArtifactsDirectory = "Library/AxuTestReports/artifacts";

        /// <summary>截图落盘等待超时秒数（CaptureScreenshot 为延迟生效，需等文件真正写出）。</summary>
        public const float ScreenshotWaitTimeoutSeconds = 10f;

        private static string _artifactsDirectory;
        private static readonly List<string> _capturedPaths = new List<string>();

        /// <summary>
        /// artifacts 目录（相对工程根）。默认 <see cref="DefaultArtifactsDirectory"/>；
        /// M2 报告器可在指定 out_path 时重定向此目录。
        /// </summary>
        public static string ArtifactsDirectory
        {
            get { return string.IsNullOrEmpty(_artifactsDirectory) ? DefaultArtifactsDirectory : _artifactsDirectory; }
            set { _artifactsDirectory = value; }
        }

        /// <summary>
        /// artifacts 目录的完整物理路径（Editor 下锚定工程根；播放器测试运行时退化为 temporaryCachePath 下的 AxuCapture 目录）。
        /// </summary>
        public static string ArtifactsRootFullPath
        {
            get
            {
                if (Application.isEditor)
                {
                    // Application.dataPath 在 Editor 下为 "{工程根}/Assets"，取上一级得工程根
                    string projectRoot = Path.GetDirectoryName(Application.dataPath);
                    return Path.Combine(projectRoot, ArtifactsDirectory.Replace('/', Path.DirectorySeparatorChar));
                }
                return Path.Combine(Application.temporaryCachePath, "AxuCapture");
            }
        }

        /// <summary>
        /// 最近一次经 <see cref="ScreenshotCoroutine"/> 确认落盘的截图完整路径；未截过图为 null。
        /// </summary>
        public static string LastScreenshotPath { get; private set; }

        /// <summary>
        /// 本次会话已确认落盘的截图完整路径（按时间顺序）。可作为 M2 报告 Artifacts 节的数据源。
        /// </summary>
        public static IReadOnlyList<string> CapturedPaths
        {
            get { return _capturedPaths; }
        }

        /// <summary>
        /// 清空截图记录（LastScreenshotPath 与 CapturedPaths），不删除磁盘文件。可在 [UnitySetUp] 中调用防串扰。
        /// </summary>
        public static void ClearCaptured()
        {
            _capturedPaths.Clear();
            LastScreenshotPath = null;
        }

        /// <summary>
        /// 发起一次截图并返回目标文件的完整路径。
        /// 注意 <see cref="ScreenCapture.CaptureScreenshot"/> 是延迟生效的（本帧渲染结束后才写出文件），
        /// 测试中请改用 <see cref="ScreenshotCoroutine"/> 等待落盘后再断言。
        /// </summary>
        /// <param name="name">截图名（作为文件名主体；非法文件名字符会被替换为 '_'，文件名自动附带 UTC 时间戳防覆盖）。</param>
        public static string Screenshot(string name)
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "AxuCapture 仅在 PlayMode 下可用：EditMode 没有 GameView 渲染输出。" +
                    "EditMode 替代方案：经 Unity Pipeline 本地 API 调用 capture_game_view 命令，" +
                    "或使用编辑器侧截图工具（详见 .zcode/08-unity-pipeline-api.md）。");
            }

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name 不能为空。", nameof(name));

            string dir = ArtifactsRootFullPath;
            Directory.CreateDirectory(dir);

            string path = BuildUniquePath(dir, SanitizeFileName(name));
            ScreenCapture.CaptureScreenshot(path);
            return path;
        }

        /// <summary>
        /// 截图协程：发起截图并逐帧等待文件真正落盘（超时 <see cref="ScreenshotWaitTimeoutSeconds"/> 则 Assert.Fail），
        /// 完成后更新 <see cref="LastScreenshotPath"/> 与 <see cref="CapturedPaths"/>。
        /// </summary>
        /// <param name="name">截图名（作为文件名主体，同 <see cref="Screenshot"/>）。</param>
        public static IEnumerator ScreenshotCoroutine(string name)
        {
            string path = Screenshot(name);

            float deadline = Time.realtimeSinceStartup + ScreenshotWaitTimeoutSeconds;
            while (!File.Exists(path))
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(
                        $"AxuCapture：截图在 {ScreenshotWaitTimeoutSeconds:0.#}s 内未落盘：{path}" +
                        "（请确认 GameView 存在且未被禁用渲染）。");
                }
                yield return null;
            }

            LastScreenshotPath = path;
            _capturedPaths.Add(path);
        }

        /// <summary>生成"名称_UTC时间戳.png"形式的唯一路径（同毫秒冲突时追加 -1/-2 序号）。</summary>
        private static string BuildUniquePath(string dir, string baseName)
        {
            string path = Path.Combine(dir, baseName + "_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
            int suffix = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(
                    dir,
                    baseName + "_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + suffix + ".png");
                suffix++;
            }
            return path;
        }

        /// <summary>把非法文件名字符替换为 '_'。</summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalidChars.Length; j++)
                {
                    if (chars[i] == invalidChars[j])
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }
            return new string(chars);
        }
    }
}
