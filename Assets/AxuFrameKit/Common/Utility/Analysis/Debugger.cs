using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace AxuFrameKit.Common.Utility.Analysis
{
    // 注意：Rider中会显示 显式实参被传递给具有调用方信息特性的形参；但不必做任何修改，因为需要将外部的调用信息传递进来，避免日志记录点的信息丢失

    public static class Debugger
    {
        #region 基础日志方法

        public static void Log(object message,
                               bool writeLog = false,
                               [CallerMemberName] string memberName = "",
                               [CallerFilePath] string sourceFilePath = "",
                               [CallerLineNumber] int sourceLineNumber = 0)
        {
            string formattedMessage = FormatMessage(message, sourceFilePath, sourceLineNumber, memberName);
            Debug.Log(formattedMessage);
            if (writeLog)
                WriteLogSafe(message, LogType.Log, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogWarning(object message,
                                      bool writeLog = false,
                                      [CallerMemberName] string memberName = "",
                                      [CallerFilePath] string sourceFilePath = "",
                                      [CallerLineNumber] int sourceLineNumber = 0)
        {
            string formattedMessage = FormatMessage(message, sourceFilePath, sourceLineNumber, memberName);
            Debug.LogWarning(formattedMessage);
            if (writeLog)
                WriteLogSafe(message, LogType.Warning, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogError(object message,
                                    bool writeLog = false,
                                    [CallerMemberName] string memberName = "",
                                    [CallerFilePath] string sourceFilePath = "",
                                    [CallerLineNumber] int sourceLineNumber = 0)
        {
            string formattedMessage = FormatMessage(message, sourceFilePath, sourceLineNumber, memberName);
            Debug.LogError(formattedMessage);
            if (writeLog)
                WriteLogSafe(message, LogType.Error, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogAssertion(object message,
                                        bool writeLog = false,
                                        [CallerMemberName] string memberName = "",
                                        [CallerFilePath] string sourceFilePath = "",
                                        [CallerLineNumber] int sourceLineNumber = 0)
        {
            string formattedMessage = FormatMessage(message, sourceFilePath, sourceLineNumber, memberName);
            Debug.LogAssertion(formattedMessage);
            if (writeLog)
                WriteLogSafe(message, LogType.Assert, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogException(Exception exception,
                                        bool writeLog = false,
                                        [CallerMemberName] string memberName = "",
                                        [CallerFilePath] string sourceFilePath = "",
                                        [CallerLineNumber] int sourceLineNumber = 0)
        {
            string formattedMessage = FormatMessage(exception?.ToString() ?? "Unknown Exception", sourceFilePath,
                                                    sourceLineNumber, memberName);
            Debug.LogException(exception);
            if (writeLog)
                WriteLogSafe(exception?.ToString() ?? "Unknown Exception", LogType.Exception, memberName, sourceFilePath, sourceLineNumber);
        }

        #endregion

        #region 集合日志方法 - 扩展功能

        /// <summary>
        /// 记录数组或集合内容，使用指定分隔符
        /// </summary>
        public static void LogArray(char separator, IEnumerable array, bool writeLog = false,
                                    [CallerMemberName] string memberName = "",
                                    [CallerFilePath] string sourceFilePath = "",
                                    [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (array == null)
            {
                Log("Array is null", writeLog, memberName, sourceFilePath, sourceLineNumber);
                return;
            }

            string content = string.Join(separator.ToString(), array.Cast<object>());
            Log($"Array Content: [{content}]", writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 记录字典内容，格式化键值对显示
        /// </summary>
        public static void LogDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary, bool writeLog = false,
                                                       [CallerMemberName] string memberName = "",
                                                       [CallerFilePath] string sourceFilePath = "",
                                                       [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (dictionary == null)
            {
                Log("Dictionary is null", writeLog, memberName, sourceFilePath, sourceLineNumber);
                return;
            }

            if (dictionary.Count == 0)
            {
                Log("Dictionary is empty", writeLog, memberName, sourceFilePath, sourceLineNumber);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Dictionary Content ({dictionary.Count}， items):");
            foreach (var kvp in dictionary)
                sb.AppendLine($"  [{kvp.Key}] = {kvp.Value}");

            Log(sb.ToString(), writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 记录对象的详细信息，包括字段和属性（内部使用反射实现，请勿高频调用）
        /// </summary>
        public static void LogObject(object obj, bool includePrivate = false, bool writeLog = false,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (obj == null)
            {
                Log("Object is null", writeLog, memberName, sourceFilePath, sourceLineNumber);
                return;
            }

            Type type = obj.GetType();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Object Info: {type.Name}");

            // 记录字段
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (includePrivate)
                bindingFlags |= BindingFlags.NonPublic;

            var fields = type.GetFields(bindingFlags);
            if (fields.Length > 0)
            {
                sb.AppendLine("Fields:");
                foreach (var field in fields)
                {
                    try
                    {
                        object value = field.GetValue(obj);
                        sb.AppendLine($"  {field.Name}: {value ?? "null"}");
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine($"  {field.Name}: <Error getting value: {e.Message}>");
                    }
                }
            }

            // 记录属性
            var properties = type.GetProperties(bindingFlags);
            if (properties.Length > 0)
            {
                sb.AppendLine("Properties:");
                foreach (var prop in properties.Where(p => p.CanRead))
                {
                    try
                    {
                        object value = prop.GetValue(obj);
                        sb.AppendLine($"  {prop.Name}: {value ?? "null"}");
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine($"  {prop.Name}: <Error getting value: {e.Message}>");
                    }
                }
            }

            Log(sb.ToString(), writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 记录性能计时信息
        /// </summary>
        public static void LogTiming(string operationName, float timeInSeconds, bool writeLog = false,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
        {
            string message =
                $"Performance: {operationName} took {timeInSeconds:F4} seconds ({timeInSeconds * 1000:F2} ms)";
            Log(message, writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 条件日志记录Log - 只在条件为真时记录
        /// </summary>
        public static void LogIf(bool condition, object message, bool writeLog = false,
                                 [CallerMemberName] string memberName = "",
                                 [CallerFilePath] string sourceFilePath = "",
                                 [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (condition)
                Log(message, writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 条件日志记录Warn - 只在条件为真时记录
        /// </summary>
        public static void LogWarnIf(bool condition, object message, bool writeLog = false,
                                 [CallerMemberName] string memberName = "",
                                 [CallerFilePath] string sourceFilePath = "",
                                 [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (condition)
                LogWarning(message, writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 条件日志记录Error - 只在条件为真时记录
        /// </summary>
        public static void LogErrorIf(bool condition, object message, bool writeLog = false,
                                 [CallerMemberName] string memberName = "",
                                 [CallerFilePath] string sourceFilePath = "",
                                 [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (condition)
                LogError(message, writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 记录方法进入和退出 - 用于追踪方法调用流程
        /// </summary>
        public static IDisposable LogMethodScope(string methodName = "", bool writeLog = false, bool opt = false,
                                                 [CallerMemberName] string memberName = "",
                                                 [CallerFilePath] string sourceFilePath = "",
                                                 [CallerLineNumber] int sourceLineNumber = 0)
        {
            string actualMethodName = string.IsNullOrEmpty(methodName) ? memberName : methodName;

            // 按照opt，选择是否是性能优化的调用
            return opt ? MethodScopePool.Get(actualMethodName, writeLog, memberName, sourceFilePath, sourceLineNumber)
                : new MethodScope(actualMethodName, writeLog, memberName, sourceFilePath, sourceLineNumber);
        }

        #endregion

        #region 辅助方法和类

        /// <summary>
        /// 格式化日志消息的统一方法
        /// </summary>
        private static string FormatMessage(object message, string sourceFilePath, int sourceLineNumber,
                                            string memberName)
        {
            return $"[{Path.GetFileNameWithoutExtension(sourceFilePath)}({sourceLineNumber}):{memberName}()] {message}";
        }

        /// <summary>
        /// 安全的日志写入方法，包含异常处理
        /// </summary>
        private static void WriteLogSafe(object message, LogType logType, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            try
            {
                WriteLog(message, logType, memberName, sourceFilePath, sourceLineNumber);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write log to file: {e.Message}");
            }
        }

        /// <summary>
        /// 对象池管理类 - 用于管理MethodScope实例的复用
        /// </summary>
        private static class MethodScopePool
        {
            // 池中最大对象数量，防止内存无限增长
            private const int MaxPoolSize = 50;

            // 使用线程安全的栈来存储可复用的MethodScope实例
            private static readonly global::System.Collections.Concurrent.ConcurrentStack<MethodScope> Pool = new();

            /// <summary>
            /// 从对象池获取MethodScope实例
            /// </summary>
            public static MethodScope Get(string methodName, bool writeLog, string memberName, string sourceFilePath, int sourceLineNumber)
            {
                // 尝试从池中获取现有实例
                if (Pool.TryPop(out MethodScope scope))
                {
                    // 重新初始化现有实例 - 这是关键步骤
                    scope.Initialize(methodName, writeLog, memberName, sourceFilePath, sourceLineNumber);
                    return scope;
                }

                // 池中没有可用实例，创建新的
                return new MethodScope(methodName, writeLog, memberName, sourceFilePath, sourceLineNumber);
            }

            /// <summary>
            /// 将MethodScope实例返回到对象池
            /// </summary>
            public static void Return(MethodScope scope)
            {
                // 只有在池未满时才回收，避免内存无限增长
                if (Pool.Count < MaxPoolSize)
                {
                    // 清理实例状态，为下次使用做准备
                    scope.Reset();
                    Pool.Push(scope);
                }
                // 如果池已满，让GC自然回收这个实例
            }
        }

        /// <summary>
        /// 方法作用域追踪类 - 实现IDisposable来自动记录方法退出
        /// 现在支持对象池复用以优化内存分配
        /// </summary>
        private class MethodScope : IDisposable
        {
            // 这些字段现在不再是readonly，因为需要支持重复初始化
            private string _methodName;
            private bool _writeLog;
            private string _memberName;
            private string _sourceFilePath;
            private int _sourceLineNumber;
            private DateTime _startTime;
            private bool _disposed; // 添加disposed标志防止重复处理

            // 构造函数 - 现在只用于首次创建
            public MethodScope(string methodName, bool writeLog, string memberName, string sourceFilePath, int sourceLineNumber)
            {
                Initialize(methodName, writeLog, memberName, sourceFilePath, sourceLineNumber);
            }

            /// <summary>
            /// 初始化或重新初始化实例 - 这是对象池复用的关键方法
            /// </summary>
            public void Initialize(string methodName, bool writeLog, string memberName, string sourceFilePath, int sourceLineNumber)
            {
                _methodName = methodName;
                _writeLog = writeLog;
                _memberName = memberName;
                _sourceFilePath = sourceFilePath;
                _sourceLineNumber = sourceLineNumber;
                _startTime = DateTime.Now;
                _disposed = false; // 重置disposed状态

                // 记录方法进入
                Log($"→ Entering {_methodName}", _writeLog, _memberName, _sourceFilePath, _sourceLineNumber);
            }

            /// <summary>
            /// 清理实例状态，为对象池复用做准备
            /// </summary>
            public void Reset()
            {
                _methodName = null;
                _memberName = null;
                _sourceFilePath = null;
                _sourceLineNumber = 0;
                _writeLog = false;
                _disposed = false;
            }

            public void Dispose()
            {
                // 防止重复Dispose
                if (_disposed)
                    return;

                var duration = DateTime.Now - _startTime;
                Log($"← Exiting {_methodName} (Duration: {duration.TotalMilliseconds:F2}ms)", _writeLog, _memberName, _sourceFilePath, _sourceLineNumber);

                _disposed = true;

                // 将实例返回到对象池而不是等待GC回收
                MethodScopePool.Return(this);
            }
        }

        #endregion

        #region 文件写入系统

        public static void OpenLogFolder()
        {
            Application.OpenURL("file://" + _path);
        }

        private static string _path;
        private static readonly object _fileLock = new object();
        private const long MaxLOGFileSize = 10 * 1024 * 1024; // 10MB

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitPath()
        {
            // 创建专门的日志文件夹，而不是直接使用persistentDataPath根目录
            lock (_fileLock)
            {
                _path = Path.Combine(Application.persistentDataPath, "Logs");
            }

            // 确保日志目录存在 - 这是关键步骤！
            try
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                    Debug.Log($"Created log directory: {_path}");
                }
            }
            catch (Exception e)
            {
                // 如果无法创建专门的日志文件夹，回退到persistentDataPath根目录
                Debug.LogWarning($"Failed to create log directory: {e.Message}. Falling back to persistentDataPath root.");
                _path = Application.persistentDataPath;
            }
        }

        /// <summary>
        /// 写入日志到文件，支持文件大小限制和多副本创建
        /// 现在包含了完整的错误处理和目录创建逻辑
        /// </summary>
        private static void WriteLog(object message, LogType logType, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            lock (_fileLock)
            {
                try
                {
                    string baseFileName = $"Log-{DateTime.Today:yyyy-MM-dd}";
                    string fileName = GetAvailableLogFileName(baseFileName);
                    string fullPath = Path.Combine(_path, fileName);

                    // 构建详细的日志信息
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string fileInfo = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}:{sourceLineNumber}, {memberName}()";
                    string formattedLog = $"[{logType.ToString()}, {timestamp}, {fileInfo}]: {message}\n";

                    // 写入文件
                    File.AppendAllText(fullPath, formattedLog);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.LogError($"Log write failed - Access denied: {ex.Message}");
                }
                catch (DirectoryNotFoundException ex)
                {
                    Debug.LogError($"Log write failed - Directory not found: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Debug.LogError($"Log write failed - IO error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Log write failed - Unexpected error: {ex.Message}");
                }
            }

            /// <summary>
            /// 获取可用的日志文件名，如果文件过大则创建新副本
            /// </summary>
            static string GetAvailableLogFileName(string baseFileName)
            {
                string fileName = $"{baseFileName}.txt";
                string fullPath = Path.Combine(_path, fileName);

                // 检查文件大小，如果超过限制则创建新副本
                if (File.Exists(fullPath))
                {
                    FileInfo fileInfo = new FileInfo(fullPath);
                    if (fileInfo.Length >= MaxLOGFileSize)
                    {
                        int copyNumber = 1;
                        do
                        {
                            fileName = $"{baseFileName}_{copyNumber:D2}.txt";
                            fullPath = Path.Combine(_path, fileName);
                            copyNumber++;
                        } while (File.Exists(fullPath) && new FileInfo(fullPath).Length >= MaxLOGFileSize);
                    }
                }

                return fileName;
            }

            #endregion
        }
    }
}