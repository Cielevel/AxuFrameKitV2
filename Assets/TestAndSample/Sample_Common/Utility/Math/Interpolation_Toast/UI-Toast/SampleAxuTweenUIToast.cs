using AxuFrameKit.Common.UI.Kit.Toast;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Math.Interpolation_Toast.UI_Toast
{
    public class SampleAxuTweenUIToast : UISideToast<SampleAxuTweenUIUnit>
    {
        private int _index;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Toast();
        }

        protected override void OnToast(SampleAxuTweenUIUnit unit) => unit.SetText(_index++.ToString());
    }
}