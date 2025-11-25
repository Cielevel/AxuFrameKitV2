using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerVec3 : AxuTweener
    {
        private bool _isLocal;
        
        private Transform _transform;
        
        private Vector3 _start, _end;

        public AxuTweenerVec3(float duration, Transform transform, Vector3 start, Vector3 end, bool isLocal) : base(duration)
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
            var z = _start.z + (_end.z - _start.z) * _t;

            if (_isLocal)
                _transform.localPosition = new Vector3(x, y, z);
            else
                _transform.position = new Vector3(x, y, z);
        }
    }
}