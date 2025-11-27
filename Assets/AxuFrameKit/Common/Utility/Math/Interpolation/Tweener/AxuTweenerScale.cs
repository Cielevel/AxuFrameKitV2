using UnityEngine;
using AxuFrameKit._Core.Utility.Math.Interpolation;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerScale : AxuTweener<Transform>
    {
        private Vector3 _start, _end;

        public AxuTweenerScale(float duration, Transform transform, Vector3 end) : base(duration)
        {
            _target = transform;
            _end       = end;
        }

        protected override void OnStartup() => _start = _target.localScale;

        public void SetEnd(Vector3 end) => _end = end;
        
        protected override void Action()
        {
            var x = _start.x + (_end.x - _start.x) * _t;
            var y = _start.y + (_end.y - _start.y) * _t;
            var z = _start.z + (_end.z - _start.z) * _t;

            _target.localScale = new Vector3(x, y, z);
        }
    }
}