using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行期 Console 环形缓冲（[InitializeOnLoad]，线程安全）。
    /// 挂 <c>Application.logMessageReceivedThreaded</c> 收集 Error / Exception / Assert / Warning 四级条目，
    /// 供报告的 console 节（计数 + 样本）采样。容量 200 条，超限淘汰最旧。
    /// <para>域重载后静态字段清空、本类重新订阅——PlayMode 运行中由 <c>AxuTestReportWriter.RunStarted</c>
    /// 调 <see cref="BeginRun"/> 恢复"运行中"标记，保证采样窗口正确。</para>
    /// </summary>
    [InitializeOnLoad]
    public static class AxuConsoleRing
    {
        /// <summary>缓冲容量（条）。</summary>
        public const int Capacity = 200;

        /// <summary>报告样本条数上限。</summary>
        public const int SampleLimit = 20;

        /// <summary>单条样本文本截断长度（字符）。</summary>
        public const int SampleMaxLength = 500;

        private static readonly object Gate = new object();
        private static readonly Queue<RingEntry> Entries = new Queue<RingEntry>(Capacity);
        private static bool _runActive;
        private static DateTime _runStartedUtc = DateTime.MinValue;

        static AxuConsoleRing()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        /// <summary>进入一次运行：清空缓冲并记录起始时刻（之后的条目才算"运行期间"）。线程安全。</summary>
        public static void BeginRun()
        {
            lock (Gate)
            {
                Entries.Clear();
                _runActive = true;
                _runStartedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>清空缓冲（不改变运行标记）。线程安全。</summary>
        public static void Clear()
        {
            lock (Gate)
            {
                Entries.Clear();
            }
        }

        /// <summary>
        /// 采样为报告 console 节：统计运行期间（<see cref="BeginRun"/> 之后收到的）Error 级与 Warning 级条目数，
        /// 并附上至多 <see cref="SampleLimit"/> 条样本（单条截断 <see cref="SampleMaxLength"/> 字符）。
        /// 无样本时 Samples 为 null（配合 JSON 序列化忽略空值）。
        /// </summary>
        public static ReportConsole Snapshot()
        {
            lock (Gate)
            {
                var console = new ReportConsole();
                var samples = new List<string>();
                foreach (var entry in Entries)
                {
                    if (_runActive && entry.ReceivedAtUtc < _runStartedUtc)
                        continue;

                    if (entry.IsError)
                        console.ErrorCount++;
                    else
                        console.WarningCount++;

                    if (samples.Count < SampleLimit)
                        samples.Add("[" + entry.Severity + "] " + Truncate(entry.Message));
                }

                if (samples.Count > 0)
                    console.Samples = samples;
                return console;
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            bool isError;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    isError = true;
                    break;
                case LogType.Warning:
                    isError = false;
                    break;
                default:
                    return; // Log / Info 不入环
            }

            lock (Gate)
            {
                Entries.Enqueue(new RingEntry
                {
                    Severity = type.ToString(),
                    IsError = isError,
                    Message = condition,
                    ReceivedAtUtc = DateTime.UtcNow
                });
                while (Entries.Count > Capacity)
                    Entries.Dequeue();
            }
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= SampleMaxLength)
                return text;
            return text.Substring(0, SampleMaxLength) + "…";
        }

        private struct RingEntry
        {
            public string Severity;
            public bool IsError;
            public string Message;
            public DateTime ReceivedAtUtc;
        }
    }
}
