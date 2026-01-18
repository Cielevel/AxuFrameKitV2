using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Analysis.InGameConsole
{
    /// <summary>
    /// InGameConsole核心逻辑类
    /// 负责日志收集、过滤和管理
    /// </summary>
    public class InGameConsole : IDisposable
    {
        // 配置
        private readonly InGameConsoleManager _manager;
        
        // 日志数据
        private readonly List<LogEntry> _allLogs = new();
        private readonly List<LogEntry> _filteredLogs = new();
        private readonly Dictionary<int, int> _collapseMap = new();  // hash -> index in _allLogs
        
        // 计数器
        private int _logCount;
        private int _warningCount;
        private int _errorCount;
        private int _logIndex;
        
        // 过滤状态
        private bool _showLogs = true;
        private bool _showWarnings = true;
        private bool _showErrors = true;
        private bool _collapseEnabled;
        private string _searchFilter = string.Empty;
        
        // UI
        private InGameConsoleUI _ui;
        
        // 事件
        public event Action OnLogsChanged;
        public event Action<int, int, int> OnCountsChanged;  // log, warning, error
        
        // Properties
        public bool IsVisible => _ui?.IsVisible ?? false;
        public IReadOnlyList<LogEntry> FilteredLogs => _filteredLogs;
        public int LogCount => _logCount;
        public int WarningCount => _warningCount;
        public int ErrorCount => _errorCount;
        
        public bool ShowLogs
        {
            get => _showLogs;
            set
            {
                if (_showLogs != value)
                {
                    _showLogs = value;
                    ApplyFilter();
                }
            }
        }
        
        public bool ShowWarnings
        {
            get => _showWarnings;
            set
            {
                if (_showWarnings != value)
                {
                    _showWarnings = value;
                    ApplyFilter();
                }
            }
        }
        
        public bool ShowErrors
        {
            get => _showErrors;
            set
            {
                if (_showErrors != value)
                {
                    _showErrors = value;
                    ApplyFilter();
                }
            }
        }
        
        public bool CollapseEnabled
        {
            get => _collapseEnabled;
            set
            {
                if (_collapseEnabled != value)
                {
                    _collapseEnabled = value;
                    ApplyFilter();
                }
            }
        }
        
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value ?? string.Empty;
                    ApplyFilter();
                }
            }
        }
        
        public InGameConsole(InGameConsoleManager manager)
        {
            _manager = manager;
        }
        
        /// <summary>
        /// 初始化Console UI
        /// </summary>
        public void Initialize()
        {
            _ui = new InGameConsoleUI(this, _manager);
            _ui.Initialize();
        }
        
        /// <summary>
        /// 添加日志条目
        /// </summary>
        public void AddLog(string message, string stackTrace, LogType logType)
        {
            // 限制日志数量
            if (_allLogs.Count >= _manager.MaxLogCount)
            {
                // 移除最旧的日志
                RemoveOldestLog();
            }
            
            var entry = new LogEntry(
                message, 
                _manager.CaptureStackTrace ? stackTrace : string.Empty, 
                logType, 
                _logIndex++
            );
            
            // Collapse处理
            int collapseKey = entry.GetCollapseKey();
            if (_collapseMap.TryGetValue(collapseKey, out int existingIndex))
            {
                // 更新已存在条目的计数
                var existingEntry = _allLogs[existingIndex];
                existingEntry.CollapseCount++;
                _allLogs[existingIndex] = existingEntry;
            }
            else
            {
                _allLogs.Add(entry);
                _collapseMap[collapseKey] = _allLogs.Count - 1;
            }
            
            // 更新计数
            UpdateCounts(logType, 1);
            
            // 应用过滤并通知UI
            ApplyFilter();
        }
        
        /// <summary>
        /// 移除最旧的日志
        /// </summary>
        private void RemoveOldestLog()
        {
            if (_allLogs.Count == 0) return;
            
            var oldest = _allLogs[0];
            UpdateCounts(oldest.LogType, -1);
            
            // 从collapse map中移除
            int collapseKey = oldest.GetCollapseKey();
            _collapseMap.Remove(collapseKey);
            
            _allLogs.RemoveAt(0);
            
            // 更新collapse map中的索引
            var keysToUpdate = new List<int>(_collapseMap.Keys);
            foreach (var key in keysToUpdate)
            {
                if (_collapseMap[key] > 0)
                {
                    _collapseMap[key]--;
                }
            }
        }
        
        /// <summary>
        /// 更新日志计数
        /// </summary>
        private void UpdateCounts(LogType logType, int delta)
        {
            switch (logType)
            {
                case LogType.Log:
                    _logCount += delta;
                    break;
                case LogType.Warning:
                    _warningCount += delta;
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    _errorCount += delta;
                    break;
            }
            
            OnCountsChanged?.Invoke(_logCount, _warningCount, _errorCount);
        }
        
        /// <summary>
        /// 应用过滤条件
        /// </summary>
        public void ApplyFilter()
        {
            _filteredLogs.Clear();
            
            bool hasSearchFilter = !string.IsNullOrEmpty(_searchFilter);
            var seenHashes = new HashSet<int>();
            
            foreach (var log in _allLogs)
            {
                // 类型过滤
                if (!ShouldShowLogType(log.LogType)) continue;
                
                // 搜索过滤
                if (hasSearchFilter && !log.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                // Collapse过滤
                if (_collapseEnabled)
                {
                    int hash = log.GetCollapseKey();
                    if (seenHashes.Contains(hash)) continue;
                    seenHashes.Add(hash);
                }
                
                _filteredLogs.Add(log);
            }
            
            OnLogsChanged?.Invoke();
        }
        
        /// <summary>
        /// 判断是否应该显示某类型的日志
        /// </summary>
        private bool ShouldShowLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Log => _showLogs,
                LogType.Warning => _showWarnings,
                LogType.Error => _showErrors,
                LogType.Assert => _showErrors,
                LogType.Exception => _showErrors,
                _ => true
            };
        }
        
        /// <summary>
        /// 清除所有日志
        /// </summary>
        public void Clear()
        {
            _allLogs.Clear();
            _filteredLogs.Clear();
            _collapseMap.Clear();
            _logCount = 0;
            _warningCount = 0;
            _errorCount = 0;
            
            OnCountsChanged?.Invoke(0, 0, 0);
            OnLogsChanged?.Invoke();
        }
        
        /// <summary>
        /// 获取指定索引的日志详情
        /// </summary>
        public LogEntry? GetLogAt(int index)
        {
            if (index >= 0 && index < _filteredLogs.Count)
            {
                return _filteredLogs[index];
            }
            return null;
        }
        
        /// <summary>
        /// 显示Console
        /// </summary>
        public void Show() => _ui?.Show();
        
        /// <summary>
        /// 隐藏Console
        /// </summary>
        public void Hide() => _ui?.Hide();
        
        /// <summary>
        /// 切换显示/隐藏状态
        /// </summary>
        public void Toggle() => _ui?.Toggle();
        
        /// <summary>
        /// 销毁资源
        /// </summary>
        public void Dispose()
        {
            _ui?.Dispose();
            _allLogs.Clear();
            _filteredLogs.Clear();
            _collapseMap.Clear();
        }
    }
}
