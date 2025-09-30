using UnityEngine;

namespace AxuFrameKit.Common.Auto.AutoCamera
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

        private void Update()
        {
            transform.forward = MainCamera.transform.forward;
        }

        private void OnDestroy()
        {
            FaceCameraManager.Instance.Unregister(this);
        }

        public void UpdateCamera(Camera cam) => MainCamera = cam;
    }
}
