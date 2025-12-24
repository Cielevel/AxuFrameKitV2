using System;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ToggleGroup
{
    [RequireComponent(typeof(Image)), RequireComponent(typeof(ToggleChild))]
    public class ToggleButtonSpriteUnit : ToggleBaseUnit
    {
        [SerializeField] private Image _image;
        
        [SerializeField] private Sprite _spriteOn;
        [SerializeField] private Sprite _spriteOff;

        public override void SetState(bool state) => _image.sprite = state ? _spriteOn : _spriteOff;
        
        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}