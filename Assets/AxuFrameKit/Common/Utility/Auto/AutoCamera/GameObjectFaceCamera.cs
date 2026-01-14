using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoCamera
{
    public class GameObjectFaceCamera : MonoBehaviour
    {
        [field: SerializeField]
        public Camera MainCamera { get; private set; }

        private void Awake()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            FaceCameraManager.Instance.Register(this);
        }

        private void LateUpdate()
        {
            if (MainCamera != null)
            {
                var cameraTransform = MainCamera.transform;
                var cameraPosition = cameraTransform.position;
                var transformPosition = transform.position;

                // 使用 LookAt 方式确保物体始终朝向相机
                transform.LookAt(cameraPosition - (cameraPosition - transformPosition) * 0.99f + cameraTransform.forward * 0.01f);
            }
        }

        private void OnDestroy()
        {
            if (FaceCameraManager.Instance != null)
                FaceCameraManager.Instance.Unregister(this);
        }

        public void UpdateCamera(Camera cam) => MainCamera = cam;
    }
}