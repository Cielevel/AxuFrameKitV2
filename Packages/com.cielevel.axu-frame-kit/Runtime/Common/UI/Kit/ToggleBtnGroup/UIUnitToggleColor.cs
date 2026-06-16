using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    [RequireComponent(typeof(Image))]
    public class UIUnitToggleColor : UIToggleBaseUnit
    {
        [SerializeField] private Image _image;
        
        [SerializeField] private Color _colorOn;
        [SerializeField] private Color _colorOff;

        public override void SetState(bool state) => _image.color = state ? _colorOn : _colorOff;
        
        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}