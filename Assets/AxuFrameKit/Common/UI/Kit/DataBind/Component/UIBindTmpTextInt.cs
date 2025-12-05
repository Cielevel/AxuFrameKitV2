using System;
using TMPro;
using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(TMP_Text))]
    public class UIBindTmpTextInt : UIBindComponentGeneric<int>
    {
        [SerializeField] private TMP_Text _text;
    
        public override void SetData(int data) => _text.text = data.ToString();

        private void OnValidate()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
        }
    }
}