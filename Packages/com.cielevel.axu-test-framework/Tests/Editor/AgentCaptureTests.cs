using System.IO;
using AxuTestFramework.Agent.Tools;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] AgentCapture 纯逻辑（路径拼合/回退/目录确保）契约（Phase 3 P0，对齐已落地实现）。
    /// 截图本体（ScreenCapture + 帧等待）属 e2e/PlayMode 范畴，本套件只覆盖 ResolveArtifactsPath 与常量契约：
    ///  - artifactsDir 空回落 "artifacts"；fileName 空回落 "screenshot.png"；
    ///  - 返回 Path.Combine 后的完整路径，并确保所在目录存在（随用随建）。
    /// null 目录回退会在 cwd（工程根）建 "artifacts/"：用例自检清理，仅删除本轮新建且为空的目录。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AgentCaptureTests
    {
        const string ScratchDir = "Temp/AxuTestFramework_CaptureTests";

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(ScratchDir)) Directory.Delete(ScratchDir, true);
        }

        [Test]
        public void ScreenshotWaitFrames_契约常量为3帧()
        {
            Assert.That(AgentCapture.ScreenshotWaitFrames, Is.EqualTo(3));
        }

        [Test]
        public void ResolveArtifactsPath_目录与文件名齐全_拼合并确保目录存在()
        {
            var path = AgentCapture.ResolveArtifactsPath(ScratchDir, "shot.png");

            Assert.That(path, Is.EqualTo(Path.Combine(ScratchDir, "shot.png")));
            Assert.That(Directory.Exists(ScratchDir), Is.True, "应随用随建所在目录。");
        }

        [Test]
        public void ResolveArtifactsPath_文件名空_回落screenshotpng()
        {
            var path = AgentCapture.ResolveArtifactsPath(ScratchDir, null);

            Assert.That(path, Is.EqualTo(Path.Combine(ScratchDir, "screenshot.png")));
            Assert.That(Directory.Exists(ScratchDir), Is.True);
        }

        [Test]
        public void ResolveArtifactsPath_目录空_回落artifacts并拼文件名()
        {
            var artifactsRoot = Path.GetFullPath("artifacts");
            var existedBefore = Directory.Exists(artifactsRoot);
            try
            {
                var path = AgentCapture.ResolveArtifactsPath(null, "shot.png");

                Assert.That(path, Is.EqualTo(Path.Combine("artifacts", "shot.png")));
                Assert.That(Directory.Exists(artifactsRoot), Is.True);
            }
            finally
            {
                // 仅清理本轮新建且未被他人写入的目录，避免污染工程根
                if (!existedBefore
                    && Directory.Exists(artifactsRoot)
                    && Directory.GetFiles(artifactsRoot).Length == 0
                    && Directory.GetDirectories(artifactsRoot).Length == 0)
                {
                    Directory.Delete(artifactsRoot);
                }
            }
        }
    }
}
