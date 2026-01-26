using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;
using UnityEngine.UI;

namespace TestAndSample.Sample_Common.Utility.Math.Interpolation_Toast
{
    public class SampleAxuTweenUI : MonoBehaviour
    {
        public Vector3 targetPosition;
        public Vector3 targetEulerAngle;

        [Space]
        
        public Image img;
        public Color toColorA, toColorB;

        [Space]
        
        public float duration;

        public EaseType easeType;

        private AxuTweener<Transform> _tweener;
        
        private void Start()
        {
            img.ToColor(toColorA, duration).Start().OnComplete(() =>
            {
                img.ToColor(toColorB, duration).Start().OnComplete(() =>
                {
                    img.ToColor(toColorA, duration).Start();
                });
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _tweener.Kill();
            
            if (Input.GetKeyDown(KeyCode.P))
                _tweener.Pause();
            
            if (Input.GetKeyDown(KeyCode.L))
                _tweener.Resume();

            if (Input.GetKeyDown(KeyCode.R))
                AxuTween.Replay(_tweener);
        }
    }
}
