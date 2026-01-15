using UnityEngine;
using AxuFrameKit._Core.Enum;

namespace AxuFrameKit.Common.Utility.Auto.AutoTracker
{
    /// <summary>
    /// Smoothly tracks target position with configurable speed and stop radius
    /// </summary>
    public class SmoothPositionTracker : MonoBehaviour
    {
        [field: SerializeField]
        public Transform Target { get; set; }

        [field: SerializeField]
        public WorldAxis WorldAxis { get; set; } = WorldAxis.All;

        [field: SerializeField]
        public float Speed { get; set; } = 5f;

        [field: SerializeField]
        public float StopRadius { get; set; } = 0.1f;

        [field: SerializeField]
        public bool UseUnscaledTime { get; set; }

        [field: SerializeField]
        public bool AutoStart { get; set; } = true;

        [field: SerializeField]
        public bool IsRunning { get; private set; }

        [field: SerializeField]
        public bool ShowGizmos { get; set; } = true;

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void Start()
        {
            if (AutoStart && Target != null)
                StartTracking();
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;

            if (Target == null)
            {
                StopTracking();
                return;
            }

            TrackPosition();
        }

        public void StartTracking()
        {
            if (Target != null && WorldAxis != WorldAxis.None)
                IsRunning = true;
        }

        public void StopTracking()
        {
            IsRunning = false;
        }

        private void TrackPosition()
        {
            var currentPos = _transform.position;
            var targetPos = Target.position;
            var desiredPos = currentPos;

            if ((WorldAxis & WorldAxis.X) != 0) desiredPos.x = targetPos.x;
            if ((WorldAxis & WorldAxis.Y) != 0) desiredPos.y = targetPos.y;
            if ((WorldAxis & WorldAxis.Z) != 0) desiredPos.z = targetPos.z;

            var delta = desiredPos - currentPos;
            var sqrDistance = delta.sqrMagnitude;

            if (sqrDistance > StopRadius * StopRadius)
            {
                var deltaTime = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var maxDelta = Speed * deltaTime;
                _transform.position = Vector3.MoveTowards(currentPos, desiredPos, maxDelta);
            }
        }

        private void OnDrawGizmos()
        {
            if (!ShowGizmos || Target == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Target.position, StopRadius);
        }
    }
}
