using System;
using AxuFrameKit._Core.Enum;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoFollower
{
    /// <summary>
    /// 有中间过渡的跟随器
    /// </summary>
    public class SmoothPositionFollower : MonoBehaviour
    {
        [field: SerializeField]
        public Transform Target { get; set; }

        [field: SerializeField]
        public WorldAxis WorldAxis { get; set; }

        [field: SerializeField]
        public float Speed { get; set; } // 移动速度

        [field: SerializeField]
        public float StopRadius { get; set; } // 停止距离（小于或等于这个距离就停止）

        [field: SerializeField]
        public bool AutoStart { get; set; } = true;

        [field: SerializeField]
        public bool IsRunning { get; private set; }

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
            if (Target != null && WorldAxis != WorldAxis.None)
                IsRunning = true;
        }

        /// <summary>
        /// 停止工作
        /// </summary>
        public void StopFollow()
        {
            IsRunning = false;
        }

        private void Follow()
        {
            Vector3 currentPos = transform.position;
            Vector3 targetFullPos = Target.position;
            Vector3 desiredPos = currentPos;

            // 根据轴向确定目标位置
            if ((WorldAxis & WorldAxis.X) == WorldAxis.X) desiredPos.x = targetFullPos.x;
            if ((WorldAxis & WorldAxis.Y) == WorldAxis.Y) desiredPos.y = targetFullPos.y;
            if ((WorldAxis & WorldAxis.Z) == WorldAxis.Z) desiredPos.z = targetFullPos.z;

            float distance = Vector3.Distance(currentPos, desiredPos);

            if (distance > StopRadius)
            {
                // 向目标位置移动
                transform.position = Vector3.MoveTowards(currentPos, desiredPos, Speed * Time.deltaTime);
            }
        }

        private void OnDrawGizmos()
        {
            if (Target != null)
                Gizmos.DrawWireSphere(Target.position, StopRadius);
        }
    }
}
