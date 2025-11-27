using UnityEngine;
using UnityEngine.UI;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation.Tweener;

namespace AxuFrameKit.Common.Utility.Math.Interpolation
{
    public static class AxuTweenExtension
    {
        private static IAxuTweenRunner Runner => AxuTweenRunner.Instance;
        
        public static AxuTweener<Transform> ToPos(this Transform self, Vector3 targetPos, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerMoveVec3>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerMoveVec3(duration, self, targetPos, false);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(targetPos);
            }
            return tween;
        }
        
        public static AxuTweener<Transform> ToPosX(this Transform self, float value, float duration)
        {
            return ToPos(self, new Vector3(value, self.position.y, self.position.z), duration);
        }
        
        public static AxuTweener<Transform> ToPosY(this Transform self, float value, float duration)
        {
            return ToPos(self, new Vector3(self.position.x, value, self.position.z), duration);
        }
        
        public static AxuTweener<Transform> ToPosZ(this Transform self, float value, float duration)
        {
            return ToPos(self, new Vector3(self.position.x, self.position.y, value), duration);
        }
        
        public static AxuTweener<Transform> ToLocalPos(this Transform self, Vector3 targetPos, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerMoveVec3>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerMoveVec3(duration, self, targetPos, true);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(targetPos);
            }
            return tween;
        }
        
        public static AxuTweener<Transform> ToLocalPosX(this Transform self, float value, float duration)
        {
            return ToLocalPos(self, new Vector3(value, self.localPosition.y, self.localPosition.z), duration);
        }
        
        public static AxuTweener<Transform> ToLocalPosY(this Transform self, float value, float duration)
        {
            return ToLocalPos(self, new Vector3(self.localPosition.x, value, self.localPosition.z), duration);
        }
        
        public static AxuTweener<Transform> ToLocalPosZ(this Transform self, float value, float duration)
        {
            return ToLocalPos(self, new Vector3(self.localPosition.x, self.localPosition.y, value), duration);
        }

        public static AxuTweener<Transform> ToLocalScale(this Transform self, Vector3 targetScale, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerScale>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerScale(duration, self, targetScale);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(targetScale);
            }
            return tween;
        }

        public static AxuTweener<Transform> RotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerRotQua(duration, self, targetRot, false);
            return tween;
        }

        public static AxuTweener<Transform> LocalRotateQua(this Transform self, Quaternion targetRot, float duration)
        {
            var tween = new AxuTweenerRotQua(duration, self, targetRot, true);
            return tween;
        }
        
        public static AxuTweener<Transform> RotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerRotEuler(duration, self, targetAngle, false);
            return tween;
        }

        public static AxuTweener<Transform> LocalRotateEuler(this Transform self, Vector3 targetAngle, float duration)
        {
            var tween = new AxuTweenerRotEuler(duration, self, targetAngle, true);
            return tween;
        }

        public static AxuTweener<RectTransform> ToAnchoredPos(this RectTransform self, Vector2 targetPos, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerAnchoredMove>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerAnchoredMove(duration, self, targetPos);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(targetPos);
            }
            return tween;
        }
        
        public static AxuTweener<RectTransform> ToAnchoredPosX(this RectTransform self, float value, float duration)
        {
            return ToAnchoredPos(self, new Vector2(value, self.anchoredPosition.y), duration);
        }
        
        public static AxuTweener<RectTransform> ToAnchoredPosY(this RectTransform self, float value, float duration)
        {
            return ToAnchoredPos(self, new Vector2(self.anchoredPosition.x, value), duration);
        }

        public static AxuTweener<Graphic> ToAlpha(this Graphic self, float value, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerGraphicFade>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerGraphicFade(duration, self, value);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(value);
            }
            return tween;
        }
        
        public static AxuTweener<CanvasGroup> ToAlpha(this CanvasGroup self, float value, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerCanvasGroupAlpha>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerCanvasGroupAlpha(duration, self, value);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(value);
            }
            return tween;
        }
        
        public static AxuTweener<Graphic> ToColor(this Graphic self, Color value, float duration)
        {
            var tween = Runner.TryGetTweener<AxuTweenerGraphicColor>();
            if (tween == null || tween.IsCompleted == false)
                tween = new AxuTweenerGraphicColor(duration, self, value);
            else
            {
                tween.SetTarget(self).SetDuration(duration);
                tween.SetEnd(value);
            }
            return tween;
        }

        /// <summary>
        /// 启动Tweener
        /// </summary>
        public static AxuTweener<T> Start<T>(this AxuTweener<T> self)
        {
            Runner.AddTween(self);
            return self;
        }
    }
}