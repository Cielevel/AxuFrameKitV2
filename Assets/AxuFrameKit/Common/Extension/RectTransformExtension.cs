using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    public static class RectTransformExtensions
    {
        /// <summary>
        /// 设置 Width 和 Height (不受锚点影响，直接设置尺寸)
        /// </summary>
        public static void SetSize(this RectTransform trans, float width, float height)
        {
            trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// 设置 UI 透明度 (CanvasGroup 或 Image/Text 通用并不容易，这里针对 CanvasGroup)
        /// </summary>
        public static void SetAlpha(this CanvasGroup group, float alpha, bool interactable = true)
        {
            group.alpha          = alpha;
            group.interactable   = interactable;
            group.blocksRaycasts = interactable;
        }
    }
}