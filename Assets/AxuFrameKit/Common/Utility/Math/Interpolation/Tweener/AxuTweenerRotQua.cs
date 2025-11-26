using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    public class AxuTweenerRotQua : AxuTweener
    {
        private bool _isLocal;
        
        private Transform _transform;
        
        private Quaternion _start, _end;

        public AxuTweenerRotQua(float duration, Transform transform, Quaternion end, bool isLocal) : base(duration)
        {
            _transform = transform;
            _end       = end;
            _isLocal   = isLocal;
        }

        protected override void OnStartup() => _start = _isLocal ? _transform.localRotation : _transform.rotation;

        protected override void Action()
        {
            // 四元数插值必须使用 Slerp (Spherical Linear Interpolation) 
            // 以确保旋转路径最短且速度均匀。
            Quaternion interpolatedRotation = Quaternion.Slerp(_start, _end, _t);

            if (_isLocal)
                _transform.localRotation = interpolatedRotation;
            else
                _transform.rotation = interpolatedRotation;
        }
    }
}