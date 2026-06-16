using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Text))]
    public class UIBindTextStr : UIBindComponentGeneric<string>
    {
        [SerializeField] private Text _text;
        
        public override void SetData(string data) => _text.text = data;
        
        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<Text>();
        }
    }
}