using System.Collections;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] 工具层组合冒烟：AxuNoErrorsScope（无错误作用域）+ AxuWait.Until（条件等待）跨帧协同。
    /// </summary>
    [Category("Axu/Play")]
    public class NoErrorsScopePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            yield return AxuWait.ForFrames(1);
        }

        [UnityTest]
        public IEnumerator AxuNoErrorsScope与WaitUntil_跨帧组合冒烟_无错误通过()
        {
            // Arrange
            int startFrame = Time.frameCount;

            // Act：无错误作用域内跨帧等待条件（3 帧后成立）
            using (AxuLogWatch.ExpectNoErrors())
            {
                Debug.Log("[AxuFrameKit.Tests] PlayMode 组合冒烟：等待 3 帧");
                yield return AxuWait.Until(() => Time.frameCount >= startFrame + 3, 5f);
                Debug.Log("[AxuFrameKit.Tests] 条件达成");
            }

            // Assert：作用域正常关闭（无错误即不 Fail），条件确实达成
            Assert.That(Time.frameCount, Is.GreaterThanOrEqualTo(startFrame + 3));
        }
    }
}
