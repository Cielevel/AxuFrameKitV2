using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// 现代化ListView组件 - 支持虚拟化滚动、对象池、多种布局模式
    /// 特性：
    /// - 虚拟化渲染（只实例化可见项）
    /// - 对象池复用
    /// - 支持垂直、水平、网格布局
    /// - 数据驱动
    /// - 支持动态Item高度/宽度
    /// - 支持选中状态管理
    /// - 完整的生命周期回调
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class UIListView : MonoBehaviour
    {
        #region Enums & Structs

        public enum LayoutMode
        {
            Vertical,       // 垂直列表
            Horizontal,     // 水平列表
            Grid            // 网格布局
        }

        [Serializable]
        public class ListViewSettings
        {
            [Header("布局设置")]
            public LayoutMode layoutMode = LayoutMode.Vertical;

            [Tooltip("默认Item大小（垂直模式为高度，水平模式为宽度）")]
            public float itemSize = 100f;

            [Tooltip("Item间距")]
            public float spacing = 0f;

            [Tooltip("网格模式：每行/列的数量")]
            public int gridConstraintCount = 2;

            [Header("边距设置")]
            [Tooltip("上下左右边距")]
            public int paddingTop = 0;
            public int paddingBottom = 0;
            public int paddingLeft = 0;
            public int paddingRight = 0;

            [Header("性能设置")]
            [Tooltip("缓冲区额外Item数量（用于平滑滚动）")]
            public int bufferCount = 2;

            [Tooltip("是否启用对象池")]
            public bool usePooling = true;

            [Header("Item设置")]
            [Tooltip("Item预制体")]
            public GameObject itemPrefab;

            [Tooltip("Item是否控制宽度/高度填充")]
            public bool controlChildSize = true;
        }

        [Serializable]
        public class SelectionSettings
        {
            [Tooltip("是否启用选中功能")]
            public bool enableSelection = true;

            [Tooltip("是否允许多选")]
            public bool allowMultipleSelection = false;

            [Tooltip("是否允许取消选中")]
            public bool allowDeselect = true;
        }

        #endregion

        #region Fields

        [SerializeField] private ListViewSettings settings = new ListViewSettings();
        [SerializeField] private SelectionSettings selectionSettings = new SelectionSettings();

        // 核心组件
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _viewport;

        // 对象池
        private UIListViewItemPool _itemPool;

        // 数据与Item管理
        private IList<object> _dataSource;
        private List<UIListViewItem> _activeItems = new List<UIListViewItem>();
        private HashSet<UIListViewItem> _selectedItems = new HashSet<UIListViewItem>();

        // 布局缓存
        private int _totalItemCount;
        private int _visibleItemCount;
        private int _currentStartIndex = -1;
        private bool _isInitialized;

        // 布局引擎
        private IListViewLayoutEngine _layoutEngine;

        #endregion

        #region Events

        /// <summary>Item被创建时</summary>
        public event Action<UIListViewItem, int> OnItemCreated;

        /// <summary>Item需要更新数据时</summary>
        public event Action<UIListViewItem, object, int> OnItemUpdate;

        /// <summary>Item被点击时</summary>
        public event Action<UIListViewItem, int> OnItemClicked;

        /// <summary>Item被选中/取消选中时</summary>
        public event Action<UIListViewItem, bool> OnItemSelectionChanged;

        /// <summary>选中项列表变化时</summary>
        public event Action<List<int>> OnSelectionChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);

            _itemPool?.Dispose();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // 获取ScrollRect组件
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : transform as RectTransform;

            // 禁用可能冲突的组件
            DisableConflictingComponents();

            // 设置Content锚点
            SetupContentAnchors();

            // 创建对象池
            if (settings.usePooling)
                _itemPool = new UIListViewItemPool(settings.itemPrefab, _content);

            // 创建布局引擎
            _layoutEngine = CreateLayoutEngine();

            // 注册滚动事件
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            _isInitialized = true;
        }

        private void DisableConflictingComponents()
        {
            var layoutGroup = _content.GetComponent<LayoutGroup>();
            if (layoutGroup != null && layoutGroup.enabled)
                layoutGroup.enabled = false;

            var sizeFitter = _content.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null && sizeFitter.enabled)
                sizeFitter.enabled = false;
        }

        private void SetupContentAnchors()
        {
            switch (settings.layoutMode)
            {
                case LayoutMode.Vertical:
                    _content.pivot = new Vector2(0.5f, 1f);
                    _content.anchorMin = new Vector2(0, 1);
                    _content.anchorMax = new Vector2(1, 1);
                    break;
                case LayoutMode.Horizontal:
                    _content.pivot = new Vector2(0f, 0.5f);
                    _content.anchorMin = new Vector2(0, 0);
                    _content.anchorMax = new Vector2(0, 1);
                    break;
                case LayoutMode.Grid:
                    _content.pivot = new Vector2(0.5f, 1f);
                    _content.anchorMin = new Vector2(0, 1);
                    _content.anchorMax = new Vector2(1, 1);
                    break;
            }
            _content.anchoredPosition = Vector2.zero;
        }

        private IListViewLayoutEngine CreateLayoutEngine()
        {
            switch (settings.layoutMode)
            {
                case LayoutMode.Vertical:
                    return new VerticalLayoutEngine(settings, _content, _viewport);
                case LayoutMode.Horizontal:
                    return new HorizontalLayoutEngine(settings, _content, _viewport);
                case LayoutMode.Grid:
                    return new GridLayoutEngine(settings, _content, _viewport);
                default:
                    return new VerticalLayoutEngine(settings, _content, _viewport);
            }
        }

        #endregion

        #region Public API - Data Binding

        /// <summary>
        /// 绑定数据源并刷新列表
        /// </summary>
        public void SetDataSource<T>(IList<T> dataSource, bool resetPosition = true)
        {
            _dataSource = new List<object>(dataSource.Count);
            foreach (var item in dataSource)
                _dataSource.Add(item);

            RefreshList(resetPosition);
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        public void RefreshList(bool resetPosition = true)
        {
            if (!_isInitialized || _dataSource == null)
                return;

            _totalItemCount = _dataSource.Count;

            // 强制更新Canvas以获取正确的viewport大小
            if (_viewport.rect.height == 0 || _viewport.rect.width == 0)
                Canvas.ForceUpdateCanvases();

            // 计算Content大小
            _layoutEngine.CalculateContentSize(_totalItemCount);

            // 计算可见Item数量
            _visibleItemCount = _layoutEngine.CalculateVisibleItemCount() + settings.bufferCount;

            // 回收所有当前Item
            RecycleAllItems();

            // 重置位置
            if (resetPosition)
            {
                _content.anchoredPosition = Vector2.zero;
                _currentStartIndex = -1;
                _scrollRect.velocity = Vector2.zero;
            }

            // 刷新显示
            RefreshVisibleItems();
        }

        /// <summary>
        /// 追加数据
        /// </summary>
        public void AppendData<T>(IList<T> newData)
        {
            if (_dataSource == null)
            {
                SetDataSource(newData, false);
                return;
            }

            foreach (var item in newData)
                _dataSource.Add(item);

            RefreshList(false);
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        public void Clear()
        {
            _dataSource?.Clear();
            RecycleAllItems();
            _totalItemCount = 0;
            _layoutEngine.CalculateContentSize(0);
        }

        #endregion

        #region Public API - Selection

        /// <summary>
        /// 选中指定索引的Item
        /// </summary>
        public void SelectItem(int index)
        {
            var item = GetActiveItemByIndex(index);
            if (item != null)
                SetItemSelected(item, true);
        }

        /// <summary>
        /// 取消选中指定索引的Item
        /// </summary>
        public void DeselectItem(int index)
        {
            var item = GetActiveItemByIndex(index);
            if (item != null)
                SetItemSelected(item, false);
        }

        /// <summary>
        /// 清空所有选中
        /// </summary>
        public void ClearSelection()
        {
            foreach (var item in new List<UIListViewItem>(_selectedItems))
                SetItemSelected(item, false);
        }

        /// <summary>
        /// 获取选中的索引列表
        /// </summary>
        public List<int> GetSelectedIndices()
        {
            var indices = new List<int>();
            foreach (var item in _selectedItems)
                indices.Add(item.DataIndex);
            return indices;
        }

        #endregion

        #region Public API - Navigation

        /// <summary>
        /// 滚动到指定索引
        /// </summary>
        public void ScrollToItem(int index, bool animated = true, float animationDuration = 0.3f)
        {
            if (index < 0 || index >= _totalItemCount)
                return;

            var position = _layoutEngine.GetItemPosition(index);

            if (animated && Application.isPlaying)
            {
                StartCoroutine(ScrollToPositionCoroutine(position, animationDuration));
            }
            else
            {
                _content.anchoredPosition = position;
            }
        }

        private System.Collections.IEnumerator ScrollToPositionCoroutine(Vector2 targetPosition, float duration)
        {
            Vector2 startPosition = _content.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                // 使用缓动函数
                t = EaseOutCubic(t);
                _content.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            _content.anchoredPosition = targetPosition;
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        #endregion

        #region Scroll Handling

        private void OnScrollValueChanged(Vector2 normalizedPosition)
        {
            RefreshVisibleItems();
        }

        private void RefreshVisibleItems()
        {
            if (!_isInitialized || _totalItemCount == 0 || _dataSource == null)
                return;

            // 计算当前可见范围
            var visibleRange = _layoutEngine.GetVisibleItemRange(_visibleItemCount);
            int startIndex = visibleRange.start;
            int endIndex = visibleRange.end;

            if (startIndex == _currentStartIndex)
                return;

            _currentStartIndex = startIndex;

            // 回收超出范围的Item
            RecycleOutOfRangeItems(startIndex, endIndex);

            // 创建/更新可见范围内的Item
            UpdateVisibleItems(startIndex, endIndex);
        }

        private void RecycleOutOfRangeItems(int startIndex, int endIndex)
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                if (item.DataIndex < startIndex || item.DataIndex >= endIndex)
                {
                    RecycleItem(item);
                    _activeItems.RemoveAt(i);
                }
            }
        }

        private void UpdateVisibleItems(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex && i < _totalItemCount; i++)
            {
                // 检查该索引的Item是否已存在
                if (IsItemActive(i))
                    continue;

                // 获取或创建Item
                var item = GetOrCreateItem();
                item.DataIndex = i;

                // 设置位置和大小
                _layoutEngine.PositionItem(item, i);

                // 更新数据
                UpdateItemData(item, i);

                _activeItems.Add(item);
            }
        }

        private bool IsItemActive(int index)
        {
            foreach (var item in _activeItems)
            {
                if (item.DataIndex == index)
                    return true;
            }
            return false;
        }

        private UIListViewItem GetActiveItemByIndex(int index)
        {
            foreach (var item in _activeItems)
            {
                if (item.DataIndex == index)
                    return item;
            }
            return null;
        }

        #endregion

        #region Item Management

        private UIListViewItem GetOrCreateItem()
        {
            UIListViewItem item;

            if (settings.usePooling && _itemPool != null)
            {
                item = _itemPool.Get();
                
                // 新创建的 Item 需要初始化
                if (item.IsNewlyCreated)
                {
                    item.Initialize(this);
                    OnItemCreated?.Invoke(item, -1);
                }
            }
            else
            {
                var itemGo = Instantiate(settings.itemPrefab, _content);
                item = itemGo.GetComponent<UIListViewItem>();

                if (item == null)
                {
                    Debug.LogError("Item prefab must have UIListViewItem component!");
                    item = itemGo.AddComponent<UIListViewItem>();
                }

                // 初始化Item
                item.Initialize(this);
                OnItemCreated?.Invoke(item, -1);
                item.IsNewlyCreated = true;
            }

            item.gameObject.SetActive(true);
            return item;
        }

        private void RecycleItem(UIListViewItem item)
        {
            item.gameObject.SetActive(false);
            item.DataIndex = -1;

            // 如果Item被选中，取消选中
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
                item.SetSelected(false);
            }

            if (settings.usePooling)
                _itemPool.Return(item);
        }

        private void RecycleAllItems()
        {
            foreach (var item in _activeItems)
                RecycleItem(item);

            _activeItems.Clear();
        }

        private void UpdateItemData(UIListViewItem item, int index)
        {
            var data = _dataSource[index];
            item.UpdateData(data, index);
            OnItemUpdate?.Invoke(item, data, index);
        }

        #endregion

        #region Item Callbacks

        internal void OnItemClickedInternal(UIListViewItem item)
        {
            OnItemClicked?.Invoke(item, item.DataIndex);

            // 处理选中逻辑
            if (selectionSettings.enableSelection)
            {
                bool newState = !item.IsSelected;

                // 单选模式：取消其他选中
                if (!selectionSettings.allowMultipleSelection && newState)
                    ClearSelection();

                // 不允许取消选中
                if (!selectionSettings.allowDeselect && !newState && _selectedItems.Count <= 1)
                    newState = true;

                SetItemSelected(item, newState);
            }
        }

        private void SetItemSelected(UIListViewItem item, bool selected)
        {
            if (item.IsSelected == selected)
                return;

            item.SetSelected(selected);

            if (selected)
                _selectedItems.Add(item);
            else
                _selectedItems.Remove(item);

            OnItemSelectionChanged?.Invoke(item, selected);
            OnSelectionChanged?.Invoke(GetSelectedIndices());
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (settings.itemSize <= 0)
                settings.itemSize = 100f;

            if (settings.gridConstraintCount <= 0)
                settings.gridConstraintCount = 1;
        }
#endif

        #endregion
    }
}
