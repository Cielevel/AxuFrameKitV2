using System;
using UnityEngine;
using AxuFrameKit._Core.Enum;

namespace AxuFrameKit.Common.Utility.Auto.AutoFollower
{
    public class PositionFollower : MonoBehaviour
    {
        [field: SerializeField]
        public Transform Target { get; set; }

        [field: SerializeField]
        public WorldAxis WorldAxis { get; set; }

        [field: SerializeField]
        public bool KeepOffset { get; set; } = true;

        [field: SerializeField]
        public bool AutoStart { get; set; } = true;

        [field: SerializeField]
        public bool IsRunning { get; private set; }

        private Vector3 _offset;

        private void Start()
        {
            if (AutoStart && Target != null)
                StartFollow();
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;

            if (Target == null)
            {
                IsRunning = false;
                return;
            }

            Follow();
        }

        public void StartFollow()
        {
            if (Target == null) return;

            if (KeepOffset)
                _offset = transform.position - Target.position;
            else
                _offset = Vector3.zero;

            IsRunning = true;
        }

        public void StopFollow()
        {
            IsRunning = false;
        }

        private void Follow()
        {
            Vector3 targetPos = Target.position;
            Vector3 currentPos = transform.position;
            Vector3 finalPos = currentPos;

            if ((WorldAxis & WorldAxis.X) == WorldAxis.X)
                finalPos.x = targetPos.x + _offset.x;

            if ((WorldAxis & WorldAxis.Y) == WorldAxis.Y)
                finalPos.y = targetPos.y + _offset.y;

            if ((WorldAxis & WorldAxis.Z) == WorldAxis.Z)
                finalPos.z = targetPos.z + _offset.z;

            // 避免不必要的赋值开销
            if (transform.position != finalPos)
                transform.position = finalPos;
        }
    }
}