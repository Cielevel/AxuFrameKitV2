using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation.Tweener;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Math.Interpolation
{
    public static class AxuTweenExtension
    {
        private static AxuTweenRunner Runner => AxuTweenRunner.Instance;
        
        public static AxuTweener Move(this Transform self, Vector3 targetPos, float duration)
        {
            var tween = new AxuTweenerVec3(duration, self, self.position, targetPos, false);
            Runner.AddTween(tween);
            return tween;
        }
        
        public static AxuTweener LocalMove(this Transform self, Vector3 targetPos, float duration)
        {
            var tween = new AxuTweenerVec3(duration, self, self.localPosition, targetPos, true);
            Runner.AddTween(tween);
            return tween;
        }

        public static AxuTweener RotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerQua(duration, self, self.rotation, targetRot, false);
            Runner.AddTween(tween);
            return tween;
        }

        public static AxuTweener LocalRotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerQua(duration, self, self.localRotation, targetRot, true);
            Runner.AddTween(tween);
            return tween;
        }
        
        public static AxuTweener RotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerEuler(duration, self, self.eulerAngles, targetAngle, false);
            Runner.AddTween(tween);
            return tween;
        }

        public static AxuTweener LocalRotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerEuler(duration, self, self.localEulerAngles, targetAngle, true);
            Runner.AddTween(tween);
            return tween;
        }
    }
}