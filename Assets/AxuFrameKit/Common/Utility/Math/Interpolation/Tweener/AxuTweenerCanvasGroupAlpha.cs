using System;
using AxuFrameKit._Core.Utility.Math.Interpolation;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    /// <summary>
    /// 自定义Float曲线
    /// </summary>
    public class AxuTweenerCustomFloat : AxuTweener<object>
    {
        private float _start, _end;
        
        private Action<float> _setter;
        
        public AxuTweenerCustomFloat(float duration, float start, float end, Action<float> setter) : base(duration)
        {
            Duration = duration;

            _start = start;
            _end   = end;

            _setter = setter;
        }

        protected override void OnStartup() { }
        
        public void SetEnd(float end) => _end = end;
        
        protected override void Action() => _setter?.Invoke(_start + (_end - _start) * _t);
    }
}