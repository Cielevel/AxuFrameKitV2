using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    /// <summary>
    /// 按锚点移动的Tweener，UI专属。
    /// </summary>
    public class AxuTweenerAnchoredMove : AxuTweener<RectTransform>
    {
        private bool _isLocal;
        
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
}