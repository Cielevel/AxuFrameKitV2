namespace AxuFrameKit._Core.Utility.Common
{
    /// <summary>
    /// 统一日志记录器，支持 Unity 和控制台环境
    /// </summary>
    public static class Logger
    {
#if !UNITY_5_3_OR_NEWER
        /// <summary> 打印常规日志 </summary>
        public static void Log(object message) => System.Console.WriteLine("LOG:" + message);
        /// <summary> 打印警告日志 </summary>
        public static void Warning(object message) => System.Console.WriteLine("WARNING:" + message);
        /// <summary> 打印错误日志 </summary>
        public static void Error(object message) => System.Console.WriteLine("ERROR:" + message);
        /// <summary> 打印异常详情 </summary>
        public static void Exception(System.Exception exception) => System.Console.WriteLine("EXCEPTION:" + exception);
        /// <summary> 打印断言日志 </summary>
        public static void Assertion(object assertion) => System.Console.WriteLine("ASSERTION:" + assertion);
#endif

#if UNITY_5_3_OR_NEWER
        /// <summary> 打印常规日志 </summary>
        public static void Log(object message) => UnityEngine.Debug.Log(message);
        /// <summary> 打印警告日志 </summary>
        public static void Warning(object message) => UnityEngine.Debug.LogWarning(message);
        /// <summary> 打印错误日志 </summary>
        public static void Error(object message) => UnityEngine.Debug.LogError(message);
        /// <summary> 打印异常详情 </summary>
        public static void Exception(System.Exception exception) => UnityEngine.Debug.LogException(exception);
        /// <summary> 打印断言日志 </summary>
        public static void Assertion(object assertion) => UnityEngine.Debug.LogAssertion(assertion);
#endif
    }
}