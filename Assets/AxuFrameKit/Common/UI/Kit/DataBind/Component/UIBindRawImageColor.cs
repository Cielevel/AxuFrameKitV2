using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(RawImage))]
    public class UIBindRawImageColor : UIBindComponentGeneric<Color>
    {
        [SerializeField] private RawImage _image;

        public override void SetData(Color data) => _image.color = data;

        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<RawImage>();
        }
    }
}
