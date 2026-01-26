using System;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(RawImage))]
    public class UIBindRawImageTexture : UIBindComponentGeneric<Texture>
    {
        [SerializeField] private RawImage _image;

        public override void SetData(Texture data) => _image.texture = data;

        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<RawImage>();
        }
    }
}