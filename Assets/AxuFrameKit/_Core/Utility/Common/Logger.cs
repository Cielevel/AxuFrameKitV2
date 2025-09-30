namespace AxuFrameKit._Core.Utility.Common
{
    public static class Logger
    {
#if !UNITY_5_3_OR_NEWER
        public static void Log(object message) => System.Console.WriteLine("LOG:" + message);
        public static void Warning(object message) => System.Console.WriteLine("WARNING:" + message);
        public static void Error(object message) => System.Console.WriteLine("ERROR:" + message);
        public static void Exception(System.Exception exception) => System.Console.WriteLine("EXCEPTION:" + exception);
        public static void Assertion(object assertion) => System.Console.WriteLine("ASSERTION:" + assertion);
#endif

#if UNITY_5_3_OR_NEWER
        public static void Log(object message) => UnityEngine.Debug.Log(message);
        public static void Warning(object message) => UnityEngine.Debug.LogWarning(message);
        public static void Error(object message) => UnityEngine.Debug.LogError(message);
        public static void Exception(System.Exception exception) => UnityEngine.Debug.LogException(exception);
        public static void Assertion(object assertion) => UnityEngine.Debug.LogAssertion(assertion);
#endif
    }
}