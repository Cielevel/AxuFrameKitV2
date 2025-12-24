using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ToggleGroup
{
    /// <summary>
    /// 一个物体上对ToggleUnit进行控制的组件；同时处理逻辑层
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ToggleChild : MonoBehaviour
    {
        /// <summary>
        /// 可用于直接引用注册
        /// </summary>
        public event Action<bool> OnSetState; 

        [SerializeField] private Button _button;
        
        [SerializeField] private bool _autoValidate;
        
        [SerializeField] private List<ToggleBaseUnit> _unitList;

        [SerializeField] private ToggleParent _parent;

        public bool State { get; private set; }

        private string _id;
        private int _index;

        public void Init(ToggleParent parent, string id, int index, Action<string> onToggleId, Action<int> onToggleIndex)
        {
            _parent = parent;
            _id     = id;
            _index  = index;

            _button.onClick.AddListener(() => onToggleIndex?.Invoke(_index));
            _button.onClick.AddListener(() => onToggleId?.Invoke(_id));
            _button.onClick.AddListener(OnClick);
        }

        public void SetState(bool state)
        {
            State = state;
            
            foreach (var unit in _unitList)
                unit.SetState(State);
            
            OnSetState?.Invoke(State); // 注意初始化状态时，也会调用
        }

        private void OnClick() // 按钮按下，切换状态。
        {
            State = !State;

            SetState(State);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();

            OnSetState = delegate { };
        }

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_autoValidate == false) return;
            
            _parent = transform.parent.GetComponent<ToggleParent>();
            
            _unitList = GetComponents<ToggleBaseUnit>().ToList();
        }
    }
}