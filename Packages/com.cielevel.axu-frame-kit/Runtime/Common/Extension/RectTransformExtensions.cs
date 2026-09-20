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

        // ==================== 锚点预设 ====================

        /// <summary>
        /// 重置锚点为左上角 (0, 1)
        /// </summary>
        public static void ResetAnchors(this RectTransform trans)
        {
            trans.anchorMin = new Vector2(0f, 1f);
            trans.anchorMax = new Vector2(0f, 1f);
            trans.pivot = new Vector2(0f, 1f);
            trans.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 设置锚点 (同时设置 min, max 和 pivot)
        /// </summary>
        public static void SetAnchors(this RectTransform trans, Vector2 anchor)
        {
            trans.anchorMin = anchor;
            trans.anchorMax = anchor;
            trans.pivot = anchor;
        }

        /// <summary>
        /// 设置锚点 (同时设置 min, max 和 pivot)
        /// </summary>
        public static void SetAnchors(this RectTransform trans, float x, float y)
        {
            trans.SetAnchors(new Vector2(x, y));
        }

        /// <summary>
        /// 设置锚点为左上角 (0, 1)
        /// </summary>
        public static void SetAnchorsTopLeft(this RectTransform trans)
        {
            trans.SetAnchors(0f, 1f);
        }

        /// <summary>
        /// 设置锚点为上中 (0.5, 1)
        /// </summary>
        public static void SetAnchorsTopCenter(this RectTransform trans)
        {
            trans.SetAnchors(0.5f, 1f);
        }

        /// <summary>
        /// 设置锚点为右上角 (1, 1)
        /// </summary>
        public static void SetAnchorsTopRight(this RectTransform trans)
        {
            trans.SetAnchors(1f, 1f);
        }

        /// <summary>
        /// 设置锚点为左中 (0, 0.5)
        /// </summary>
        public static void SetAnchorsMiddleLeft(this RectTransform trans)
        {
            trans.SetAnchors(0f, 0.5f);
        }

        /// <summary>
        /// 设置锚点为中心 (0.5, 0.5)
        /// </summary>
        public static void SetAnchorsCenter(this RectTransform trans)
        {
            trans.SetAnchors(0.5f, 0.5f);
        }

        /// <summary>
        /// 设置锚点为右中 (1, 0.5)
        /// </summary>
        public static void SetAnchorsMiddleRight(this RectTransform trans)
        {
            trans.SetAnchors(1f, 0.5f);
        }

        /// <summary>
        /// 设置锚点为左下角 (0, 0)
        /// </summary>
        public static void SetAnchorsBottomLeft(this RectTransform trans)
        {
            trans.SetAnchors(0f, 0f);
        }

        /// <summary>
        /// 设置锚点为下中 (0.5, 0)
        /// </summary>
        public static void SetAnchorsBottomCenter(this RectTransform trans)
        {
            trans.SetAnchors(0.5f, 0f);
        }

        /// <summary>
        /// 设置锚点为右下角 (1, 0)
        /// </summary>
        public static void SetAnchorsBottomRight(this RectTransform trans)
        {
            trans.SetAnchors(1f, 0f);
        }

        /// <summary>
        /// 设置为拉伸填充 (Stretch All)
        /// </summary>
        public static void SetAnchorsStretch(this RectTransform trans)
        {
            trans.anchorMin = Vector2.zero;
            trans.anchorMax = Vector2.one;
            trans.pivot = new Vector2(0.5f, 0.5f);
            trans.offsetMin = Vector2.zero;
            trans.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置为水平拉伸 (Stretch Horizontal)
        /// </summary>
        public static void SetAnchorsStretchHorizontal(this RectTransform trans, float verticalAnchor = 0.5f)
        {
            trans.anchorMin = new Vector2(0f, verticalAnchor);
            trans.anchorMax = new Vector2(1f, verticalAnchor);
            trans.pivot = new Vector2(0.5f, verticalAnchor);
            trans.offsetMin = new Vector2(0f, trans.offsetMin.y);
            trans.offsetMax = new Vector2(0f, trans.offsetMax.y);
        }

        /// <summary>
        /// 设置为垂直拉伸 (Stretch Vertical)
        /// </summary>
        public static void SetAnchorsStretchVertical(this RectTransform trans, float horizontalAnchor = 0.5f)
        {
            trans.anchorMin = new Vector2(horizontalAnchor, 0f);
            trans.anchorMax = new Vector2(horizontalAnchor, 1f);
            trans.pivot = new Vector2(horizontalAnchor, 0.5f);
            trans.offsetMin = new Vector2(trans.offsetMin.x, 0f);
            trans.offsetMax = new Vector2(trans.offsetMax.x, 0f);
        }

        // ==================== Pivot 设置 ====================

        /// <summary>
        /// 设置中心点
        /// </summary>
        public static void SetPivot(this RectTransform trans, Vector2 pivot)
        {
            trans.pivot = pivot;
        }

        /// <summary>
        /// 设置中心点
        /// </summary>
        public static void SetPivot(this RectTransform trans, float x, float y)
        {
            trans.pivot = new Vector2(x, y);
        }

        /// <summary>
        /// 设置中心点为中心
        /// </summary>
        public static void SetPivotCenter(this RectTransform trans)
        {
            trans.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// 设置中心点为左上角
        /// </summary>
        public static void SetPivotTopLeft(this RectTransform trans)
        {
            trans.pivot = new Vector2(0f, 1f);
        }

        // ==================== 尺寸与位置 ====================

        /// <summary>
        /// 获取宽度
        /// </summary>
        public static float GetWidth(this RectTransform trans)
        {
            return trans.rect.width;
        }

        /// <summary>
        /// 获取高度
        /// </summary>
        public static float GetHeight(this RectTransform trans)
        {
            return trans.rect.height;
        }

        /// <summary>
        /// 设置宽度
        /// </summary>
        public static void SetWidth(this RectTransform trans, float width)
        {
            trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        /// <summary>
        /// 设置高度
        /// </summary>
        public static void SetHeight(this RectTransform trans, float height)
        {
            trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// 设置 anchoredPosition
        /// </summary>
        public static void SetAnchoredPosition(this RectTransform trans, float x, float y)
        {
            trans.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// 设置 anchoredPosition X 分量
        /// </summary>
        public static void SetAnchoredPositionX(this RectTransform trans, float x)
        {
            Vector2 pos = trans.anchoredPosition;
            pos.x = x;
            trans.anchoredPosition = pos;
        }

        /// <summary>
        /// 设置 anchoredPosition Y 分量
        /// </summary>
        public static void SetAnchoredPositionY(this RectTransform trans, float y)
        {
            Vector2 pos = trans.anchoredPosition;
            pos.y = y;
            trans.anchoredPosition = pos;
        }

        /// <summary>
        /// 设置 offsetMin (左下角偏移)
        /// </summary>
        public static void SetOffsetMin(this RectTransform trans, float x, float y)
        {
            trans.offsetMin = new Vector2(x, y);
        }

        /// <summary>
        /// 设置 offsetMax (右上角偏移)
        /// </summary>
        public static void SetOffsetMax(this RectTransform trans, float x, float y)
        {
            trans.offsetMax = new Vector2(x, y);
        }

        // ==================== 世界空间边界 ====================

        /// <summary>
        /// 获取世界空间中的边界矩形
        /// </summary>
        public static Bounds GetWorldBounds(this RectTransform trans)
        {
            Vector3[] corners = new Vector3[4];
            trans.GetWorldCorners(corners);

            Vector3 min = corners[0];
            Vector3 max = corners[0];

            for (int i = 1; i < 4; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        /// <summary>
        /// 获取世界空间中的矩形大小
        /// </summary>
        public static Vector2 GetWorldSize(this RectTransform trans)
        {
            Vector3[] corners = new Vector3[4];
            trans.GetWorldCorners(corners);

            float width = Mathf.Abs(corners[2].x - corners[0].x);
            float height = Mathf.Abs(corners[2].y - corners[0].y);

            return new Vector2(width, height);
        }

        // ==================== 可见性检测 ====================

        /// <summary>
        /// 检测 RectTransform 是否在父级视口内可见
        /// </summary>
        /// <param name="trans">要检测的 RectTransform</param>
        /// <param name="viewport">视口 RectTransform，如果为 null 则使用父级</param>
        /// <returns>是否在视口内</returns>
        public static bool IsVisibleInParent(this RectTransform trans, RectTransform viewport = null)
        {
            if (viewport == null)
            {
                viewport = trans.parent as RectTransform;
                if (viewport == null) return true;
            }

            Vector3[] myCorners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];

            trans.GetWorldCorners(myCorners);
            viewport.GetWorldCorners(parentCorners);

            // 检查是否有重叠
            float myMinX = Mathf.Min(myCorners[0].x, myCorners[2].x);
            float myMaxX = Mathf.Max(myCorners[0].x, myCorners[2].x);
            float myMinY = Mathf.Min(myCorners[0].y, myCorners[2].y);
            float myMaxY = Mathf.Max(myCorners[0].y, myCorners[2].y);

            float parentMinX = Mathf.Min(parentCorners[0].x, parentCorners[2].x);
            float parentMaxX = Mathf.Max(parentCorners[0].x, parentCorners[2].x);
            float parentMinY = Mathf.Min(parentCorners[0].y, parentCorners[2].y);
            float parentMaxY = Mathf.Max(parentCorners[0].y, parentCorners[2].y);

            return myMaxX > parentMinX && myMinX < parentMaxX &&
                   myMaxY > parentMinY && myMinY < parentMaxY;
        }

        /// <summary>
        /// 检测 RectTransform 是否完全在父级视口内
        /// </summary>
        public static bool IsFullyVisibleInParent(this RectTransform trans, RectTransform viewport = null)
        {
            if (viewport == null)
            {
                viewport = trans.parent as RectTransform;
                if (viewport == null) return true;
            }

            Vector3[] myCorners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];

            trans.GetWorldCorners(myCorners);
            viewport.GetWorldCorners(parentCorners);

            float myMinX = Mathf.Min(myCorners[0].x, myCorners[2].x);
            float myMaxX = Mathf.Max(myCorners[0].x, myCorners[2].x);
            float myMinY = Mathf.Min(myCorners[0].y, myCorners[2].y);
            float myMaxY = Mathf.Max(myCorners[0].y, myCorners[2].y);

            float parentMinX = Mathf.Min(parentCorners[0].x, parentCorners[2].x);
            float parentMaxX = Mathf.Max(parentCorners[0].x, parentCorners[2].x);
            float parentMinY = Mathf.Min(parentCorners[0].y, parentCorners[2].y);
            float parentMaxY = Mathf.Max(parentCorners[0].y, parentCorners[2].y);

            return myMinX >= parentMinX && myMaxX <= parentMaxX &&
                   myMinY >= parentMinY && myMaxY <= parentMaxY;
        }

        // ==================== 坐标转换 ====================

        /// <summary>
        /// 将屏幕坐标转换为 RectTransform 的局部坐标
        /// </summary>
        public static bool ScreenPointToLocalPoint(this RectTransform trans, Vector2 screenPoint, Camera camera, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(trans, screenPoint, camera, out localPoint);
        }

        /// <summary>
        /// 将世界坐标转换为 RectTransform 的局部坐标
        /// </summary>
        public static Vector2 WorldToLocalPoint(this RectTransform trans, Vector3 worldPoint, Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trans, screenPoint, camera, out Vector2 localPoint);
            return localPoint;
        }
    }
}
