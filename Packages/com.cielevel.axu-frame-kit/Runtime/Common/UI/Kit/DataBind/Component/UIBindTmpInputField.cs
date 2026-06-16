using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(TMP_InputField))]
    public class UIBindTmpInputField : UIBindComponentGeneric<string>
    {
        [SerializeField] private TMP_InputField _inputField;

        public override void SetData(string data) => _inputField.text = data;

        private void OnValidate()
        {
            if (_inputField == null)
                _inputField = GetComponent<TMP_InputField>();
        }
    }
}
