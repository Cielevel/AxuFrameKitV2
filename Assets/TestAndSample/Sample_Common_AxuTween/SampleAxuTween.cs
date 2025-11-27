using System;
using AxuFrameKit._Core.Utility.Math.Interpolation;
using AxuFrameKit.Common.Utility.Math.Interpolation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TestAndSample.Sample_Common_AxuTween
{
    public class SampleAxuTween : MonoBehaviour
    {
        public Vector3 targetPosition;
        public Vector3 targetEulerAngle;

        public float duration;

        public EaseType easeType;

        private AxuTweener<Transform> _tweener;
        
        private void Start()
        {
            _tweener = transform.ToLocalPos(targetPosition, duration).SetEase(easeType).Start().OnComplete(() =>
            {
                transform.ToLocalScale(Vector3.one, duration).SetEase(easeType).Start();
                transform.RotateEuler(targetEulerAngle, duration).SetEase(easeType).Start().OnComplete(() =>
                {
                    transform.RotateEuler(Vector3.zero, duration).SetEase(easeType).Start();
                    transform.ToLocalScale(Vector3.one * 1.5f, duration).SetEase(easeType).Start();
                    transform.ToLocalPos(targetPosition + Vector3.one * 10, duration).SetEase(easeType).Start();
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
            {
                transform.ToPos(Vector3.one * Random.Range(0f, 10f), 2f).Start();
            }
        }
    }
}
