using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerGraphicFade : AxuTweener<Graphic>
    {
        private float _start, _end;
        
        public AxuTweenerGraphicFade(float duration, Graphic target, float endValue) : base(duration)
        {
            _target = target;
            _end    = endValue;
        }

        public void SetEnd(float endValue) => _end = endValue;
    
        protected override void OnStartup()
        {
            if (_target != null)
                _start = _target.color.a;
        }

        protected override void Action()
        {
            if (_target == null) return;

            var currentAlpha = _start + (_end - _start) * _t;
            var color = _target.color;
            
            _target.color = new Color(color.r, color.g, color.b, currentAlpha);
        }
    }
}