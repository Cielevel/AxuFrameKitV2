using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Text))]
    public class UIBindTextInt : UIBindComponentGeneric<int>
    {
        [SerializeField] private Text _text;
    
        public override void SetData(int data) => _text.text = data.ToString();

        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<Text>();
        }
    }
}