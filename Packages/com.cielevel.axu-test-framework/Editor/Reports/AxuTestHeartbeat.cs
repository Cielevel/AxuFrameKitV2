using System;
using UnityEditor;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// UTF 运行心跳：运行期间（<see cref="AxuTestReportWriter.RunStarted"/> → <c>RunFinished</c>）按固定间隔
    /// 刷新 <c>Temp/axu_test_run_status.json</c> 的 <c>HeartbeatUtc</c>。
    /// <para>存在原因：互斥闸靠状态文件判断"是否已有运行在途"，而**启动时间不足以判活**——
    /// 单条 PlayMode 用例可以跑十几分钟（互斥闸的陈旧阈值 15 分钟），仅按启动时间判定会把仍在运行的
    /// 测试当作陈旧残留并放行并发运行。心跳是唯一的活信号。</para>
    /// <para>域重载安全：运行开始回调发生在重载后的新域，本类在此时挂载；进程消失（崩溃）后心跳自然停止，
    /// 状态文件随时间越过陈旧阈值并自动放行——这正是期望的恢复路径。</para>
    /// <para>挂载是幂等的（编辑器窗口重载/多次运行不会重复挂钩），<c>RunFinished</c> 与域重载都会摘除。</para>
    /// </summary>
    public static class AxuTestHeartbeat
    {
        /// <summary>心跳写入间隔（秒）：远小于 15 分钟的陈旧阈值，也不会造成可见的写盘开销。</summary>
        public const double IntervalSeconds = 5.0;

        static bool _armed;
        static DateTime _nextWriteUtc = DateTime.MinValue;

        /// <summary>挂载心跳（运行开始；重复调用无副作用）。</summary>
        public static void Arm()
        {
            if (_armed)
                return;
            _armed = true;
            _nextWriteUtc = DateTime.MinValue;
            EditorApplication.update += Tick;
        }

        /// <summary>摘除心跳（运行结束；未挂载时无副作用）。</summary>
        public static void Disarm()
        {
            if (!_armed)
                return;
            _armed = false;
            EditorApplication.update -= Tick;
        }

        static void Tick()
        {
            if (DateTime.UtcNow < _nextWriteUtc)
                return;
            _nextWriteUtc = DateTime.UtcNow.AddSeconds(IntervalSeconds);
            AxuTestRunStatus.TouchHeartbeat();
        }
    }
}
