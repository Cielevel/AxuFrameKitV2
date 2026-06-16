using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    [RequireComponent(typeof(Slider))]
    public class UIBindSlider : UIBindComponentGeneric<float>
    {
        [SerializeField] private Slider _slider;

        public override void SetData(float data) => _slider.value = data;

        private void OnValidate()
        {
            if (_slider == null)
                _slider = GetComponent<Slider>();
        }
    }
}
