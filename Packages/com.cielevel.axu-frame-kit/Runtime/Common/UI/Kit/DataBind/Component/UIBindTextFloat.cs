using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Text))]
    public class UIBindTextFloat : UIBindComponentGeneric<float>
    {
        [SerializeField] private Text _text;
    
        [SerializeField] private int _decimalPlace = 2;
        
        public override void SetData(float data)
        {
            _text.text = data.ToString($"F{Mathf.Max(0, _decimalPlace)}");
        }

        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<Text>();
        }
    }
}