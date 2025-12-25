using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ToggleGroup
{
    public class UIToggleParent : MonoBehaviour
    {
        public event Action<string> OnToggleIdChanged;
        public event Action<int> OnToggleIndexChanged; 
        
        public event Action<List<string>> OnMultipleToggleIdChanged;
        public event Action<List<int>> OnMultipleToggleIndexChanged;
        
        [SerializeField] private bool _autoValidateChild;
        
        [Header("Setting")]
        [SerializeField] private bool _allowMultipleOn; // 允许多个child状态为true
        [SerializeField] private bool _allowNoneOn; // 允许无child状态为true，否则至少一个child状态为true（默认为wrapper中第一个默认状态为true的，或者第一个）

        [Space]
        
        [SerializeField] private List<ToggleChildWrapper> _childWrapperList;

        private List<string> _activeMultipleToggleIdList;
        private List<int> _activeMultipleToggleIndexList;
        
        private void Start()
        {
            if (_childWrapperList == null || _childWrapperList.Count == 0)
            {
                Debug.LogWarning($"ToggleParent({gameObject.name})的child列表为空，将默认设置子物体中的child到列表中");
                _childWrapperList = GetComponentsInChildren<UIToggleChild>()
                                    .Select(e => new ToggleChildWrapper() { Child = e }).ToList();

                if (_childWrapperList == null || _childWrapperList.Count == 0)
                {
                    Debug.LogError($"ToggleParent({gameObject.name})没有找到任何ToggleChild组件，请检查");
                    return;
                }
            }

            OnValidate(); // 确保在Start时进行一次验证，防止编辑器中未调用OnValidate导致的问题

            foreach (var wrapper in _childWrapperList)
            {
                wrapper.Child.Init(this, wrapper.ID, wrapper.Index, OnToggleId, OnToggleIndex);
                wrapper.Child.SetState(wrapper.DefaultState);
            }

            if (_allowMultipleOn)
            {
                _activeMultipleToggleIdList    = new List<string>(_childWrapperList.Count);
                _activeMultipleToggleIndexList = new List<int>(_childWrapperList.Count);
            }
        }

        private void OnToggleId(string id)
        {
            OnToggleIdChanged?.Invoke(id);

            if (_allowMultipleOn)
            {
                var state = _childWrapperList.Find(e => e.ID == id).State;

                if (state)
                    _activeMultipleToggleIdList.Add(id);
                else
                    _activeMultipleToggleIdList.Remove(id);
                
                OnMultipleToggleIdChanged?.Invoke(_activeMultipleToggleIdList);
            }
        }

        private void OnToggleIndex(int index)
        {
            OnToggleIndexChanged?.Invoke(index);

            if (_allowMultipleOn)
            {
                var state = _childWrapperList.Find(e => e.Index == index).State;

                if (state)
                    _activeMultipleToggleIndexList.Add(index);
                else
                    _activeMultipleToggleIndexList.Remove(index);
                
                OnMultipleToggleIndexChanged?.Invoke(_activeMultipleToggleIndexList);
            }
        }

        #region Cache and Validate
        
        private List<bool> _boolCache = new();
        
        private void OnValidate()
        {
            if (_autoValidateChild == false) return;

            // 1. 基础列表填充与同步
            SyncChildList();

            if (_childWrapperList == null || _childWrapperList.Count == 0)
                return;

            // 2. 处理“至少有一个为True”的情况
            if (_allowNoneOn == false && _childWrapperList.Count > 0 && _childWrapperList.All(e => !e.DefaultState))
            {
                _childWrapperList[0].DefaultState = true;
            }

            // 3. 处理“单选”逻辑
            if (_allowMultipleOn == false)
            {
                // 如果缓存为空或长度不匹配，直接更新缓存并退出，防止初始化时的逻辑干扰
                if (_boolCache == null || _boolCache.Count != _childWrapperList.Count)
                {
                    UpdateCache();
                    return;
                }

                // 寻找用户在面板上“新勾选”的那一项
                int newlySelected = -1;
                for (int i = 0; i < _childWrapperList.Count; i++)
                {
                    // 如果当前项为 true，但缓存中为 false，说明这是刚刚点的
                    if (_childWrapperList[i].DefaultState && _boolCache[i] == false)
                    {
                        newlySelected = i;
                        break;
                    }
                }

                // 如果找到了新勾选的项，则强制将其他所有项置为 false
                if (newlySelected != -1)
                {
                    for (int i = 0; i < _childWrapperList.Count; i++)
                    {
                        if (i != newlySelected)
                        {
                            _childWrapperList[i].DefaultState = false;
                        }
                    }
                }
                // 特殊情况：如果用户手动把仅有的一个 True 关掉了，且不允许NoneOn
                else if (!_allowNoneOn && _childWrapperList.Count(e => e.DefaultState) == 0)
                {
                    // 这种情况下由于没有 newSelected，可以默认回退到第一个
                    _childWrapperList[0].DefaultState = true;
                }
                
                // 最后处理：单选时，如果有多个被选中，只保留第一个
                if (_childWrapperList.Count(e => e.DefaultState) > 1)
                {
                    _childWrapperList.ForEach(e => e.DefaultState = false);
                    _childWrapperList[0].DefaultState = true;
                }
            }

            UpdateCache();
        }

        // 建议将填充逻辑抽离，使 OnValidate 结构清晰
        private void SyncChildList()
        {
            var children = GetComponentsInChildren<UIToggleChild>();
            
            _childWrapperList ??= new List<ToggleChildWrapper>();

            // 清理可能已经销毁的引用
            _childWrapperList.RemoveAll(e => e.Child == null);

            foreach (var child in children)
            {
                if (_childWrapperList.Any(e => e.Child == child)) continue;
                _childWrapperList.Add(new ToggleChildWrapper
                {
                    Child = child,
                    ID    = child.gameObject.name.ToLower().Replace(' ', '_'),
                    Index = child.transform.GetSiblingIndex()
                });
            }
        }

        private void UpdateCache() 
        {
            _boolCache.Clear();
            if (_childWrapperList != null)
            {
                foreach(var item in _childWrapperList)
                    _boolCache.Add(item.DefaultState);
            }
        }
        
        #endregion

        [Serializable]
        private class ToggleChildWrapper
        {
            [field: SerializeField] public UIToggleChild Child { get; set; }
            
            [field: SerializeField] public bool DefaultState { get; set; }
            
            [field: SerializeField] public string ID { get; set; }
            
            [field: SerializeField] public int Index { get; set; }

            public bool State => Child.State;
        }
    }
}
