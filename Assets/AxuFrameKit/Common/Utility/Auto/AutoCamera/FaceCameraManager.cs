using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoCamera
{
    public class FaceCameraManager : SingletonMonoCrDs<FaceCameraManager>
    {
        private readonly HashSet<GameObjectFaceCamera> _faceCameraHs = new(16);

        public void Register(GameObjectFaceCamera faceCamera)
        {
            _faceCameraHs.Add(faceCamera);
        }

        public void Unregister(GameObjectFaceCamera faceCamera)
        {
            _faceCameraHs.Remove(faceCamera);
        }

        /// <summary>
        /// 朝向的相机变更
        /// </summary>
        public void UpdateCamera(Camera cam)
        {
            foreach (var faceCamera in _faceCameraHs)
                faceCamera.UpdateCamera(cam);
        }
    }
}