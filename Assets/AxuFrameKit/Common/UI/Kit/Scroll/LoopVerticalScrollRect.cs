using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.Scroll
{
    /// <summary>
    /// 高性能循环复用垂直列表 (Infinite/Recycling ScrollView)
    /// 更新版：增加防冲突机制和初始化安全检查
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class LoopVerticalScrollRect : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("列表项的Prefab")]
        public GameObject itemPrefab;

        [Tooltip("列表项的高度 (包含间距)")]
        public float itemHeight = 100f;

        [Tooltip("预加载的缓冲数量 (视口外上下多渲染几个)")]
        public int bufferCount = 2;

        [Tooltip("初始化的数据总数")]
        public int totalCount = 0;

        [Tooltip("元素纵向间距")] 
        public float space = 0f;

        // --- 事件 ---
        public System.Action<Transform, int> onUpdateItem;

        // --- 内部变量 ---
        private ScrollRect scrollRect;
        private RectTransform content;
        private RectTransform viewPort;
    
        private List<LoopItem> activeItems = new List<LoopItem>();
        private Stack<LoopItem> itemPool = new Stack<LoopItem>();
    
        private int visibleCount;
        private int oldStartIndex = -1;
        private bool isInited = false;

        private class LoopItem
        {
            public Transform transform;
            public RectTransform rectTransform;
            public int index; 
            public GameObject gameObject;

            public LoopItem(GameObject obj)
            {
                this.gameObject    = obj;
                this.transform     = obj.transform;
                this.rectTransform = obj.GetComponent<RectTransform>();
                this.index         = -1;
            }
        }

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            content    = scrollRect.content;
            viewPort   = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
        
            // --- 关键修复：自动处理冲突组件 ---
            var layoutGroup = content.GetComponent<LayoutGroup>();
            if (layoutGroup != null && layoutGroup.enabled)
            {
                layoutGroup.enabled = false;
                Debug.LogWarning("[LoopScroll] 检测到 Content 上存在 LayoutGroup，已自动禁用以避免冲突。");
            }

            var sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null && sizeFitter.enabled)
            {
                sizeFitter.enabled = false;
                Debug.LogWarning("[LoopScroll] 检测到 Content 上存在 ContentSizeFitter，已自动禁用以避免冲突。");
            }

            // 强制设置Content的锚点，简化坐标计算
            content.pivot = new Vector2(0.5f, 1f);
            // 这里根据需求，通常垂直列表宽度是自适应的，所以X轴用Stretch(0,1)或者Center(0.5)都可以
            // 这里为了兼容性，我们只锁定Y轴为顶部对齐
            content.anchorMin        = new Vector2(content.anchorMin.x, 1f);
            content.anchorMax        = new Vector2(content.anchorMax.x, 1f);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0); // 重置Y位置

            scrollRect.onValueChanged.AddListener(OnScroll);
        }

        /// <summary>
        /// 初始化或刷新列表
        /// </summary>
        public void RefillCells(int count, bool resetPosition = true)
        {
            // 延迟一帧或者强制刷新Layout，确保viewport高度正确
            // 这里简单的做法是直接计算
            if (viewPort.rect.height == 0)
            {
                // 如果在Start里立刻调用，Canvas可能还没构建完成导致高度为0
                // 强制刷新一下Canvas
                Canvas.ForceUpdateCanvases();
            }

            totalCount = count;
        
            // 1. 手动撑开Content高度 (代替 ContentSizeFitter)
            float totalHeight = totalCount * itemHeight;
            // 保持宽度的设置，只修改高度
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

            // 2. 计算视口需要显示多少个Item
            float viewHeight = viewPort.rect.height;
            if (viewHeight <= 0)
                viewHeight = 1080f; // 兜底保护，防止除以0或负数
        
            visibleCount = Mathf.CeilToInt(viewHeight / itemHeight) + bufferCount;

            // 3. 回收所有
            RecycleAll();

            // 4. 重置位置
            if (resetPosition)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
                oldStartIndex            = -1;
                scrollRect.velocity      = Vector2.zero; // 停止惯性
            }

            isInited = true;
            // 5. 立即刷新
            OnScroll(Vector2.zero);
        }

        private void OnScroll(Vector2 pos)
        {
            if (!isInited || totalCount == 0) return;

            float contentY = content.anchoredPosition.y;

            // 核心公式：根据Content上移的距离，算出当前应该显示第几个Item
            int startIndex = Mathf.FloorToInt(contentY / itemHeight);
        
            // 允许一定的越界（橡皮筋效果时），但在计算数据索引时要限制住
            if (startIndex < -1) startIndex = 0; // 稍微放宽一点范围防止顶部闪烁
        
            int endIndex = startIndex + visibleCount;

            if (startIndex == oldStartIndex) return;
            oldStartIndex = startIndex;

            // 1. 回收越界
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                LoopItem item = activeItems[i];
                // 增加一点容错范围 (buffer)
                if (item.index < startIndex - 1 || item.index >= endIndex + 1) 
                {
                    ReturnToPool(item);
                    activeItems.RemoveAt(i);
                }
            }

            // 2. 填充空缺
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
            
                // 核心公式：手动设置Y坐标
                float yPos = -i * itemHeight; 
            
                // 保持X轴的自适应
                newItem.rectTransform.anchoredPosition = new Vector2(0, yPos + space);
                // 确保没有被缩放
                newItem.rectTransform.localScale = Vector3.one;

                activeItems.Add(newItem);

                if (onUpdateItem != null)
                {
                    onUpdateItem(newItem.transform, i);
                }
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
                GameObject    obj = Instantiate(itemPrefab, content);
                RectTransform rt  = obj.GetComponent<RectTransform>();
            
                // 强制设置Item的锚点，防止LayoutGroup残留设置的影响
                rt.pivot      = new Vector2(0.5f, 1f);
                rt.anchorMin  = new Vector2(0, 1);          // 左上
                rt.anchorMax  = new Vector2(1, 1);          // 右上 (宽度撑满)
                rt.sizeDelta  = new Vector2(0, itemHeight); // 高度固定，宽度由Offset控制
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