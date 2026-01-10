using System;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AxuFrameKit.Common.Utility.Raycast
{
    public class SimpleCamClicker : SingletonMonoCrDs<SimpleCamClicker>
    {
        [Header("Settings")]
        [Tooltip("用于发射射线的摄像机。如果为空，将使用 Camera.main。")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("射线检测的最大距离。")]
        [SerializeField] private float maxDistance = 100f;

        [Tooltip("允许进行射线检测的层级。")]
        [SerializeField] private LayerMask includeLayerMask = ~0;

        [Tooltip("触发射线检测的鼠标按键索引（0=左键，1=右键，2=中键）。")]
        [SerializeField] private int mouseButtonIndex = 0;

        [Tooltip("如果为 true，当鼠标悬停在 UI 元素上时，将阻止射线检测。")]
        [SerializeField] private bool blockOverUI = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLine = true;

        public event Action<Vector3> OnDetectPoint;
        public event Action<Transform> OnDetectTransform;
        public event Action<RaycastHit> OnDetectHit;

        private Camera TargetCamera
        {
            get
            {
                if (targetCamera == null)
                    targetCamera = Camera.main;
                return targetCamera;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(mouseButtonIndex))
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            // Check if pointer is over UI
            if (blockOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            PerformRaycast();
        }

        private void PerformRaycast()
        {
            if (TargetCamera == null)
                return;

            Ray ray = TargetCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, maxDistance, includeLayerMask))
            {
                if (showDebugLine)
                    Debug.DrawLine(ray.origin, hit.point, Color.green, 1f);

                OnDetectPoint?.Invoke(hit.point);
                OnDetectTransform?.Invoke(hit.transform);
                OnDetectHit?.Invoke(hit);
            }
            else
            {
                if (showDebugLine)
                    Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red, 1f);
            }
        }

        /// <summary>
        /// Try to get the current hit result under the mouse.
        /// Useful for polling from other scripts.
        /// </summary>
        public bool TryGetHit(out RaycastHit hit)
        {
            hit = default;
            if (TargetCamera == null)
                return false;

            Ray ray = TargetCamera.ScreenPointToRay(Input.mousePosition);
            return Physics.Raycast(ray, out hit, maxDistance, includeLayerMask);
        }
    }
}
