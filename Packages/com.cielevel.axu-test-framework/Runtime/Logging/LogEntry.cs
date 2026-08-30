using UnityEngine;

namespace AxuTestFramework
{
    /// <summary>
    /// 一条被 <see cref="AxuLogWatch"/> 捕获的日志条目（不可变）。
    /// </summary>
    public class LogEntry
    {
        /// <summary>创建一条日志条目。</summary>
        /// <param name="message">日志消息（Application.logMessageReceived 的 condition 参数）。</param>
        /// <param name="stackTrace">堆栈文本（可能为 null 或空字符串）。</param>
        /// <param name="type">日志级别（AxuLogWatch 只捕获 Error / Exception / Assert）。</param>
        public LogEntry(string message, string stackTrace, LogType type)
        {
            Message = message;
            StackTrace = stackTrace;
            Type = type;
        }

        /// <summary>日志消息。</summary>
        public string Message { get; }

        /// <summary>堆栈文本（可能为 null 或空字符串）。</summary>
        public string StackTrace { get; }

        /// <summary>日志级别。</summary>
        public LogType Type { get; }
    }
}
