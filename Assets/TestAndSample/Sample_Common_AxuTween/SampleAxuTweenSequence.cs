using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;

namespace TestAndSample.Sample_Common_AxuTween
{
    public class SampleAxuTweenSequence : MonoBehaviour
    {
        public Vector3 targetPosition;
        public Vector3 targetEulerAngle;

        public float duration;

        public EaseType easeType;

        private AxuTweener _tweener;
        
        private void Start()
        {
            var sequence = AxuTween.Sequence();

            sequence.Append(transform.Move(targetPosition, duration).SetEase(easeType))
                    .Append(transform.RotateEuler(targetEulerAngle, duration).SetEase(easeType));

            _tweener = sequence;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _tweener.Kill();
            
            if (Input.GetKeyDown(KeyCode.P))
                _tweener.Pause();
            
            if (Input.GetKeyDown(KeyCode.L))
                _tweener.Play();
        }
    }
}
