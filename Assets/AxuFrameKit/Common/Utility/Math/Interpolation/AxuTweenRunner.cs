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
        public static void PlayAll() => Runner.PlayAll();
        public static void KillAll() => Runner.KillAll();

        public static AxuSequence Sequence()
        {
            var sequence = new AxuSequence();
            Runner.AddTween(sequence);
            return sequence;
        }

        public static void Replay(IAxuTweener tween)
        {
            tween.Replay();
            Runner.AddTween(tween);
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

        public void AddTween(IAxuTweener tweener) => _tweenList.Add(tweener);

        public void PauseAll() => _tweenList.ForEach(t => t.Pause());
        public void PlayAll() => _tweenList.ForEach(t => t.Play());
        public void KillAll() => _tweenList.ForEach(t => t.Kill());
    }
}