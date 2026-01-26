using System;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(RawImage))]
    public class UIBindRawImage : UIBindComponentGeneric<Sprite>
    {
        [SerializeField] private RawImage _image;

        public override void SetData(Sprite data) => _image.texture = data.texture;

        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<RawImage>();
        }
    }
}