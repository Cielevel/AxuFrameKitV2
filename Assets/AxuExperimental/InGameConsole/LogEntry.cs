using UnityEngine;

namespace AxuExperimental.InGameConsole
{
    /// <summary>
    /// 日志条目的数据结构
    /// </summary>
    public struct LogEntry
    {
        public string Message { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }
        public string Timestamp { get; }
        public int CollapseCount { get; set; }
        public int Index { get; }
        
        // 用于Collapse模式的哈希值
        private readonly int _hash;
        
        public LogEntry(string message, string stackTrace, LogType logType, int index)
        {
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            LogType = logType;
            Timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            CollapseCount = 1;
            Index = index;
            
            // 计算用于去重的哈希值 (message + logType)
            _hash = (message?.GetHashCode() ?? 0) ^ ((int)logType * 397);
        }
        
        /// <summary>
        /// 获取用于Collapse模式的唯一标识符
        /// </summary>
        public int GetCollapseKey() => _hash;
        
        /// <summary>
        /// 获取日志类型的颜色
        /// </summary>
        public string GetColorHex()
        {
            return LogType switch
            {
                UnityEngine.LogType.Error => "#ff6b6b",      // 红色
                UnityEngine.LogType.Assert => "#ff6b6b",     // 红色
                UnityEngine.LogType.Warning => "#ffd93d",    // 黄色
                UnityEngine.LogType.Exception => "#ff4757",  // 深红色
                _ => "#e8e8e8"                               // 白色 (Log)
            };
        }
        
        /// <summary>
        /// 获取日志类型图标
        /// </summary>
        public string GetIcon()
        {
            return LogType switch
            {
                UnityEngine.LogType.Error => "✕",
                UnityEngine.LogType.Assert => "✕",
                UnityEngine.LogType.Warning => "⚠",
                UnityEngine.LogType.Exception => "❌",
                _ => "ℹ"
            };
        }
        
        /// <summary>
        /// 获取格式化的显示文本（第一行）
        /// </summary>
        public string GetDisplayText()
        {
            // 只显示第一行消息
            var firstLine = Message.Split('\n')[0];
            if (firstLine.Length > 200)
            {
                firstLine = firstLine.Substring(0, 200) + "...";
            }
            return firstLine;
        }
    }
}
