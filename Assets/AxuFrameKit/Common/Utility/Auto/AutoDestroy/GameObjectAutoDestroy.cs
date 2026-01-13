using System;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    public class GameObjectAutoDestroy : MonoBehaviour
    {
        public event Action OnDestroy;
        public event Action<Vector3, Quaternion, Vector3> OnDestroyInfo;

        [field: SerializeField]
        public float Duration { get; private set; } = 1f;

        [field: SerializeField]
        public bool AutoStart { get; private set; } = true;

        private float _timer;

        private bool _isRunning;

        private void Start()
        {
            if (AutoStart)
                _isRunning = true;
        }

        private void Update()
        {
            if (_isRunning == false)
                return;

            _timer += Time.deltaTime;
            if (_timer < Duration)
                return;

            OnDestroy?.Invoke();
            OnDestroyInfo?.Invoke(transform.position, transform.rotation, transform.localScale);
            Destroy(gameObject);
            _isRunning = false;
        }
    }
}