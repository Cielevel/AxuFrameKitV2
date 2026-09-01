using NUnit.Framework;
using UnityEngine.TestTools;
using AxuTestFramework;

namespace AxuTestFramework.Samples
{
    /// <summary>
    /// EditMode 基础示例：命名规范（方法名_条件_期望结果）、纯断言、AxuLogWatch、
    /// LogAssert.Expect 与 AxuTestState 静态重置登记。
    /// </summary>
    [Category("Axu/Samples")]
    public sealed class SampleEditModeTests
    {
        [Test]
        public void 纯断言_两数相加_得到和()
        {
            Assert.AreEqual(4, 2 + 2);
        }

        [Test]
        public void AxuTestState_登记重置回调_ResetStatics_会执行()
        {
            var counter = 0;
            AxuTestState.Register("SampleEditMode.Counter", () => counter++);

            AxuTestState.ResetStatics();

            Assert.AreEqual(1, counter, "ResetStatics 应触发登记的重置回调");
            AxuTestState.Unregister("SampleEditMode.Counter");
        }

        [Test]
        public void LogAssert_预期错误_不判失败()
        {
            LogAssert.Expect(LogType.Error, "sample expected error");
            UnityEngine.Debug.LogError("sample expected error");
        }

        [Test]
        public void AxuLogWatch_捕获错误_HasErrors为真()
        {
            LogAssert.Expect(LogType.Error, "watched error");
            using (var watch = new AxuLogWatch())
            {
                UnityEngine.Debug.LogError("watched error");
                Assert.IsTrue(watch.HasErrors, "AxuLogWatch 应捕获期间的 Error 日志");
            }
        }
    }
}
