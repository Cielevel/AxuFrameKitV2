using TMPro;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(TMP_Text))]
    public class UIBindTmpTextStr : UIBindComponentGeneric<string>
    {
        [SerializeField] private TMP_Text _text;
        
        public override void SetData(string data) => _text.text = data;
        
        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
        }
    }
}