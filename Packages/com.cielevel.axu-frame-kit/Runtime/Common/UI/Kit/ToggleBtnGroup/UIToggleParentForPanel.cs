using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.UI.Framework;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    public class UIToggleParentForPanel : MonoBehaviour
    {
        [SerializeField] private bool _autoValidate;

        [SerializeField] private bool _useIndex;
        
        [SerializeField] private UIToggleParent _toggleParent;
        
        [SerializeField] private List<Panel> _panelList;

        private void Awake()
        {
            if (_toggleParent == null)
            {
                Debug.LogError("UITogglePanelParent: ToggleParent为空！");
                return;
            }

            if (_toggleParent.AllowMultipleOn)
            {
                if (_useIndex)
                    _toggleParent.OnMultipleSelectionIndexChanged += OnMultipleSelectionIndexChanged;
                else
                    _toggleParent.OnMultipleSelectionIdChanged += OnMultipleSelectionIdChanged;
            }
            else
            {
                if (_useIndex)
                    _toggleParent.OnIndexChanged += OnIndexChanged;
                else
                    _toggleParent.OnIdChanged += OnIdChanged;
            }
        }

        private void OnDestroy()
        {
            // 成对注销 Awake 中的订阅（未订阅的 -= 是无害空操作）；场景卸载时对方可能已销毁
            if (_toggleParent == null)
                return;

            _toggleParent.OnMultipleSelectionIndexChanged -= OnMultipleSelectionIndexChanged;
            _toggleParent.OnMultipleSelectionIdChanged -= OnMultipleSelectionIdChanged;
            _toggleParent.OnIndexChanged -= OnIndexChanged;
            _toggleParent.OnIdChanged -= OnIdChanged;
        }

        private void OnIdChanged(string id) // 单选开启
        {
            _panelList.ForEach(e =>
            {
                if (e.id == id)
                    e.uiComponent.Show();
                else
                    e.uiComponent.Hide();
            });
        }

        private void OnIndexChanged(int index) // 单选开启
        {
            _panelList.ForEach(e =>
            {
                if (e.index == index)
                    e.uiComponent.Show();
                else
                    e.uiComponent.Hide();
            });
        }

        private void OnMultipleSelectionIdChanged(List<string> multipleSelection)
        {
            _panelList.ForEach(e =>
            {
                if (multipleSelection.Contains(e.id))
                    e.uiComponent.Show();
                else
                    e.uiComponent.Hide();
            });
        }
        
        private void OnMultipleSelectionIndexChanged(List<int> multipleSelection)
        {
            _panelList.ForEach(e =>
            {
                if (multipleSelection.Contains(e.index))
                    e.uiComponent.Show();
                else
                    e.uiComponent.Hide();
            });
        }

        private void OnValidate()
        {
            if (_autoValidate == false)
                return;

            // 只收集 UIBasePanel（Area/Page/Unit/Toast 等基类组件不参与），
            // 并同步填 id（组件 ID 为空则用物体名），保证 OnIdChanged 的 e.id == id 比较有值可命中
            _panelList = GetComponentsInChildren<UIBasePanel>().Select(e => new Panel()
            {
                uiComponent = e,
                id = string.IsNullOrEmpty(e.ID) ? e.gameObject.name : e.ID,
                index = e.transform.GetSiblingIndex()
            }).ToList();
        }

        [Serializable]
        private class Panel
        {
            public UIBaseComponent uiComponent;
            public string id;
            public int index;
        }
    }
}