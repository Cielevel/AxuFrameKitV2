using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.Scroll
{
    [RequireComponent(typeof(ScrollRect))]
    public class UILoopVerticalScrollRect : MonoBehaviour
    {
        [Header("配置")]
        public GameObject itemPrefab;
        public float itemHeight = 100f;
        public int bufferCount = 2;
        public int totalCount = 0;
        public float space = 0f;

        [Header("Padding (匹配VerticalLayoutGroup)")]
        public int paddingTop = 0;
        public int paddingBottom = 0;
        public int paddingLeft = 0;
        public int paddingRight = 0;

        [Header("控制子宽度(ControlChildWidth)")]
        public bool controlChildWidth = true;

        public System.Action<Transform, int> onUpdateItem;

        private ScrollRect scrollRect;
        private RectTransform content;
        private RectTransform viewPort;
        private List<LoopItem> activeItems = new List<LoopItem>();
        private Stack<LoopItem> itemPool = new Stack<LoopItem>();
        private int visibleCount;
        private int oldStartIndex = -1;
        private bool isInited = false;

        private float itemOriginalWidth; // 用于关闭controlChildWidth时恢复宽度

        private class LoopItem
        {
            public Transform transform;
            public RectTransform rectTransform;
            public int index;
            public GameObject gameObject;
            public LoopItem(GameObject obj)
            {
                this.gameObject = obj;
                this.transform = obj.transform;
                this.rectTransform = obj.GetComponent<RectTransform>();
                this.index = -1;
            }
        }

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            content    = scrollRect.content;
            viewPort   = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;

            // 禁用冲突组件
            var layoutGroup = content.GetComponent<LayoutGroup>();
            if (layoutGroup != null && layoutGroup.enabled) layoutGroup.enabled = false;
            var sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null && sizeFitter.enabled) sizeFitter.enabled = false;

            // Content锚点归顶
            content.pivot = new Vector2(0.5f, 1f);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.anchoredPosition = new Vector2(0, 0);

            // 原始宽度
            var prefabRT = itemPrefab.GetComponent<RectTransform>();
            itemOriginalWidth = prefabRT != null ? prefabRT.sizeDelta.x : 100; //兜底
            scrollRect.onValueChanged.AddListener(OnScroll);
        }

        /// <summary>刷新列表</summary>
        public void RefillCells(int count, bool resetPosition = true)
        {
            if (viewPort.rect.height == 0) Canvas.ForceUpdateCanvases();

            totalCount = count;
            float totalHeight = paddingTop + paddingBottom + totalCount * itemHeight + Mathf.Max(0, totalCount - 1) * space;
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

            float viewHeight = viewPort.rect.height;
            if (viewHeight <= 0) viewHeight = 1080f;
            visibleCount = Mathf.CeilToInt(viewHeight / (itemHeight + space)) + bufferCount;
            RecycleAll();

            if (resetPosition)
            {
                content.anchoredPosition = new Vector2(0, 0);
                oldStartIndex = -1;
                scrollRect.velocity = Vector2.zero;
            }

            isInited = true;
            OnScroll(Vector2.zero);
        }

        // ...
        private void OnScroll(Vector2 pos)
        {
            if (!isInited || totalCount == 0) return;
            float contentY                 = content.anchoredPosition.y;
            int   startIndex               = Mathf.FloorToInt((contentY - paddingTop) / (itemHeight + space));
            if (startIndex < 0) startIndex = 0;
            int endIndex                   = startIndex + visibleCount;
            if (startIndex == oldStartIndex) return;
            oldStartIndex = startIndex;

            // 回收越界
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                LoopItem item = activeItems[i];
                if (item.index < startIndex - 1 || item.index >= endIndex + 1)
                {
                    ReturnToPool(item);
                    activeItems.RemoveAt(i);
                }
            }

            // 填充新项
            for (int i = startIndex; i < endIndex; i++)
            {
                if (i < 0 || i >= totalCount) continue;
                bool isShowing = false;
                foreach (var item in activeItems)
                {
                    if (item.index == i)
                    {
                        isShowing = true;
                        break;
                    }
                }

                if (isShowing) continue;
                LoopItem newItem = GetFromPool();
                newItem.index = i;

                float yPos = -paddingTop - i * (itemHeight + space);

                if (controlChildWidth)
                {
                    newItem.rectTransform.anchorMin = new Vector2(0, 1);
                    newItem.rectTransform.anchorMax = new Vector2(0, 1);
                    newItem.rectTransform.pivot     = new Vector2(0, 1);
                    float targetWidth = content.rect.width - paddingLeft - paddingRight;
                    newItem.rectTransform.sizeDelta        = new Vector2(targetWidth, itemHeight);
                    newItem.rectTransform.anchoredPosition = new Vector2(paddingLeft, yPos);
                }
                else
                {
                    newItem.rectTransform.anchorMin        = new Vector2(0, 1);
                    newItem.rectTransform.anchorMax        = new Vector2(0, 1);
                    newItem.rectTransform.pivot            = new Vector2(0, 1);
                    newItem.rectTransform.sizeDelta        = new Vector2(itemOriginalWidth, itemHeight);
                    newItem.rectTransform.anchoredPosition = new Vector2(paddingLeft, yPos);
                }

                newItem.rectTransform.localScale = Vector3.one;
                activeItems.Add(newItem);
                if (onUpdateItem != null) onUpdateItem(newItem.transform, i);
            }
        }

        private LoopItem GetFromPool()
        {
            LoopItem item;
            if (itemPool.Count > 0)
            {
                item = itemPool.Pop();
                item.gameObject.SetActive(true);
            }
            else
            {
                GameObject obj = Instantiate(itemPrefab, content);
                RectTransform rt = obj.GetComponent<RectTransform>();
                if (controlChildWidth)
                {
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0, itemHeight);
                }
                else
                {
                    rt.anchorMin = new Vector2(0.5f, 1);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(itemOriginalWidth, itemHeight);
                }
                rt.localScale = Vector3.one;
                item = new LoopItem(obj);
            }
            return item;
        }

        private void ReturnToPool(LoopItem item)
        {
            item.gameObject.SetActive(false);
            item.index = -1;
            itemPool.Push(item);
        }

        private void RecycleAll()
        {
            foreach (var item in activeItems)
            {
                ReturnToPool(item);
            }
            activeItems.Clear();
        }
    }
}