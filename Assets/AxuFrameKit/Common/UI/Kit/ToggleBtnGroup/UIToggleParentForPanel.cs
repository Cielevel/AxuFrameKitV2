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

            _panelList = GetComponentsInChildren<UIBaseComponent>().Select(e=> new Panel()
            {
                uiComponent = e,
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