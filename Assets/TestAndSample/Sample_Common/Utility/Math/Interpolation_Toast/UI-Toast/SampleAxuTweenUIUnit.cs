using AxuFrameKit.Common.UI.Kit.Toast;
using TMPro;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Math.Interpolation_Toast.UI_Toast
{
    public class SampleAxuTweenUIUnit : UIToastUnit
    {
        [SerializeField] private TMP_Text _text;
        
        public void SetText(string content) => _text.text = content;
    }
}