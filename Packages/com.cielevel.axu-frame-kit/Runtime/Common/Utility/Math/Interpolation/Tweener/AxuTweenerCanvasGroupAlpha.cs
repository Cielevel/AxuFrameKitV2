using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerCanvasGroupAlpha : AxuTweener<CanvasGroup>
    {
        private float _start, _end;
        
        public AxuTweenerCanvasGroupAlpha(float duration, CanvasGroup target, float endValue) : base(duration)
        {
            _target = target;
            _end    = endValue;
        }

        public void SetEnd(float endValue) => _end = endValue;
    
        protected override void OnStartup()
        {
            if (_target != null)
                _start = _target.alpha;
        }

        protected override void Action()
        {
            if (_target == null) return;

            float currentAlpha = _start + (_end - _start) * _t;

            _target.alpha = currentAlpha;
        }
    }
}