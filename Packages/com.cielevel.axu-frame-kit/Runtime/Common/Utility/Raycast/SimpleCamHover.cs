using System;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AxuFrameKit.Common.Utility.Raycast
{
    public class SimpleCamHover : SingletonMonoCrDs<SimpleCamHover>
    {
        [Header("Settings")]
        [Tooltip("用于发射射线的摄像机。如果为空，将使用 Camera.main。")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("射线检测的最大距离。")]
        [SerializeField] private float maxDistance = 100f;

        [Tooltip("允许进行射线检测的层级。")]
        [SerializeField] private LayerMask includeLayerMask = ~0;

        [Tooltip("如果为 true，当鼠标悬停在 UI 元素上时，将阻止射线检测。")]
        [SerializeField] private bool blockOverUI = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLine = true;

        /// <summary>
        /// 当悬停开始时触发（进入物体）
        /// </summary>
        public event Action<Transform> OnHoverEnter;
        
        /// <summary>
        /// 当悬停结束时触发（离开物体）
        /// </summary>
        public event Action<Transform> OnHoverExit;
        
        /// <summary>
        /// 当持续悬停时触发（每帧）
        /// </summary>
        public event Action<RaycastHit> OnHoverStay;

        private Transform _currentHoverTransform;

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
            HandleHover();
        }

        private void HandleHover()
        {
            // Check if pointer is over UI
            if (blockOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                ClearHover();
                return;
            }

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
                    Debug.DrawLine(ray.origin, hit.point, Color.yellow, Time.deltaTime);

                Transform hitTransform = hit.transform;
                
                // 处理 Enter 和 Exit
                if (_currentHoverTransform != hitTransform)
                {
                    if (_currentHoverTransform != null)
                    {
                        OnHoverExit?.Invoke(_currentHoverTransform);
                    }
                    
                    _currentHoverTransform = hitTransform;
                    OnHoverEnter?.Invoke(_currentHoverTransform);
                }

                // 处理 Stay
                OnHoverStay?.Invoke(hit);
            }
            else
            {
                if (showDebugLine)
                    Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red, Time.deltaTime);
                
                ClearHover();
            }
        }

        private void ClearHover()
        {
            if (_currentHoverTransform != null)
            {
                OnHoverExit?.Invoke(_currentHoverTransform);
                _currentHoverTransform = null;
            }
        }
    }
}
