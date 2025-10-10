using System;
using System.Collections;
using System.Collections.Concurrent;
using AxuFrameKit.Common.Utility.Multiton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.TaskX
{
    public interface ICoroutineQ
    {
        ICoroutineQ EnQ(IEnumerator coroutine);

        ICoroutineQ EnQ(Action action);

        ICoroutineQ Then(IEnumerator coroutine);

        ICoroutineQ Delay(float duration, Action action);
        ICoroutineQ Delay(float duration);
        
        void Run();

        void Stop();
    }

    public class CoroutineQ : MultitonMono<CoroutineQ>, ICoroutineQ
    {
        private bool _isRunning = false;
        
        private Coroutine _coroutine;

        private ConcurrentQueue<IEnumerator> _queue;

        #region Entry

        public ICoroutineQ EnQ(IEnumerator coroutine)
        {
            MakeSure();
            _queue.Enqueue(coroutine);
            return this;
        }

        public ICoroutineQ EnQ(Action action)
        {
            MakeSure();
            _queue.Enqueue(IEAction(action));
            return this;
        }
        
        public ICoroutineQ Delay(float duration, Action action)
        {
            MakeSure();
            EnQ(IEDelay(duration, action));
            return this;
        }
        
        public ICoroutineQ Delay(float duration)
        {
            MakeSure();
            EnQ(IEDelay(duration));
            return this;
        }

            #endregion

        ICoroutineQ ICoroutineQ.Then(IEnumerator coroutine)
        {
            MakeSure();
            _queue.Enqueue(coroutine);
            return this;
        }
        
        ICoroutineQ ICoroutineQ.Delay(float duration, Action action)
        {
            MakeSure();
            EnQ(IEDelay(duration, action));
            return this;
        }
        
        ICoroutineQ ICoroutineQ.Delay(float duration)
        {
            MakeSure();
            EnQ(IEDelay(duration));
            return this;
        }

        private IEnumerator Loop()
        {
            while (_isRunning)
            {
                if (_queue.TryDequeue(out var next))
                    yield return StartCoroutine(next);
            }
        }

        private IEnumerator IEDelay(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }
        
        private IEnumerator IEDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator IEAction(Action action)
        {
            action?.Invoke();
            yield return null;
        }

        public void Run()
        {
            if (_coroutine != null && _isRunning)
                return;
            
            _isRunning = true;
            _coroutine = StartCoroutine(Loop());
        }

        public void Stop()
        {
            if (_coroutine != null && _isRunning)
            {
                _isRunning = false;
                StopCoroutine(_coroutine);
            }
        }

        private void MakeSure()
        {
            _queue ??= new ConcurrentQueue<IEnumerator>();
        }
    }
}