namespace AxuFrameKit.Common.Utility.Timer
{
    /// <summary>
    /// 倒计时静态入口类
    /// </summary>
    public static class Timers
    {
        private static ICountdownManager CdManager => CountdownManager.Instance;

        private static IStopwatchManager SwManager => StopwatchManager.Instance;

        /// <summary>
        /// 启动一个倒计时
        /// </summary>
        /// <param name="time">倒计时时长（秒）</param>
        /// <returns>计时器句柄，可用于通过链式调用绑定回调或控制状态</returns>
        public static ICountdownHandle StartCd(float time) => CdManager.StartCd(time);

        /// <summary>
        /// 启动一个纯粹的正计时器（无限计时）
        /// </summary>
        /// <returns>计时器句柄，可用于通过链式调用绑定回调或控制状态</returns>
        public static IStopwatchHandle StartStopwatch() => SwManager.StartStopwatch();
    }
}