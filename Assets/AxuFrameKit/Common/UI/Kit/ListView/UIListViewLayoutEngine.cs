using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView布局引擎接口
    /// 职责：
    /// - 计算Content大小
    /// - 计算Item位置
    /// - 计算可见Item范围
    /// </summary>
    public interface IListViewLayoutEngine
    {
        /// <summary>计算Content的总大小</summary>
        void CalculateContentSize(int itemCount);

        /// <summary>计算可见Item数量</summary>
        int CalculateVisibleItemCount();

        /// <summary>获取可见Item的范围（起始索引和结束索引）</summary>
        (int start, int end) GetVisibleItemRange(int visibleCount);

        /// <summary>定位Item的位置</summary>
        void PositionItem(UIListViewItem item, int index);

        /// <summary>获取Item的位置（用于滚动到指定Item）</summary>
        Vector2 GetItemPosition(int index);
    }

    #region Base Layout Engine

    /// <summary>
    /// 布局引擎基类
    /// </summary>
    public abstract class BaseLayoutEngine : IListViewLayoutEngine
    {
        protected readonly UIListView.ListViewSettings settings;
        protected readonly RectTransform content;
        protected readonly RectTransform viewport;

        protected BaseLayoutEngine(UIListView.ListViewSettings settings, RectTransform content, RectTransform viewport)
        {
            this.settings = settings;
            this.content = content;
            this.viewport = viewport;
        }

        public abstract void CalculateContentSize(int itemCount);
        public abstract int CalculateVisibleItemCount();
        public abstract (int start, int end) GetVisibleItemRange(int visibleCount);
        public abstract void PositionItem(UIListViewItem item, int index);
        public abstract Vector2 GetItemPosition(int index);

        protected float GetItemSize()
        {
            return settings.itemSize;
        }

        protected float GetSpacing()
        {
            return settings.spacing;
        }
    }

    #endregion

    #region Vertical Layout Engine

    /// <summary>
    /// 垂直布局引擎
    /// </summary>
    public class VerticalLayoutEngine : BaseLayoutEngine
    {
        public VerticalLayoutEngine(UIListView.ListViewSettings settings, RectTransform content, RectTransform viewport)
            : base(settings, content, viewport)
        {
        }

        public override void CalculateContentSize(int itemCount)
        {
            float totalHeight = settings.paddingTop + settings.paddingBottom;
            totalHeight += itemCount * GetItemSize();
            totalHeight += Mathf.Max(0, itemCount - 1) * GetSpacing();

            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
        }

        public override int CalculateVisibleItemCount()
        {
            float viewHeight = viewport.rect.height;
            if (viewHeight <= 0) viewHeight = Screen.height;

            float itemWithSpacing = GetItemSize() + GetSpacing();
            return Mathf.CeilToInt(viewHeight / itemWithSpacing);
        }

        public override (int start, int end) GetVisibleItemRange(int visibleCount)
        {
            float scrollPosition = content.anchoredPosition.y;
            float itemWithSpacing = GetItemSize() + GetSpacing();

            int startIndex = Mathf.FloorToInt((scrollPosition - settings.paddingTop) / itemWithSpacing);
            startIndex = Mathf.Max(0, startIndex);

            int endIndex = startIndex + visibleCount;

            return (startIndex, endIndex);
        }

        public override void PositionItem(UIListViewItem item, int index)
        {
            var rt = item.RectTransform;

            // 计算Y位置
            float yPos = -settings.paddingTop - index * (GetItemSize() + GetSpacing());

            if (settings.controlChildSize)
            {
                // 填充模式 - 使用offsetMin/offsetMax来正确处理拉伸锚点
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1f);

                // 设置高度，宽度通过offset控制
                rt.sizeDelta = new Vector2(0, GetItemSize());
                rt.anchoredPosition = new Vector2(0, yPos);

                // 使用offsetMin和offsetMax来设置左右边距
                rt.offsetMin = new Vector2(settings.paddingLeft, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-settings.paddingRight, rt.offsetMax.y);
            }
            else
            {
                // 固定宽度模式
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1f);
                rt.anchoredPosition = new Vector2(settings.paddingLeft, yPos);

                var sizeDelta = rt.sizeDelta;
                sizeDelta.y = GetItemSize();
                rt.sizeDelta = sizeDelta;
            }

            rt.localScale = Vector3.one;
        }

        public override Vector2 GetItemPosition(int index)
        {
            float yPos = index * (GetItemSize() + GetSpacing());
            return new Vector2(0, yPos);
        }
    }

    #endregion

    #region Horizontal Layout Engine

    /// <summary>
    /// 水平布局引擎
    /// </summary>
    public class HorizontalLayoutEngine : BaseLayoutEngine
    {
        public HorizontalLayoutEngine(UIListView.ListViewSettings settings, RectTransform content, RectTransform viewport)
            : base(settings, content, viewport)
        {
        }

        public override void CalculateContentSize(int itemCount)
        {
            float totalWidth = settings.paddingLeft + settings.paddingRight;
            totalWidth += itemCount * GetItemSize();
            totalWidth += Mathf.Max(0, itemCount - 1) * GetSpacing();

            content.sizeDelta = new Vector2(totalWidth, content.sizeDelta.y);
        }

        public override int CalculateVisibleItemCount()
        {
            float viewWidth = viewport.rect.width;
            if (viewWidth <= 0) viewWidth = Screen.width;

            float itemWithSpacing = GetItemSize() + GetSpacing();
            return Mathf.CeilToInt(viewWidth / itemWithSpacing);
        }

        public override (int start, int end) GetVisibleItemRange(int visibleCount)
        {
            float scrollPosition = -content.anchoredPosition.x;
            float itemWithSpacing = GetItemSize() + GetSpacing();

            int startIndex = Mathf.FloorToInt((scrollPosition - settings.paddingLeft) / itemWithSpacing);
            startIndex = Mathf.Max(0, startIndex);

            int endIndex = startIndex + visibleCount;

            return (startIndex, endIndex);
        }

        public override void PositionItem(UIListViewItem item, int index)
        {
            var rt = item.RectTransform;

            // 计算X位置
            float xPos = settings.paddingLeft + index * (GetItemSize() + GetSpacing());

            if (settings.controlChildSize)
            {
                // 填充模式 - 使用offsetMin/offsetMax来正确处理拉伸锚点
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0f, 0.5f);

                // 设置宽度，高度通过offset控制
                rt.sizeDelta = new Vector2(GetItemSize(), 0);
                rt.anchoredPosition = new Vector2(xPos, 0);

                // 使用offsetMin和offsetMax来设置上下边距
                rt.offsetMin = new Vector2(rt.offsetMin.x, settings.paddingBottom);
                rt.offsetMax = new Vector2(rt.offsetMax.x, -settings.paddingTop);
            }
            else
            {
                // 固定高度模式
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(xPos, 0);

                var sizeDelta = rt.sizeDelta;
                sizeDelta.x = GetItemSize();
                rt.sizeDelta = sizeDelta;
            }

            rt.localScale = Vector3.one;
        }

        public override Vector2 GetItemPosition(int index)
        {
            float xPos = index * (GetItemSize() + GetSpacing());
            return new Vector2(-xPos, 0);
        }
    }

    #endregion

    #region Grid Layout Engine

    /// <summary>
    /// 网格布局引擎
    /// </summary>
    public class GridLayoutEngine : BaseLayoutEngine
    {
        public GridLayoutEngine(UIListView.ListViewSettings settings, RectTransform content, RectTransform viewport)
            : base(settings, content, viewport)
        {
        }

        private int GetColumnCount() => settings.gridConstraintCount;

        private int GetRowCount(int itemCount)
        {
            int columns = GetColumnCount();
            return Mathf.CeilToInt((float)itemCount / columns);
        }

        public override void CalculateContentSize(int itemCount)
        {
            int rowCount = GetRowCount(itemCount);
            float totalHeight = settings.paddingTop + settings.paddingBottom;
            totalHeight += rowCount * GetItemSize();
            totalHeight += Mathf.Max(0, rowCount - 1) * GetSpacing();

            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
        }

        public override int CalculateVisibleItemCount()
        {
            float viewHeight = viewport.rect.height;
            if (viewHeight <= 0) viewHeight = Screen.height;

            float itemWithSpacing = GetItemSize() + GetSpacing();
            int visibleRows = Mathf.CeilToInt(viewHeight / itemWithSpacing);

            return visibleRows * GetColumnCount();
        }

        public override (int start, int end) GetVisibleItemRange(int visibleCount)
        {
            float scrollPosition = content.anchoredPosition.y;
            float itemWithSpacing = GetItemSize() + GetSpacing();
            int columns = GetColumnCount();

            int startRow = Mathf.FloorToInt((scrollPosition - settings.paddingTop) / itemWithSpacing);
            startRow = Mathf.Max(0, startRow);

            int startIndex = startRow * columns;
            int endIndex = startIndex + visibleCount;

            return (startIndex, endIndex);
        }

        public override void PositionItem(UIListViewItem item, int index)
        {
            var rt = item.RectTransform;
            int columns = GetColumnCount();

            int row = index / columns;
            int col = index % columns;

            // 计算位置
            float availableWidth = viewport.rect.width - settings.paddingLeft - settings.paddingRight;
            float itemWidth = (availableWidth - (columns - 1) * GetSpacing()) / columns;

            float xPos = settings.paddingLeft + col * (itemWidth + GetSpacing());
            float yPos = -settings.paddingTop - row * (GetItemSize() + GetSpacing());

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1f);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(itemWidth, GetItemSize());
            rt.localScale = Vector3.one;
        }

        public override Vector2 GetItemPosition(int index)
        {
            int columns = GetColumnCount();
            int row = index / columns;
            float yPos = row * (GetItemSize() + GetSpacing());
            return new Vector2(0, yPos);
        }
    }

    #endregion
}
