using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerMoveVec2 : AxuTweener<Transform>
    {
        private bool _isLocal;
        
        private Vector2 _start, _end;

        public AxuTweenerMoveVec2(float duration, Transform transform, Vector2 end, bool isLocal) : base(duration)
        {
            _target = transform;
            _end       = end;
            _isLocal   = isLocal;
        }

        protected override void OnStartup() => _start = _isLocal ? _target.localPosition : _target.position;
        
        public void SetEnd(Vector2 end) => _end = end;
        
        protected override void Action()
        {
            var x = _start.x + (_end.x - _start.x) * _t;
            var y = _start.y + (_end.y - _start.y) * _t;

            if (_isLocal)
                _target.localPosition = new Vector3(x, y);
            else
                _target.position = new Vector3(x, y);
        }
    }
}