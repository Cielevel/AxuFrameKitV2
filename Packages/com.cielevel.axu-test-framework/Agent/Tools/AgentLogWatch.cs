using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 日志监视器（v1 <c>Runtime/Logging/AxuLogWatch</c> 思路移植，见 CHANGELOG）：
    /// 订阅 <c>Application.logMessageReceivedThreaded</c>，Error/Exception/Assert 计入 ErrorCount、
    /// Warning 计入 WarningCount；保留最近 20 条错误快照（"condition (asset:line)"）供诊断——
    /// FIFO 淘汰：达到 <see cref="RecentErrorLimit"/> 后移除最旧、追加最新，被淘汰总数计入 DroppedCount。
    /// 用 using（或显式 Dispose）划定监视窗口；内部 lock 保证线程安全（回调可能在非主线程触发）。
    /// </summary>
    public sealed class AgentLogWatch : IDisposable
    {
        /// <summary>错误快照保留条数（FIFO 环形窗口大小；超出即淘汰最旧）。</summary>
        public const int RecentErrorLimit = 20;

        /// <summary>单条错误快照的截断长度（字符）。</summary>
        public const int EntryMaxLength = 500;

        readonly object _gate = new object();
        readonly List<string> _recentErrors = new List<string>(RecentErrorLimit);
        int _errorCount;
        int _warningCount;
        int _droppedCount;
        bool _disposed;

        /// <summary>创建监视器并开始捕获日志。</summary>
        public AgentLogWatch()
        {
            // 只挂 Threaded 版本：接收全部日志（含非主线程），避免与 logMessageReceived 同挂导致重复计数
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        /// <summary>捕获到的 Error/Exception/Assert 条数（自构造或最近一次 ResetCounts 起）。</summary>
        public int ErrorCount
        {
            get { lock (_gate) return _errorCount; }
        }

        /// <summary>捕获到的 Warning 条数（自构造或最近一次 ResetCounts 起）。</summary>
        public int WarningCount
        {
            get { lock (_gate) return _warningCount; }
        }

        /// <summary>最近错误快照（"condition (asset:line)"，FIFO 保留最新 RecentErrorLimit 条，单条截断 EntryMaxLength；线程安全副本）。</summary>
        public IReadOnlyList<string> RecentErrors
        {
            get { lock (_gate) return _recentErrors.ToArray(); }
        }

        /// <summary>因淘汰未保留的错误快照总条数（= ErrorCount − 当前保留条数；ResetCounts 一并清零）。</summary>
        public int DroppedCount
        {
            get { lock (_gate) return _droppedCount; }
        }

        /// <summary>清零计数与快照（稳定窗前重置基线用）。</summary>
        public void ResetCounts()
        {
            lock (_gate)
            {
                _errorCount = 0;
                _warningCount = 0;
                _recentErrors.Clear();
                _droppedCount = 0;
            }
        }

        /// <summary>结束监视并反挂回调；幂等，可安全重复调用。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    lock (_gate)
                    {
                        _errorCount++;
                        if (_recentErrors.Count >= RecentErrorLimit)
                        {
                            _recentErrors.RemoveAt(0); // FIFO：满则淘汰最旧，保留的恒为最近 N 条
                            _droppedCount++;
                        }
                        _recentErrors.Add(Format(condition, stackTrace));
                    }
                    break;
                case LogType.Warning:
                    lock (_gate) _warningCount++;
                    break;
            }
        }

        /// <summary>格式化 "condition (asset:line)"：位置信息取自堆栈首处 "(at ...)" 标记。</summary>
        static string Format(string condition, string stackTrace)
        {
            var location = ExtractFirstLocation(stackTrace);
            var text = string.IsNullOrEmpty(location) ? condition : condition + " (" + location + ")";
            if (text.Length <= EntryMaxLength) return text;
            return text.Substring(0, EntryMaxLength) + "…";
        }

        static string ExtractFirstLocation(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;

            var start = stackTrace.IndexOf("(at ", StringComparison.Ordinal);
            if (start < 0) return null;

            var end = stackTrace.IndexOf(')', start);
            if (end < 0) return null;

            return stackTrace.Substring(start + 4, end - start - 4);
        }
    }
}
