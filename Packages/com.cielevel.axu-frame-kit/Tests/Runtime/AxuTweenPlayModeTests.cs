using System.Collections;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] AxuTween 基础补间：Custom float 补间经 AxuTweenRunner 每帧驱动到达终点。
    /// Runner 为 SingletonMonoCrDs（自动创建 + DontDestroyOnLoad），TearDown 显式销毁防残留。
    /// </summary>
    [Category("Axu/Play")]
    public class AxuTweenPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            AxuTestState.Register("Tween/AxuTweenRunner", DestroyRunner);
            yield return AxuWait.ForFrames(1);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyRunner();
            yield return null;
        }

        /// <summary>销毁自动创建的 Runner GameObject（幂等；静态 _instance 随 Unity-null 语义自然失效）。</summary>
        private static void DestroyRunner()
        {
            var go = GameObject.Find("[AxuTweenRunner]");
            if (go != null)
                Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator AxuTween_CustomFloat_按缓动到达终点()
        {
            // Arrange
            // 注意：AxuTweenRunner 在完成当帧 Tik 内即 Recycle+Reset（IsCompleted/RunTime 复位），
            // 测试侧无法事后轮询到 IsCompleted==true；完成通知必须使用 OnComplete 回调
            // （其在回收前同步触发）。此语义为 M4 测试模板要点。
            bool completed = false;
            float value = 0f;

            // Act：0.2 秒补间 0 → 10，Start 后由 Runner 每帧驱动
            var tween = AxuTween.Custom(0f, 10f, 0.2f, v => value = v)
                .OnComplete(() => completed = true)
                .Start();
            yield return AxuWait.Until(() => completed, 5f);

            // Assert：完成回调已触发，且值到达终点（完成帧进度钳制为 1，InCubic(1)=1）
            Assert.That(completed, Is.True, "OnComplete 回调应在补间完成时触发");
            Assert.That(value, Is.EqualTo(10f).Within(0.001f));
        }
    }
}
