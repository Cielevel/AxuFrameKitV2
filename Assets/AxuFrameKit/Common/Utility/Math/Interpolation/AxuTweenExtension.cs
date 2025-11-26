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
            var tween = new AxuTweenerMoveVec3(duration, self, targetPos, false);
            return tween;
        }
        
        public static AxuTweener MoveX(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(value, self.position.y, self.position.z), false);
            return tween;
        }
        
        public static AxuTweener MoveY(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(self.position.x, value, self.position.z), false);
            return tween;
        }
        
        public static AxuTweener MoveZ(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(self.position.x, self.position.y, value), false);
            return tween;
        }
        
        public static AxuTweener LocalMove(this Transform self, Vector3 targetPos, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, targetPos, true);
            return tween;
        }
        
        public static AxuTweener LocalMoveX(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(value, self.localPosition.y, self.localPosition.z), false);
            return tween;
        }
        
        public static AxuTweener LocalMoveY(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(self.localPosition.x, value, self.localPosition.z), false);
            return tween;
        }
        
        public static AxuTweener LocalMoveZ(this Transform self, float value, float duration)
        {
            var tween = new AxuTweenerMoveVec3(duration, self, new Vector3(self.localPosition.x, self.localPosition.y, value), false);
            return tween;
        }

        public static AxuTweener LocalScale(this Transform self, Vector3 targetScale, float duration)
        {
            var tween = new AxuTweenerScale(duration, self, targetScale);
            return tween;
        }

        public static AxuTweener RotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerRotQua(duration, self, targetRot, false);
            return tween;
        }

        public static AxuTweener LocalRotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerRotQua(duration, self, targetRot, true);
            return tween;
        }
        
        public static AxuTweener RotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerRotEuler(duration, self, targetAngle, false);
            return tween;
        }

        public static AxuTweener LocalRotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerRotEuler(duration, self, targetAngle, true);
            return tween;
        }

        public static AxuTweener AnchoredMove(this RectTransform self, Vector2 targetPos, float duration)
        {
            var tween = new AxuTweenerAnchoredMove(duration, self, targetPos);
            return tween;
        }
        
        public static AxuTweener AnchoredMoveX(this RectTransform self, float value, float duration)
        {
            var tween = new AxuTweenerAnchoredMove(duration, self, new Vector2(value, self.anchoredPosition.y));
            return tween;
        }
        
        public static AxuTweener AnchoredMoveY(this RectTransform self, float value, float duration)
        {
            var tween = new AxuTweenerAnchoredMove(duration, self, new Vector2(self.anchoredPosition.x, value));
            return tween;
        }

        /// <summary>
        /// 启动Tweener
        /// </summary>
        public static AxuTweener Start(this AxuTweener self)
        {
            Runner.AddTween(self);
            return self;
        }
    }
}