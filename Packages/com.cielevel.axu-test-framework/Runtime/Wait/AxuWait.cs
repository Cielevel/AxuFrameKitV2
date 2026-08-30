using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace AxuTestFramework
{
    /// <summary>
    /// PlayMode 等待工具：条件等待、帧等待、秒等待，返回 IEnumerator 供 [UnityTest]（或 [UnitySetUp]）协程 yield 使用。
    /// 超时经 NUnit <see cref="Assert.Fail"/> 以清晰的超时消息终止测试。
    /// </summary>
    public static class AxuWait
    {
        /// <summary>
        /// 条件等待：每帧（或按 <paramref name="checkIntervalSeconds"/> 间隔）检查条件，成立即通过；
        /// 超过 <paramref name="timeoutSeconds"/> 仍未成立则调用 <see cref="Assert.Fail"/>（消息含超时与实际等待时长）。
        /// </summary>
        /// <param name="condition">待满足的条件（每轮检查时调用，抛出的异常会直接令测试失败）。</param>
        /// <param name="timeoutSeconds">超时秒数，必须大于 0。以真实时间（Time.realtimeSinceStartup）计时。</param>
        /// <param name="checkIntervalSeconds">检查间隔秒数；&lt;= 0 表示逐帧检查。&gt; 0 时按缩放时间挂起 WaitForSeconds。</param>
        public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, float checkIntervalSeconds = 0)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (timeoutSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, "timeoutSeconds 必须大于 0 秒。");

            // 间隔模式下只构造一次 WaitForSeconds，避免循环内逐轮分配
            WaitForSeconds intervalWait = checkIntervalSeconds > 0f ? new WaitForSeconds(checkIntervalSeconds) : null;
            float startRealtime = Time.realtimeSinceStartup;

            while (!condition())
            {
                float elapsed = Time.realtimeSinceStartup - startRealtime;
                if (elapsed >= timeoutSeconds)
                {
                    string intervalText = checkIntervalSeconds > 0f
                        ? $"检查间隔 {checkIntervalSeconds:0.###}s"
                        : "逐帧检查";
                    Assert.Fail(
                        $"AxuWait.Until 超时：条件在 {timeoutSeconds:0.###}s 内未成立" +
                        $"（实际等待 {elapsed:0.###}s，{intervalText}）。");
                }

                if (intervalWait != null)
                    yield return intervalWait;
                else
                    yield return null;
            }
        }

        /// <summary>
        /// 帧等待：逐帧挂起指定次数（yield null），共前进 <paramref name="frameCount"/> 帧。
        /// </summary>
        public static IEnumerator ForFrames(int frameCount)
        {
            if (frameCount < 0)
                throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "frameCount 不能为负。");

            for (int i = 0; i < frameCount; i++)
                yield return null;
        }

        /// <summary>
        /// 秒等待：按缩放时间挂起 <see cref="WaitForSeconds"/>；<paramref name="seconds"/> 为 0 时不等待。
        /// </summary>
        public static IEnumerator ForSeconds(float seconds)
        {
            if (seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "seconds 不能为负。");

            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
        }
    }
}
