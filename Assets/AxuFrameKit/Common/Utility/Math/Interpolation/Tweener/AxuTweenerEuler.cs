using AxuFrameKit._Core.Utility.Math.Interpolation;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation.Tweener
{
    // AxuTweenEuler 类：直接对欧拉角 (Vector3) 进行插值。
    // 注意：对欧拉角进行 Lerp 可能会导致非最短路径或万向锁问题，
    // 但这是最简单且常见的欧拉角 Tween 实现方式。
    public class AxuTweenerEuler : AxuTweener
    {
        private bool _isLocal;
        
        private Transform _transform;
        
        private Vector3 _start, _end;

        public AxuTweenerEuler(float duration, Transform transform, Vector3 start, Vector3 end, bool isLocal) : base(duration)
        {
            _transform = transform;
            _start     = start;
            _end       = end;
            _isLocal   = isLocal;
        }

        protected override void Action()
        {
            // 直接对 Vector3 的欧拉角进行线性插值
            var x = _start.x + (_end.x - _start.x) * _t;
            var y = _start.y + (_end.y - _start.y) * _t;
            var z = _start.z + (_end.z - _start.z) * _t;

            Vector3 interpolatedEuler = new Vector3(x, y, z);

            if (_isLocal)
                // 将插值后的欧拉角赋值给局部旋转
                _transform.localEulerAngles = interpolatedEuler;
            else
                // 注意：在世界空间下，需要将欧拉角转换为四元数
                _transform.rotation = Quaternion.Euler(interpolatedEuler);
        }
    }
}