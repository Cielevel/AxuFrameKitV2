using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerRotQua : AxuTweener<Transform>
    {
        private bool _isLocal;
        
        private Quaternion _start, _end;

        public AxuTweenerRotQua(float duration, Transform transform, Quaternion end, bool isLocal) : base(duration)
        {
            _target = transform;
            _end       = end;
            _isLocal   = isLocal;
        }

        protected override void OnStartup() => _start = _isLocal ? _target.localRotation : _target.rotation;

        public void SetEnd(Quaternion end) => _end = end;
        
        protected override void Action()
        {
            // 四元数插值必须使用 Slerp (Spherical Linear Interpolation) 
            // 以确保旋转路径最短且速度均匀。
            Quaternion interpolatedRotation = Quaternion.Slerp(_start, _end, _t);

            if (_isLocal)
                _target.localRotation = interpolatedRotation;
            else
                _target.rotation = interpolatedRotation;
        }
    }
}