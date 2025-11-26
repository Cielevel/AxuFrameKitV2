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
                    .Append(transform.RotateEuler(targetEulerAngle, duration).SetEase(easeType))
                    .Append(transform.Move(Vector3.zero, duration).SetEase(easeType))
                    .Append(transform.LocalScale(Vector3.one * 1.3f, duration).SetEase(easeType))
                    .Append(transform.LocalScale(Vector3.one, duration).SetEase(easeType))
                    .Start();

            _tweener = sequence;
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
