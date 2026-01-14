using System;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoDestroy
{
    public class GameObjectAutoDestroy : MonoBehaviour
    {
        public event Action Destroyed;
        public event Action<Vector3, Quaternion, Vector3> DestroyedInfo;

        [field: SerializeField]
        public float Duration { get; private set; } = 1f;

        [field: SerializeField]
        public bool AutoStart { get; private set; } = true;

        [field: SerializeField]
        public bool UseUnscaledTime { get; private set; }

        private float _timer;
        private bool _isRunning;

        private void Start()
        {
            if (AutoStart)
                _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            _timer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_timer < Duration)
                return;

            var position = transform.position;
            var rotation = transform.rotation;
            var scale = transform.localScale;

            Destroyed?.Invoke();
            DestroyedInfo?.Invoke(position, rotation, scale);
            Destroy(gameObject);
        }

        public void StartTimer() => _isRunning = true;

        public void StopTimer() => _isRunning = false;

        public void ResetTimer()
        {
            _timer = 0f;
            _isRunning = false;
        }

        public void RestartTimer()
        {
            _timer = 0f;
            _isRunning = true;
        }
    }
}