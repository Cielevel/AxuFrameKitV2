using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    /// <summary>
    /// Toggle子单元
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIToggleChild : MonoBehaviour
    {
        // 事件：当状态发生真正改变时回调
        public event Action<bool> OnStateChanged;

        [SerializeField] private bool _includeChildUnit;
        
        [SerializeField] private Button _button;
        
        [Tooltip("表现层单元列表")]
        [SerializeField] private List<UIToggleBaseUnit> _unitList;

        // 运行时持有的Parent引用
        private UIToggleParent _parent;
        
        // 唯一标识
        public string ID { get; private set; }
        public int Index { get; private set; }
        
        // 当前状态
        public bool IsOn { get; private set; }

        /// <summary>
        /// 初始化方法，由Parent调用
        /// </summary>
        public void Init(UIToggleParent parent, string id, int index, bool defaultState)
        {
            _parent = parent;
            ID = id;
            Index = index;

            // 移除旧的监听，防止重复
            _button.onClick.RemoveListener(OnClicked);
            _button.onClick.AddListener(OnClicked);

            // 强制设置一次初始状态（不触发动画或回调，或者根据需求触发）
            SetStateInternal(defaultState, true);
        }

        /// <summary>
        /// 仅供 Parent 调用的设置状态方法
        /// </summary>
        /// <param name="isOn">目标状态</param>
        /// <param name="forceUpdate">是否强制刷新（即使状态没变也刷新表现层）</param>
        public void SetState(bool isOn, bool forceUpdate = false)
        {
            if (IsOn == isOn && !forceUpdate) return;
            SetStateInternal(isOn);
        }

        private void SetStateInternal(bool isOn, bool isInit = false)
        {
            IsOn = isOn;

            // 驱动表现层 (颜色、图片等)
            if (_unitList != null)
            {
                foreach (var unit in _unitList)
                {
                    if (unit != null) unit.SetState(IsOn);
                }
            }

            // 只有非初始化阶段才抛出变更事件，避免Start时大量逻辑触发
            if (!isInit)
            {
                OnStateChanged?.Invoke(IsOn);
            }
        }

        private void OnClicked()
        {
            if (_parent != null)
            {
                // 关键改变：不直接修改 State，而是通知 Parent 处理
                _parent.OnChildClick(this);
            }
        }

        private void OnValidate()
        {
            if (_button == null) _button = GetComponent<Button>();
        }
        
        // 供编辑器脚本或Parent自动获取Unit使用
        public void CollectUnits()
        {
            _unitList = _includeChildUnit
                ? new List<UIToggleBaseUnit>(GetComponentsInChildren<UIToggleBaseUnit>())
                : new List<UIToggleBaseUnit>(GetComponents<UIToggleBaseUnit>());
        }
    }
}