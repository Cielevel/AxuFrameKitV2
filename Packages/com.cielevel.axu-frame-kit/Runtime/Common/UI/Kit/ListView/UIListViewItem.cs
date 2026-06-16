using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView Item基类
    /// 负责：
    /// - Item的生命周期管理
    /// - 点击事件处理
    /// - 选中状态管理
    /// - 数据绑定抽象
    /// 
    /// 使用方式：
    /// 继承此类并实现 OnUpdateData 方法来自定义数据绑定逻辑
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIListViewItem : MonoBehaviour, IPointerClickHandler
    {
        #region Fields

        [Header("选中视觉反馈（可选）")]
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f, 1f);

        // 数据相关
        private UIListView _parentListView;
        private object _data;
        private int _dataIndex = -1;
        private bool _isSelected;
        
        // 组件缓存
        private RectTransform _rectTransform;
        
        // 标记是否为新创建的 Item（用于生命周期管理）
        public bool IsNewlyCreated { get; internal set; }

        #endregion

        #region Properties

        /// <summary>当前Item的数据索引</summary>
        public int DataIndex
        {
            get => _dataIndex;
            internal set => _dataIndex = value;
        }

        /// <summary>当前Item绑定的数据</summary>
        public object Data => _data;

        /// <summary>是否被选中</summary>
        public bool IsSelected => _isSelected;

        /// <summary>RectTransform组件</summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 由ListView调用，初始化Item
        /// </summary>
        internal void Initialize(UIListView parentListView)
        {
            _parentListView = parentListView;
            OnInitialize();
        }

        /// <summary>
        /// 子类可重写此方法进行自定义初始化
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        #endregion

        #region Data Binding

        /// <summary>
        /// 更新Item数据（由ListView调用）
        /// </summary>
        internal void UpdateData(object data, int index)
        {
            _data = data;
            _dataIndex = index;
            OnUpdateData(data, index);
        }

        /// <summary>
        /// 子类必须实现此方法来处理数据更新
        /// </summary>
        /// <param name="data">绑定的数据对象</param>
        /// <param name="index">当前索引</param>
        protected virtual void OnUpdateData(object data, int index)
        {
            // 子类实现具体的数据绑定逻辑
            // 例如：
            // var myData = data as MyDataClass;
            // nameText.text = myData.Name;
            // iconImage.sprite = myData.Icon;
        }

        #endregion

        #region Selection

        /// <summary>
        /// 设置选中状态（由ListView调用）
        /// </summary>
        internal void SetSelected(bool selected)
        {
            if (_isSelected == selected)
                return;

            _isSelected = selected;
            UpdateVisualState();
            OnSelectionChanged(selected);
        }

        /// <summary>
        /// 更新选中状态的视觉反馈
        /// </summary>
        private void UpdateVisualState()
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(_isSelected);

            if (backgroundImage != null)
                backgroundImage.color = _isSelected ? selectedColor : normalColor;
        }

        /// <summary>
        /// 选中状态变化时的回调（子类可重写）
        /// </summary>
        protected virtual void OnSelectionChanged(bool isSelected)
        {
        }

        #endregion

        #region Click Handling

        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnItemClicked();
            _parentListView?.OnItemClickedInternal(this);
        }

        /// <summary>
        /// Item被点击时的回调（子类可重写）
        /// </summary>
        protected virtual void OnItemClicked()
        {
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Item从对象池取出或被创建时
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// Item回收到对象池时
        /// </summary>
        protected virtual void OnHide()
        {
        }

        /// <summary>
        /// 内部调用 - Item显示（由对象池管理）
        /// </summary>
        public void OnShowInternal()
        {
            OnShow();
        }

        /// <summary>
        /// 内部调用 - Item隐藏（由对象池管理）
        /// </summary>
        public void OnHideInternal()
        {
            // 隐藏时重置选中状态
            if (_isSelected)
            {
                _isSelected = false;
                UpdateVisualState();
            }
            OnHide();
        }

        private void OnEnable()
        {
            // 不再直接使用，由对象池管理生命周期
        }

        private void OnDisable()
        {
            // 不再直接使用，由对象池管理生命周期
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 获取强类型数据
        /// </summary>
        protected T GetData<T>() where T : class
        {
            return _data as T;
        }

        /// <summary>
        /// 安全获取强类型数据
        /// </summary>
        protected bool TryGetData<T>(out T result) where T : class
        {
            result = _data as T;
            return result != null;
        }

        #endregion
    }
}
