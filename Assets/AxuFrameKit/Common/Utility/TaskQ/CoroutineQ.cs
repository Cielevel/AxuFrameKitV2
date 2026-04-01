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
            while (_isRunning)
            {
                while (0 < _insertQ.Count && _insertQ.TryDequeue(out var insertNext))
                    yield return StartCoroutine(insertNext);

                if (0 < _queue.Count && _queue.TryDequeue(out var next))
                    yield return StartCoroutine(next);
                else
                    yield return null;
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