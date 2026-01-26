using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Image))]
    public class UIBindImageColor : UIBindComponentGeneric<Color>
    {
        [SerializeField] private Image _image;

        public override void SetData(Color data) => _image.color = data;

        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}
