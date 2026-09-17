using System;
using System.Collections;
using System.Collections.Concurrent;
using AxuFrameKit.Common.Utility.Multiton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.TaskQ
{
    public interface ICoroutineQ
    {
        ICoroutineQ EnQ(IEnumerator coroutine);

        ICoroutineQ EnQ(Action action);

        ICoroutineQ Delay(float duration, Action action);
        ICoroutineQ Delay(float duration);

        ICoroutineQ Insert(IEnumerator coroutine);
        ICoroutineQ Insert(Action action);

        void Run();

        void Pause();

        void StopAndClear();
    }

    /// <summary>
    /// 协程队列；支持从不同线程入队；但是所有执行都会在主线程上进行。
    /// 子协程异常只跳过该项并继续队列，不会终止父 Loop（修复异常后 _isRunning 残留导致的假死）。
    /// </summary>
    public class CoroutineQ : MultitonMono<CoroutineQ>, ICoroutineQ
    {
        private bool _isRunning = false;

        private Coroutine _loopCoroutine;

        private ConcurrentQueue<IEnumerator> _queue;
        private ConcurrentQueue<IEnumerator> _insertQ;

        protected override void OnThisCreated()
        {
            base.OnThisCreated();

            _queue = new ConcurrentQueue<IEnumerator>();
            _insertQ = new ConcurrentQueue<IEnumerator>();
        }

        protected override void OnThisDestroyed()
        {
            base.OnThisDestroyed();

            StopAndClear();
        }

        #region Entry

        public ICoroutineQ EnQ(IEnumerator coroutine)
        {
            _queue.Enqueue(coroutine);
            return this;
        }

        public ICoroutineQ EnQ(Action action)
        {
            _queue.Enqueue(IEAction(action));
            return this;
        }

        public ICoroutineQ Delay(float duration, Action action)
        {
            if (Mathf.Approximately(duration, 0f) || duration <= 0)
                EnQ(IEAction(action));
            else
                EnQ(IEDelay(duration, action));
            return this;
        }

        public ICoroutineQ Delay(float duration)
        {
            EnQ(IEDelay(duration));
            return this;
        }

        public ICoroutineQ Insert(IEnumerator coroutine)
        {
            _insertQ.Enqueue(coroutine);
            return this;
        }

        public ICoroutineQ Insert(Action action)
        {
            _insertQ.Enqueue(IEAction(action));
            return this;
        }

        #endregion

        private IEnumerator Loop()
        {
            try
            {
                while (_isRunning)
                {
                    // 插入队列优先消费，全部经安全宿主步进，异常不再波及父 Loop
                    while (0 < _insertQ.Count && _insertQ.TryDequeue(out var insertNext))
                        yield return SafeHost(insertNext);

                    if (0 < _queue.Count && _queue.TryDequeue(out var next))
                        yield return SafeHost(next);
                    else
                        yield return null; // 空队列让出一帧，避免每帧空转
                }
            }
            finally
            {
                // Loop 无论正常跑完、被 Pause 的 StopCoroutine 停止，还是子协程异常导致的意外终止，
                // 退出时都复位状态，保证 Run 的守卫条件自洽、不再出现 _isRunning 残留 true 的假死
                _isRunning = false;
                _loopCoroutine = null;
            }
        }

        /// <summary>
        /// 子协程安全宿主：手动步进子协程（不再经 StartCoroutine 嵌套），
        /// 每次 MoveNext 均由 try/catch 保护——子协程抛异常只终止该项（LogError 后队列继续），
        /// 子协程的挂起指令原样转发给 Unity 调度器，步进节奏与原生嵌套协程一致。
        /// </summary>
        private static IEnumerator SafeHost(IEnumerator routine)
        {
            while (true)
            {
                bool moved;
                object current;
                try
                {
                    moved = routine.MoveNext();
                    current = moved ? routine.Current : null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CoroutineQ] 子协程异常，跳过该项继续队列：{ex}");
                    yield break;
                }

                if (!moved)
                    yield break;

                yield return current;
            }
        }

        private static IEnumerator IEDelay(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }

        private static IEnumerator IEDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        private static IEnumerator IEAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        public void Run()
        {
            if (_loopCoroutine != null && _isRunning)
                return;

            _isRunning = true;
            _loopCoroutine = StartCoroutine(Loop());
        }

        /// <summary>
        /// 暂停；但会保留后续未执行的任务。
        /// </summary>
        public void Pause()
        {
            if (_loopCoroutine != null && _isRunning)
            {
                _isRunning = false;
                StopCoroutine(_loopCoroutine);
            }
        }

        /// <summary>
        /// 暂停，然后清除未执行的任务。
        /// </summary>
        public void StopAndClear()
        {
            Pause();

            _queue.Clear();
            _insertQ.Clear();
        }
    }
}