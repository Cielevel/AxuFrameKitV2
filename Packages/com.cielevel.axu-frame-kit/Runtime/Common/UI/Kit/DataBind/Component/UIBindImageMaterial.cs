using System;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Image))]
    public class UIBindImageMaterial : UIBindComponentGeneric<Material>
    {
        [SerializeField] private Image _image;

        public override void SetData(Material data) => _image.material = data;

        private void OnValidate()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}