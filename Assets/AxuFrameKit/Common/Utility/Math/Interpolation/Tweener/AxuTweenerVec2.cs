using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerVec2 : AxuTweener
    {
        private bool _isLocal;
        
        private Transform _transform;
        
        private Vector2 _start, _end;

        public AxuTweenerVec2(float duration, Transform transform, Vector2 start, Vector2 end, bool isLocal) : base(duration)
        {
            _transform = transform;
            _start     = start;
            _end       = end;
            _isLocal   = isLocal;
        }

        protected override void Action()
        {
            var x = _start.x + (_end.x - _start.x) * _t;
            var y = _start.y + (_end.y - _start.y) * _t;

            if (_isLocal)
                _transform.localPosition = new Vector3(x, y);
            else
                _transform.position = new Vector3(x, y);
        }
    }
}