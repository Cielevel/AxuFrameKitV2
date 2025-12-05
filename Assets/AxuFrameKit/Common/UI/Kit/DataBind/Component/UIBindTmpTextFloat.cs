using System;
using TMPro;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(TMP_Text))]
    public class UIBindTmpTextFloat : UIBindComponentGeneric<float>
    {
        [SerializeField] private TMP_Text _text;
    
        [SerializeField] private int _decimalPlace = 2;
        
        public override void SetData(float data)
        {
            _text.text = data.ToString($"F{Mathf.Max(0, _decimalPlace)}");
        }

        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
        }
    }
}