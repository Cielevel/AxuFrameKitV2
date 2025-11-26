using System;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;

namespace TestAndSample.Sample_Common_AxuTween
{
    public class SampleAxuTweenUI : MonoBehaviour
    {
        public Vector3 targetPosition;
        public Vector3 targetEulerAngle;

        public float duration;

        public EaseType easeType;

        private AxuTweener _tweener;
        
        private void Start()
        {
            _tweener = transform.LocalMove(targetPosition, duration).SetEase(easeType).Start().OnComplete(() =>
            {
                transform.LocalScale(Vector3.one, duration).SetEase(easeType).Start();
                transform.RotateEuler(targetEulerAngle, duration).SetEase(easeType).Start().OnComplete(() =>
                {
                    transform.RotateEuler(Vector3.zero, duration).SetEase(easeType).Start();
                    transform.LocalScale(Vector3.one * 1.5f, duration).SetEase(easeType).Start();
                    transform.LocalMove(targetPosition + Vector3.one * 10, duration).SetEase(easeType).Start();
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
