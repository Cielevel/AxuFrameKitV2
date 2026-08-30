using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuTestFramework
{
    /// <summary>
    /// 日志监视器：捕获监视窗口内出现的 Error / Exception / Assert 级日志（消息 + 堆栈）。
    /// 用 using（或显式 Dispose）划定监视窗口；需要"无错误"断言时优先用 <see cref="ExpectNoErrors"/>。
    /// 注意：只做被动记录，不与 LogAssert 交互——预期内的错误日志仍应先经 LogAssert.Expect 声明。
    /// </summary>
    public class AxuLogWatch : IDisposable
    {
        /// <summary>最多捕获的错误条数（防异常刷屏时无界增长），超出部分丢弃并计入 <see cref="DroppedCount"/>。</summary>
        public const int MaxCapturedErrors = 512;

        private readonly object _gate = new object();
        private readonly List<LogEntry> _errors = new List<LogEntry>();
        private int _droppedCount;
        private bool _disposed;

        /// <summary>创建监视器并开始捕获日志。</summary>
        public AxuLogWatch()
        {
            // 只挂 Threaded 版本：它接收全部日志（含主线程），避免与 logMessageReceived 同时挂接导致重复计数
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        /// <summary>是否捕获到至少一条 Error / Exception / Assert。</summary>
        public bool HasErrors
        {
            get
            {
                lock (_gate)
                {
                    return _errors.Count > 0;
                }
            }
        }

        /// <summary>捕获到的错误条目快照（线程安全副本，每次取快照会复制一份列表）。</summary>
        public IReadOnlyList<LogEntry> Errors
        {
            get
            {
                lock (_gate)
                {
                    return _errors.ToArray();
                }
            }
        }

        /// <summary>因超出 <see cref="MaxCapturedErrors"/> 上限而被丢弃的条数。</summary>
        public int DroppedCount
        {
            get
            {
                lock (_gate)
                {
                    return _droppedCount;
                }
            }
        }

        /// <summary>
        /// 创建"无错误"作用域（<see cref="AxuNoErrorsScope"/>）：using 结束时若捕获到错误，
        /// 经 Assert.Fail 令测试失败并附汇总消息。
        /// </summary>
        public static AxuNoErrorsScope ExpectNoErrors()
        {
            return new AxuNoErrorsScope(new AxuLogWatch());
        }

        /// <summary>结束监视并反挂回调；可安全重复调用。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        /// <summary>日志回调（可能在非主线程触发），仅收集 Error / Exception / Assert 三级。</summary>
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            lock (_gate)
            {
                if (_errors.Count >= MaxCapturedErrors)
                {
                    _droppedCount++;
                    return;
                }
                _errors.Add(new LogEntry(condition, stackTrace, type));
            }
        }
    }
}
