using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using AxuTestFramework;

namespace AxuTestFramework.Samples
{
    /// <summary>
    /// PlayMode 基础示例：[UnitySetUp] 先 AxuTestState.ResetStatics()（域重载开/关都要做）、
    /// AxuWait 等待原语、AxuNoErrorsScope 无错误断言。无需场景资产。
    /// </summary>
    [Category("Axu/Samples")]
    public sealed class SamplePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AxuWait_ForFrames_等待指定帧数后完成()
        {
            yield return AxuWait.ForFrames(3);
            Assert.Pass("等待 3 帧未超时即通过");
        }

        [UnityTest]
        public IEnumerator AxuWait_Until_条件成立_提前返回()
        {
            var frames = 0;
            yield return AxuWait.Until(() => ++frames >= 2, timeoutSeconds: 5f);
            Assert.GreaterOrEqual(frames, 2, "条件应在两帧后成立、不触发超时");
        }

        [UnityTest]
        public IEnumerator AxuNoErrorsScope_期间无错误_正常释放()
        {
            using (new AxuNoErrorsScope())
            {
                yield return AxuWait.ForSeconds(0.1f);
            }
            Assert.Pass("作用域内无 Error 日志，Dispose 不抛异常即通过");
        }
    }
}
