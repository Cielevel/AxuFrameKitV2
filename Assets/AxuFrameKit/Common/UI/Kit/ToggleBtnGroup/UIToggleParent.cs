using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    /// <summary>
    /// Toggle组控制器
    /// <para>改进点：集中管理状态逻辑（单选/多选/防空选），支持运行时动态注册</para>
    /// </summary>
    public class UIToggleParent : MonoBehaviour
    {
        #region Inner Events

        /// <summary> 单选模式下，选中的ID变化 </summary>
        public event Action<string> OnIdChanged;
        /// <summary> 单选模式下，选中的Index变化 </summary>
        public event Action<int> OnIndexChanged;

        /// <summary> 多选模式下，选中的ID列表变化 </summary>
        public event Action<List<string>> OnMultipleSelectionIdChanged;
        public event Action<List<int>> OnMultipleSelectionIndexChanged;

        #endregion

        #region Settings

        public bool AllowMultipleOn => _allowMultipleOn;

        [Header("Settings")]
        [Tooltip("是否允许多选")]
        [SerializeField] private bool _allowMultipleOn = false;

        [Tooltip("是否允许全部关闭（如果为false，点击当前已选中的将无效）")]
        [SerializeField] private bool _allowNoneOn = false;

        [Tooltip("是否在Start时自动查找子物体并初始化")]
        [SerializeField] private bool _initOnStart = true;

        [Tooltip("是否在Start时发送变更事件？")]
        [SerializeField] private bool _triggerEventOnInit;

        #endregion

        #region Data

        // 包装类由内部维护，不直接暴露给Inspector，防止配置脱节
        [SerializeField] protected List<ToggleChildConfig> _childConfigs = new();

        protected List<UIToggleChild> _runtimeChildren = new();

        #endregion

        private void Start()
        {
            if (_initOnStart)
            {
                InitializeGroup();
            }
        }

        /// <summary>
        /// 手动初始化（如果生成的按钮是动态实例化的，可以调这个）
        /// </summary>
        public void InitializeGroup()
        {
            // 收集场景中的 Child 组件
            // 如果 _childConfigs 为空或数量不匹配，尝试重新搜集
            var childrenComponents = GetComponentsInChildren<UIToggleChild>(true); // include inactive

            _runtimeChildren.Clear();
            _runtimeChildren.AddRange(childrenComponents);

            // 确保配置列表和组件对应（简单的一一对应策略）
            // 如果你在编辑器里配好了 Config，就用 Config 的默认值；
            // 如果没配（动态生成的），就用默认值 false

            for (int i = 0; i < _runtimeChildren.Count; i++)
            {
                var child = _runtimeChildren[i];
                var id = child.gameObject.name; // 默认用名字做ID
                var index = i;
                var defaultState = false;

                // 尝试匹配配置中的默认值
                if (i < _childConfigs.Count && _childConfigs[i].child == child)
                {
                    id = _childConfigs[i].id;
                    index = _childConfigs[i].index;
                    defaultState = _childConfigs[i].defaultState;
                }

                child.Init(this, id, index, defaultState);
            }

            // 初始化的逻辑校验（确保满足 单选/必选 规则）
            ValidateStates();
        }

        /// <summary>
        /// 核心逻辑入口：处理子物体点击
        /// </summary>
        public void OnChildClick(UIToggleChild clickedChild)
        {
            var currentState = clickedChild.IsOn;

            // --- 多选模式逻辑 ---
            if (_allowMultipleOn)
            {
                // 如果当前是开，想关 -> 检查是否允许全关
                if (currentState)
                {
                    var onCount = _runtimeChildren.Count(c => c.IsOn);

                    if (_allowNoneOn == false && onCount <= 1)
                        // 已经是最后一个开启的了，且不允许全关 -> 禁止操作
                        return;

                    // 允许关闭
                    clickedChild.SetState(false);
                }
                else
                {
                    // 当前是关，想开 -> 直接开
                    clickedChild.SetState(true);
                }

                NotifyMultipleChange();
            }
            // --- 单选模式逻辑 ---
            else
            {
                if (currentState)
                {
                    // 当前已是开启状态，再次点击
                    if (_allowNoneOn == false)
                        return; // 不允许为空，点击已选中的 -> 什么都不做

                    clickedChild.SetState(false);
                    NotifySingleChange(null, -1);

                }
                else
                {
                    // 当前是关闭，点击开启 -> 互斥逻辑
                    foreach (var child in _runtimeChildren.Where(child => child != clickedChild && child.IsOn))
                        child.SetState(false);

                    clickedChild.SetState(true);
                    NotifySingleChange(clickedChild.ID, clickedChild.Index);
                }
            }
        }

        /// <summary>
        /// 校验并修正当前状态（用于初始化或强制重置时）
        /// </summary>
        private void ValidateStates()
        {
            var activeChildren = _runtimeChildren.Where(c => c.IsOn).ToList();

            if (_allowMultipleOn)
            {
                // 多选模式且不允许全关：检查是否必须有至少一个
                if (_allowNoneOn == false && activeChildren.Count == 0 && _runtimeChildren.Count > 0)
                {
                    _runtimeChildren[0].SetState(true);
                    activeChildren.Add(_runtimeChildren[0]);
                }

                if (_triggerEventOnInit)
                    NotifyMultipleChange();
            }
            else
            {
                // 单选模式
                if (activeChildren.Count > 1)
                {
                    // 如果有多个开着，只保留第一个，其他全关
                    for (int i = 1; i < activeChildren.Count; i++)
                        activeChildren[i].SetState(false);

                    activeChildren[0].SetState(true);

                    if (_triggerEventOnInit)
                        NotifySingleChange(activeChildren[0].ID, activeChildren[0].Index);
                }
                else if (activeChildren.Count == 0 && _allowNoneOn == false && _runtimeChildren.Count > 0)
                {
                    // 如果全是关的，且不允许空，默认开第一个
                    _runtimeChildren[0].SetState(true);
                    // 此时不一定触发回调，看具体需求。通常初始化不触发回调，或者在Init结束后手动触发一次。

                    if (_triggerEventOnInit)
                        NotifySingleChange(activeChildren[0].ID, activeChildren[0].Index);
                }
            }
        }

        #region Public API

        /// <summary>
        /// 通过索引选中（代码驱动）
        /// </summary>
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _runtimeChildren.Count) return;
            var child = _runtimeChildren[index];

            // 复用点击逻辑，保证规则一致
            // 但如果需要强制选中而不受规则限制，可另写逻辑
            if (!child.IsOn)
            {
                OnChildClick(child);
            }
        }

        public void SelectID(string id)
        {
            var child = _runtimeChildren.FirstOrDefault(c => c.ID == id);
            if (child != null && !child.IsOn)
            {
                OnChildClick(child);
            }
        }

        #endregion

        private void NotifySingleChange(string id, int index)
        {
            OnIdChanged?.Invoke(id);
            OnIndexChanged?.Invoke(index);
        }

        private void NotifyMultipleChange()
        {
            var onCollection = _runtimeChildren.Where(c => c.IsOn).ToArray();

            var ids = onCollection.Select(c => c.ID).ToList();
            var indexes = onCollection.Select(c => c.Index).ToList();
            OnMultipleSelectionIdChanged?.Invoke(ids);
            OnMultipleSelectionIndexChanged?.Invoke(indexes);
        }

        // 编辑器数据结构，用于Inspector配置
        [Serializable]
        public class ToggleChildConfig
        {
            public UIToggleChild child;
            public string id;
            public int index;
            public bool defaultState;
        }

        #region Editor Helper

        private void OnValidate()
        {
            // 简单的编辑器辅助，自动填名
            if (_childConfigs != null)
                foreach (var config in _childConfigs.Where(config => config.child != null && string.IsNullOrEmpty(config.id)))
                    config.id = config.child.name;
        }

        [InsButton]
        public void AutoPopulateChildren()
        {
            _childConfigs.Clear();
            var children = GetComponentsInChildren<UIToggleChild>();
            foreach (var child in children)
            {
                child.CollectUnits(); // 顺便帮子物体收集一下Unit
                _childConfigs.Add(new ToggleChildConfig
                {
                    child = child,
                    id = child.name,
                    index = child.transform.GetSiblingIndex(),
                    defaultState = false
                });
            }
            // 默认勾选第一个
            if (_childConfigs.Count > 0 && !_allowNoneOn)
            {
                _childConfigs[0].defaultState = true;
            }
        }

        #endregion
    }
}