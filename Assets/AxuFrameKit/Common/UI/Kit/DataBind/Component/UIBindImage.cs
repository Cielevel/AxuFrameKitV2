using System;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Image))]
    public class UIBindImage : UIBindComponentGeneric<Sprite>
    {
        [SerializeField] private Image _image;
        
        public override void SetData(Sprite data) => _image.sprite = data;

        private void OnValidate()
        {
            if (_image == null) 
                _image = GetComponent<Image>();
        }
    }
}