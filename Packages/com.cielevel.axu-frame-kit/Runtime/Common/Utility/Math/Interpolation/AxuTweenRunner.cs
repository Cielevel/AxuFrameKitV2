using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation.Tweener;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation
{
    public static class AxuTween
    {
        private static IAxuTweenRunner Runner => AxuTweenRunner.Instance;

        public static void PauseAll() => Runner.PauseAll();
        public static void PlayAll() => Runner.ResumeAll();
        public static void KillAll() => Runner.KillAll();

        public static AxuSequence Sequence()
        {
            var sequence = new AxuSequence();
            return sequence;
        }

        public static void Replay(IAxuTweener tween)
        {
            tween.Replay();
            Runner.AddTween(tween); // TODO Replay存在隐患：对于在运行中的Tweener，进行Replay，会重复添加到Runner中。
        }

        public static AxuTweener<object> Custom(float start, float end, float duration, Action<float> setter)
        {
            return new AxuTweenerCustomFloat(duration, start, end, setter);
        }
    }

    public interface IAxuTweenRunner
    {
        void AddTween(IAxuTweener tweener);

        T TryGetTweener<T>() where T : class, IAxuTweener;
        
        void PauseAll();
        void ResumeAll();
        void KillAll();
    }

    public class AxuTweenRunner : SingletonMonoCrDs<AxuTweenRunner>, IAxuTweenRunner
    {
        private const int DefaultCapacity = 16;
        
        private Dictionary<Type, Stack<IAxuTweener>> _pools = new(DefaultCapacity);
        
        private List<IAxuTweener> _tweenList = new(16);
        
        private void Update()
        {
            for (int i = _tweenList.Count - 1; i >= 0; i--)
            {
                IAxuTweener tweener = _tweenList[i];

                tweener.Tik(Time.deltaTime);

                if (tweener.IsCompleted)
                {
                    Recycle(_tweenList[i]);
                    
                    _tweenList.RemoveAt(i);
                }
            }
        }

        void IAxuTweenRunner.AddTween(IAxuTweener tweener)
        {
            tweener.Startup();
            _tweenList.Add(tweener);
        }

        T IAxuTweenRunner.TryGetTweener<T>()
        {
            var type = typeof(T);

            if (_pools.ContainsKey(type) && 0 < _pools[type].Count)
            {
                var result = _pools[type].Pop();
                result.Reset();
                return result as T;
            }

            return null;
        }

        private void Recycle(IAxuTweener tweener)
        {
            var type = tweener.GetType();
            
            tweener.Reset();

            if (_pools.ContainsKey(type) == false)
                _pools.Add(type, new Stack<IAxuTweener>(DefaultCapacity));

            _pools[type].Push(tweener);
        }

        public void PauseAll() => _tweenList.ForEach(t => t.Pause());
        public void ResumeAll() => _tweenList.ForEach(t => t.Resume());
        public void KillAll() => _tweenList.ForEach(t => t.Kill());
    }
}