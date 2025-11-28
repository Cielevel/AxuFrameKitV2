using System;
using AxuFrameKit.Common.UI.Kit;
using AxuFrameKit.Common.UI.Kit.Toast;
using UnityEngine;

namespace TestAndSample.Sample_Common_AxuTween.UI_Toast
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