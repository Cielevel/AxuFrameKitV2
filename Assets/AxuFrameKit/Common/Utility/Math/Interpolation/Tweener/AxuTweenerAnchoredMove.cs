using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    /// <summary>
    /// 按锚点移动的Tweener，UI专属。
    /// </summary>
    public class AxuTweenerAnchoredMove : AxuTweener<RectTransform>
    {
        private Vector2 _start, _end;
        
        public AxuTweenerAnchoredMove(float duration, RectTransform transform, Vector2 targetPos) : base(duration)
        {
            _target = transform;

            _end = targetPos;
        }

        protected override void OnStartup() => _start = _target.anchoredPosition;
        
        public void SetEnd(Vector2 end) => _end = end;
        
        protected override void Action()
        {
            var x = _start.x + (_end.x - _start.x) * _t;
            var y = _start.y + (_end.y - _start.y) * _t;

            _target.anchoredPosition = new Vector2(x, y);
        }
    }
    
    public class AxuTweenerAnchoredMoveX : AxuTweener<RectTransform>
    {
        private float _start, _end;
        
        public AxuTweenerAnchoredMoveX(float duration, RectTransform transform, float value) : base(duration)
        {
            _target = transform;

            _end = value;
        }

        protected override void OnStartup() => _start = _target.anchoredPosition.x;
        
        public void SetEnd(float end) => _end = end;
        
        protected override void Action()
        {
            var x = _start + (_end - _start) * _t;
            var y = _target.anchoredPosition.y;

            _target.anchoredPosition = new Vector2(x, y);
        }
    }
    
    public class AxuTweenerAnchoredMoveY : AxuTweener<RectTransform>
    {
        private float _start, _end;
        
        public AxuTweenerAnchoredMoveY(float duration, RectTransform transform, float value) : base(duration)
        {
            _target = transform;

            _end = value;
        }

        protected override void OnStartup() => _start = _target.anchoredPosition.y;
        
        public void SetEnd(float end) => _end = end;
        
        protected override void Action()
        {
            var x = _target.anchoredPosition.x;
            var y = _start + (_end - _start) * _t;

            _target.anchoredPosition = new Vector2(x, y);
        }
    }
}