using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind
{
    using Component;
    
    public class UIBindParent : MonoBehaviour
    {
        private enum BindType { ID, Index }
        
        [SerializeField] private BindType _bindType;
        
        [SerializeField] private List<UIBindComponent> _uiBindComponentList;

        private Dictionary<int, UIBindComponent> _indexDict;
        private Dictionary<string, UIBindComponent> _idDict;

        private void Awake()
        {
            switch (_bindType)
            {
                case BindType.ID:
                    _idDict = _uiBindComponentList.ToDictionary(e => e.ID);
                    break;
                case BindType.Index:
                    _indexDict = _uiBindComponentList.ToDictionary(e => e.Index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetData(params IUIDataUnit[] units)
        {
            if (_uiBindComponentList == null || _uiBindComponentList.Count == 0)
                return;

            foreach (var unit in units)
            {
                switch (_bindType)
                {
                    case BindType.ID:
                    {
                        if (_idDict.TryGetValue(unit.TargetId, out var component))
                            unit.TryApply(component);
                        break;
                    }
                    case BindType.Index:
                    {
                        if (_indexDict.TryGetValue(unit.TargetIndex, out var component))
                            unit.TryApply(component);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnValidate()
        {
            _uiBindComponentList = GetComponentsInChildren<UIBindComponent>(true).ToList();
        }
    }
}