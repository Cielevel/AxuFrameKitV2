using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoCamera
{
    public class GameObjectFaceCamera : MonoBehaviour
    {
        // 应用退出标记：退出拆除期不再访问 FaceCameraManager.Instance（该 getter 永不为 null，缺失时会自动新建管理器）
        private static bool s_Quitting;

        [field: SerializeField]
        public Camera MainCamera { get; private set; }

        private void Awake()
        {
            s_Quitting = false; // 域重载关闭时静态标记可能跨 Play 残留，实例重建时复位

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

        private void OnApplicationQuit() => s_Quitting = true;

        private void OnDestroy()
        {
            // 退出拆除期跳过反注册：此时访问 Instance 会新建残留管理器，且其集合随退出一并销毁
            if (s_Quitting) return;

            FaceCameraManager.Instance.Unregister(this);
        }

        public void UpdateCamera(Camera cam) => MainCamera = cam;
    }
}