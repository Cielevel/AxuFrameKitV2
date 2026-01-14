using System;

namespace AxuFrameKit.Common.Utility.Timer
{
    /// <summary>
    /// 倒计时静态入口类
    /// </summary>
    public static class CdTimer
    {
        private static ICdManager Manager => CdManager.Instance;

        /// <summary>
        /// 启动一个倒计时
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>计时器句柄，可用于取消或控制计时器</returns>
        public static ICdHandle StartCd(float time, Action onComplete)
            => Manager.StartCd(time, onComplete);

        /// <summary>
        /// 启动一个倒计时，带 Tick 回调
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <param name="onTick">每帧回调，参数为剩余时间</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>计时器句柄，可用于取消或控制计时器</returns>
        public static ICdHandle StartCd(float time, Action<float> onTick, Action onComplete)
            => Manager.StartCd(time, onTick, onComplete);
    }
}