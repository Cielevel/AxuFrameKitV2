using UnityEngine;
using TMPro;
using AxuFrameKit.Common.UI.Kit.Typewriter;

namespace TestAndSample.Sample_Common.UI.Kit.Typewriter
{
    public class SampleCommonTypewriter : MonoBehaviour
    {
        public TMP_Text text;

        public float charInterval = 0.1f;

        public bool smooth = false;

        private void Start()
        {
            TmpTypewriterManager.Instance.Play(text, charInterval, smooth: smooth);
        }
    }
}