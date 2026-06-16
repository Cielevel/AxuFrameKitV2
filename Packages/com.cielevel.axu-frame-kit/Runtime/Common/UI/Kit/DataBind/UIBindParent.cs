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
                        _idDict = _uiBindComponentList
                            .Where(e => !string.IsNullOrEmpty(e.ID))
                            .ToDictionary(e => e.ID);
                        break;
                    case BindType.Index:
                        _indexDict = _uiBindComponentList.ToDictionary(e => e.Index);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _isInitialized = true;
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