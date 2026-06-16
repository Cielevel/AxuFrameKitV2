using AxuFrameKit.Common.UI.Kit.DataBind;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// 支持自动数据绑定的 ListView Item
    /// 
    /// 使用方式：
    /// 1. 在 Item 预制体上添加 UIBindParent 组件
    /// 2. 在子对象上添加各种 UIBindXXX 组件（如 UIBindTextStr, UIBindImage 等）
    /// 3. 设置 UIBindComponent 的 ID 或 Index
    /// 4. 数据类实现 IUIDataBindable 接口
    /// 
    /// 特点：
    /// - 无需继承和重写 OnUpdateData 方法
    /// - 通过声明式数据绑定自动更新 UI
    /// - 支持 ID 绑定和 Index 绑定两种模式
    /// </summary>
    [RequireComponent(typeof(UIBindParent))]
    public class UIListViewDataBindItem : UIListViewItem
    {
        [Header("数据绑定设置")]
        // [Tooltip("数据绑定模式：ID 绑定（通过字符串ID）或 Index 绑定（通过数字索引）")]
        // [SerializeField] private UIBindParent.BindMode bindMode = UIBindParent.BindMode.ID;

        [Tooltip("是否自动查找 UIBindParent（如果当前物体没有，会在子物体中查找）")]
        [SerializeField] private bool autoFindBindParent = true;

        // 缓存的 UIBindParent 组件
        private UIBindParent _bindParent;
        private bool _isBindParentInitialized;

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();
            InitializeBindParent();
        }

        /// <summary>
        /// 初始化数据绑定组件
        /// </summary>
        private void InitializeBindParent()
        {
            if (_isBindParentInitialized)
                return;

            _bindParent = GetComponent<UIBindParent>();

            if (_bindParent == null && autoFindBindParent)
            {
                _bindParent = GetComponentInChildren<UIBindParent>(true);
            }

            if (_bindParent == null)
            {
                Debug.LogWarning($"[{nameof(UIListViewDataBindItem)}] 未找到 UIBindParent 组件，" +
                    $"请在当前物体或其子物体上添加 UIBindParent，或关闭 Auto Find 并手动绑定。");
            }

            _isBindParentInitialized = true;
        }

        #endregion

        #region Data Binding

        /// <summary>
        /// 重写数据更新方法，使用自动数据绑定
        /// </summary>
        protected override void OnUpdateData(object data, int index)
        {
            InitializeBindParent();

            if (_bindParent == null)
            {
                Debug.LogError($"[{nameof(UIListViewDataBindItem)}] 无法更新数据：UIBindParent 未设置。");
                return;
            }

            // 支持 IUIDataBindable 接口的数据类型
            if (data is IUIDataBindable bindableData)
            {
                ApplyBindableData(bindableData);
            }
            // 支持直接传递 UIDataUnit 数组
            else if (data is IUIDataUnit[] dataUnits)
            {
                ApplyDataUnits(dataUnits);
            }
            // 支持通过扩展方法转换为数据单元的数据类型
            else if (TryConvertToDataUnits(data, out var units))
            {
                ApplyDataUnits(units);
            }
            else
            {
                Debug.LogWarning($"[{nameof(UIListViewDataBindItem)}] 数据类型 {data?.GetType().Name} 不支持自动绑定。" +
                    $"请实现 IUIDataBindable 接口或使用 UIDataUnit 数组。");
            }
        }

        /// <summary>
        /// 应用实现了 IUIDataBindable 接口的数据
        /// </summary>
        private void ApplyBindableData(IUIDataBindable bindableData)
        {
            var units = bindableData.ToDataUnits();
            ApplyDataUnits(units);
        }

        /// <summary>
        /// 应用数据单元数组
        /// </summary>
        private void ApplyDataUnits(IUIDataUnit[] units)
        {
            if (units == null || units.Length == 0)
                return;

            _bindParent.SetData(units);
        }

        /// <summary>
        /// 尝试将数据转换为 UIDataUnit 数组
        /// 子类可以重写此方法以支持自定义类型
        /// </summary>
        protected virtual bool TryConvertToDataUnits(object data, out IUIDataUnit[] units)
        {
            units = null;
            return false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 手动设置数据（用于代码控制）
        /// </summary>
        public void SetBindData(params IUIDataUnit[] units)
        {
            InitializeBindParent();
            ApplyDataUnits(units);
        }

        /// <summary>
        /// 手动设置数据绑定父组件
        /// </summary>
        public void SetBindParent(UIBindParent bindParent)
        {
            _bindParent = bindParent;
            _isBindParentInitialized = true;
        }

        /// <summary>
        /// 获取当前的数据绑定父组件
        /// </summary>
        public UIBindParent GetBindParent() => _bindParent;

        #endregion
    }

    /// <summary>
    /// 数据可绑定接口
    /// 实现此接口的数据类可以自动转换为 UIDataUnit 数组
    /// </summary>
    public interface IUIDataBindable
    {
        /// <summary>
        /// 将数据转换为 UIDataUnit 数组
        /// </summary>
        IUIDataUnit[] ToDataUnits();
    }
}
