using System;

namespace AxuFrameKit._Core.Utility.Common
{
    /// <summary>
    /// 统一日志记录器（纯 C# 实现，不依赖任何引擎）。
    /// 默认输出到控制台；宿主环境可替换输出目标——
    /// Unity 侧由 Common 层的 LoggerUnityWire（运行时）与 Editor 层的 LoggerWire 接线到 UnityEngine.Debug。
    /// </summary>
    public static class Logger
    {
        /// <summary> 常规日志输出目标 </summary>
        public static Action<object> LogSink { get; set; } = message => Console.WriteLine("LOG:" + message);

        /// <summary> 警告日志输出目标 </summary>
        public static Action<object> WarningSink { get; set; } = message => Console.WriteLine("WARNING:" + message);

        /// <summary> 错误日志输出目标 </summary>
        public static Action<object> ErrorSink { get; set; } = message => Console.WriteLine("ERROR:" + message);

        /// <summary> 异常日志输出目标 </summary>
        public static Action<Exception> ExceptionSink { get; set; } = exception => Console.WriteLine("EXCEPTION:" + exception);

        /// <summary> 断言日志输出目标 </summary>
        public static Action<object> AssertionSink { get; set; } = assertion => Console.WriteLine("ASSERTION:" + assertion);

        /// <summary>
        /// 一次性替换全部输出目标（传 null 的级别保持原样）
        /// </summary>
        public static void SetSinks(Action<object> log, Action<object> warning, Action<object> error,
            Action<Exception> exception, Action<object> assertion)
        {
            if (log != null) LogSink = log;
            if (warning != null) WarningSink = warning;
            if (error != null) ErrorSink = error;
            if (exception != null) ExceptionSink = exception;
            if (assertion != null) AssertionSink = assertion;
        }

        /// <summary> 打印常规日志 </summary>
        public static void Log(object message) => LogSink(message);

        /// <summary> 打印警告日志 </summary>
        public static void Warning(object message) => WarningSink(message);

        /// <summary> 打印错误日志 </summary>
        public static void Error(object message) => ErrorSink(message);

        /// <summary> 打印异常详情 </summary>
        public static void Exception(Exception exception) => ExceptionSink(exception);

        /// <summary> 打印断言日志 </summary>
        public static void Assertion(object assertion) => AssertionSink(assertion);
    }
}
