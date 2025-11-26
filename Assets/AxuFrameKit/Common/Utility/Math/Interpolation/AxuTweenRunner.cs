using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation.Tweener;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation
{
    public static class AxuTween
    {
        private static AxuTweenRunner Runner => AxuTweenRunner.Instance;

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

        public static AxuTweener Custom(float start, float end, float duration, Action<float> setter)
        {
            return new AxuTweenerCustomFloat(duration, start, end, setter);
        }
    }

    public class AxuTweenRunner : SingletonMonoCrDs<AxuTweenRunner>
    {
        private List<IAxuTweener> _tweenList = new(16);

        private void Update()
        {
            for (int i = _tweenList.Count - 1; i >= 0; i--)
            {
                IAxuTweener tweener = _tweenList[i];

                tweener.Tik(Time.deltaTime);
                
                if (tweener.IsCompleted)
                    _tweenList.RemoveAt(i);
            }
        }

        public void AddTween(IAxuTweener tweener)
        {
            tweener.Startup();
            _tweenList.Add(tweener);
        }

        public void PauseAll() => _tweenList.ForEach(t => t.Pause());
        public void ResumeAll() => _tweenList.ForEach(t => t.Resume());
        public void KillAll() => _tweenList.ForEach(t => t.Kill());
    }
}