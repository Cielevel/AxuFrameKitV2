using UnityEngine;
using AxuFrameKit._Core.Enum;

namespace AxuFrameKit.Common.Utility.Auto.AutoTracker
{
    /// <summary>
    /// Directly tracks target position with optional offset on specified axes
    /// </summary>
    public class PositionTracker : MonoBehaviour
    {
        [field: SerializeField]
        public Transform Target { get; set; }

        [field: SerializeField]
        public WorldAxis WorldAxis { get; set; } = WorldAxis.All;

        [field: SerializeField]
        public bool KeepOffset { get; set; } = true;

        [field: SerializeField]
        public bool AutoStart { get; set; } = true;

        [field: SerializeField]
        public bool IsRunning { get; private set; }

        private Vector3 _offset;
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
            if (Target == null) return;

            if (KeepOffset)
                _offset = _transform.position - Target.position;
            else
                _offset = Vector3.zero;

            IsRunning = true;
        }

        public void StopTracking()
        {
            IsRunning = false;
        }

        public void SetOffset(Vector3 offset)
        {
            _offset = offset;
        }

        public Vector3 GetOffset() => _offset;

        private void TrackPosition()
        {
            var targetPos = Target.position;
            var currentPos = _transform.position;

            var finalPos = currentPos;
            finalPos.x = (WorldAxis & WorldAxis.X) != 0 ? targetPos.x + _offset.x : currentPos.x;
            finalPos.y = (WorldAxis & WorldAxis.Y) != 0 ? targetPos.y + _offset.y : currentPos.y;
            finalPos.z = (WorldAxis & WorldAxis.Z) != 0 ? targetPos.z + _offset.z : currentPos.z;

            _transform.position = finalPos;
        }
    }
}