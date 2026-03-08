using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Net
{
    internal sealed class SocketUnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> Queue = new();
        private static SocketUnityMainThreadDispatcher _instance;
        private static int _mainThreadId;

        public static bool IsReady => _instance != null;
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            EnsureInstance();
        }

        public static void Post(Action action)
        {
            if (action == null)
                return;

            Queue.Enqueue(action);

            if (_instance == null && IsMainThread)
                EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
                return;

            var go = new GameObject("[SocketMainThreadDispatcher]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SocketUnityMainThreadDispatcher>();
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void Update()
        {
            while (Queue.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
