using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Auto.AutoCamera
{
    public class FaceCameraManager : SingletonMonoCrDs<FaceCameraManager>
    {
        private readonly HashSet<GameObjectFaceCamera> _faceCameraHs = new(16);
        private readonly List<GameObjectFaceCamera> _faceCameraList = new(16);
        private bool _isListDirty = true;

        public void Register(GameObjectFaceCamera faceCamera)
        {
            if (_faceCameraHs.Add(faceCamera))
                _isListDirty = true;
        }

        public void Unregister(GameObjectFaceCamera faceCamera)
        {
            if (_faceCameraHs.Remove(faceCamera))
                _isListDirty = true;
        }

        /// <summary>
        /// 朝向的相机变更
        /// </summary>
        public void UpdateCamera(Camera cam)
        {
            if (_faceCameraHs.Count == 0) return;

            if (_isListDirty)
            {
                _faceCameraList.Clear();
                _faceCameraList.AddRange(_faceCameraHs);
                _isListDirty = false;
            }

            for (int i = _faceCameraList.Count - 1; i >= 0; i--)
            {
                var faceCamera = _faceCameraList[i];
                if (faceCamera != null)
                    faceCamera.UpdateCamera(cam);
                else
                {
                    _faceCameraHs.Remove(faceCamera);
                    _faceCameraList.RemoveAt(i);
                }
            }
        }
    }
}