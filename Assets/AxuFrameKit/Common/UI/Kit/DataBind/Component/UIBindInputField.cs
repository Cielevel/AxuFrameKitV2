using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(InputField))]
    public class UIBindInputField : UIBindComponentGeneric<string>
    {
        [SerializeField] private InputField _inputField;

        public override void SetData(string data) => _inputField.text = data;

        private void OnValidate()
        {
            if (_inputField == null)
                _inputField = GetComponent<InputField>();
        }
    }
}
