using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind
{
    using Component;

    public class UIBindParent : MonoBehaviour
    {
        /// <summary>
        /// 数据绑定模式
        /// </summary>
        public enum BindMode
        {
            /// <summary>通过字符串 ID 绑定组件</summary>
            ID,
            /// <summary>通过数字索引绑定组件</summary>
            Index
        }

        private enum BindType { ID, Index }

        [SerializeField] private BindType _bindType;

        [SerializeField] private List<UIBindComponent> _uiBindComponentList;

        private Dictionary<int, UIBindComponent> _indexDict;
        private Dictionary<string, UIBindComponent> _idDict;
        private bool _isInitialized;

        /// <summary>
        /// 当前绑定模式
        /// </summary>
        public BindMode CurrentBindMode => _bindType == BindType.ID ? BindMode.ID : BindMode.Index;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化字典
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;

            if (_uiBindComponentList == null || _uiBindComponentList.Count == 0)
            {
                // 尝试自动收集
                AutoCollectComponents();
            }

            if (_uiBindComponentList != null)
            {
                switch (_bindType)
                {
                    case BindType.ID:
                        // 循环构建 + 重复检测：重复 ID 记录错误并跳过后收集的组件（ToDictionary 遇重复会直接抛异常导致初始化永远失败）
                        _idDict = new Dictionary<string, UIBindComponent>(_uiBindComponentList.Count);
                        foreach (var component in _uiBindComponentList)
                        {
                            if (component == null || string.IsNullOrEmpty(component.ID))
                                continue;

                            if (_idDict.ContainsKey(component.ID))
                            {
                                Debug.LogError($"[UIBindParent] ID 重复：'{component.ID}'（组件 {component.name}），已跳过该组件，保留先收集的那个。", this);
                                continue;
                            }

                            _idDict.Add(component.ID, component);
                        }
                        break;
                    case BindType.Index:
                        // 自动收集的组件 Index 字段默认全为 0，两个以上组件直接建字典必重复，先尝试自动编号
                        AutoNumberIndexIfNeeded();

                        // 循环构建 + 重复检测：重复 Index 记录错误并跳过后收集的组件
                        _indexDict = new Dictionary<int, UIBindComponent>(_uiBindComponentList.Count);
                        foreach (var component in _uiBindComponentList)
                        {
                            if (component == null)
                                continue;

                            if (_indexDict.ContainsKey(component.Index))
                            {
                                Debug.LogError($"[UIBindParent] Index 重复：{component.Index}（组件 {component.name}），已跳过该组件，保留先收集的那个。", this);
                                continue;
                            }

                            _indexDict.Add(component.Index, component);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Index 模式下自动收集的组件 Index 默认全为 0，无法区分；
        /// 此时按收集顺序自动编号 0..n-1（与编辑器 FillIdOrIndex 按钮的编号约定一致）。
        /// 只要有任意组件的 Index 非默认值 0，就视为用户已手动配置，不做自动编号。
        /// </summary>
        private void AutoNumberIndexIfNeeded()
        {
            if (_uiBindComponentList.Count <= 1)
                return;

            bool allDefault = true;
            foreach (var component in _uiBindComponentList)
            {
                if (component == null)
                    continue;

                if (component.Index != 0)
                {
                    allDefault = false;
                    break;
                }
            }

            if (!allDefault)
                return;

            int nextIndex = 0;
            foreach (var component in _uiBindComponentList)
            {
                if (component == null)
                    continue;

                ((IUIBindComponent)component).SetIndex(nextIndex++);
            }
        }

        /// <summary>
        /// 自动收集子物体中的 UIBindComponent
        /// </summary>
        public void AutoCollectComponents()
        {
            _uiBindComponentList = GetComponentsInChildren<UIBindComponent>(true).ToList();
        }

        /// <summary>
        /// 设置数据，自动应用到绑定的组件
        /// </summary>
        public void SetData(params IUIDataUnit[] units)
        {
            if (!_isInitialized)
                Initialize();

            if (_uiBindComponentList == null || _uiBindComponentList.Count == 0)
                return;

            foreach (var unit in units)
            {
                if (unit == null)
                    continue;

                // 数据单元寻址方式与当前绑定模式匹配校验：不匹配告警并跳过
                // （否则 ID 模式收到 Index 单元时 TargetId 为 null，查字典会直接抛 ArgumentNullException）
                if (_bindType == BindType.ID && unit.UseIndex)
                {
                    Debug.LogWarning($"[UIBindParent] 当前为 ID 绑定模式，收到 Index 数据单元（TargetIndex={unit.TargetIndex}），已跳过。", this);
                    continue;
                }

                if (_bindType == BindType.Index && !unit.UseIndex)
                {
                    Debug.LogWarning($"[UIBindParent] 当前为 Index 绑定模式，收到 ID 数据单元（TargetId={unit.TargetId}），已跳过。", this);
                    continue;
                }

                switch (_bindType)
                {
                    case BindType.ID:
                        {
                            if (_idDict != null && _idDict.TryGetValue(unit.TargetId, out var component))
                                unit.TryApply(component);
                            break;
                        }
                    case BindType.Index:
                        {
                            if (_indexDict != null && _indexDict.TryGetValue(unit.TargetIndex, out var component))
                                unit.TryApply(component);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// 清空所有绑定的数据（重置为默认值）
        /// </summary>
        public void ClearData()
        {
            if (_uiBindComponentList == null)
                return;

            foreach (var component in _uiBindComponentList)
            {
                // 根据组件类型重置为默认值
                ResetComponentToDefault(component);
            }
        }

        /// <summary>
        /// 重置组件到默认值
        /// </summary>
        private void ResetComponentToDefault(UIBindComponent component)
        {
            switch (component)
            {
                case UIBindComponentGeneric<string> strComponent:
                    strComponent.SetData(string.Empty);
                    break;
                case UIBindComponentGeneric<int> intComponent:
                    intComponent.SetData(0);
                    break;
                case UIBindComponentGeneric<float> floatComponent:
                    floatComponent.SetData(0f);
                    break;
                case UIBindComponentGeneric<bool> boolComponent:
                    boolComponent.SetData(false);
                    break;
                case UIBindComponentGeneric<Sprite> spriteComponent:
                    spriteComponent.SetData(null);
                    break;
            }
        }

        private void OnValidate()
        {
            if (_uiBindComponentList == null || _uiBindComponentList.Count == 0)
            {
                _uiBindComponentList = GetComponentsInChildren<UIBindComponent>(true).ToList();
            }
        }
    }
}