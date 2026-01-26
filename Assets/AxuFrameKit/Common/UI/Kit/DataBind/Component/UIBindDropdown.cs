using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Dropdown))]
    public class UIBindDropdown : UIBindComponentGeneric<int>
    {
        [SerializeField] private Dropdown _dropdown;

        public override void SetData(int data) => _dropdown.value = data;

        private void OnValidate()
        {
            if (_dropdown == null)
                _dropdown = GetComponent<Dropdown>();
        }
    }
}
