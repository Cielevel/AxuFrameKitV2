using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.UI.Kit
{
    public class UIVideoKitHandler : MonoBehaviour { }
    
    public static class UIVideoKit
    {
        private static readonly object Lock = new();
        
        private static UIVideoKitHandler _handler;

        private static UIVideoKitHandler Handler
        {
            get
            {
                if (_handler != null) 
                    return _handler;
                
                lock (Lock)
                {
                    _handler = new GameObject("[UI Video Kit Handler]").AddComponent<UIVideoKitHandler>();
                    Object.DontDestroyOnLoad(_handler.gameObject);
                }

                return _handler;
            }
        }
        
        private static void DelayAction(float time, Action action)
        {
            Handler.StartCoroutine(IEDelayAction());
            return;
            
            IEnumerator IEDelayAction()
            {
                yield return new WaitForSeconds(time);
                action?.Invoke();
            }
        }

        public static RawImage PlayVideoSd(RectTransform parent, VideoClip videoClip, Shader customShader = null) => 
            PlayVideoMt(parent, videoClip, customShader ? new Material(customShader) : null);
        
        /// <summary>
        /// 简单播放视频，依赖于parent的比例，播放时，rawImg的大小与parent完全一致。
        /// 可以处理一般情况的视频播放（常规分辨率与常规时长）；操作不会计入对象池
        /// 返回创建的RawImage组件，以提供一些材质修改的宽裕
        /// </summary>
        public static RawImage PlayVideoMt(RectTransform parent, VideoClip videoClip, Material customMt = null)
        {
            if (!videoClip || !parent)
                return null;

            var go = new GameObject("[UI PlayVideo]");
            go.transform.SetParent(parent);
            
            // rectTransform
            var rtf = go.GetComponent<RectTransform>();
            if (rtf == null)
                rtf = go.AddComponent<RectTransform>();
            rtf.anchorMin = Vector2.zero;
            rtf.anchorMax = Vector2.one;
            rtf.pivot     = Vector2.one * 0.5f;
            rtf.offsetMax = Vector2.zero;
            rtf.offsetMin = Vector2.zero;
            
            rtf.localScale = Vector3.one; // 确保缩放为1

            // videoPlayer
            var videoPlayer = go.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip        = videoClip;
            var videoSize = new Vector2(videoClip.width, videoClip.height);
            
            // RenderTexture
            var rt = new RenderTexture((int)videoSize.x, (int)videoSize.y, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt);
            videoPlayer.targetTexture = rt;

            // rawImage
            var rawImg = go.AddComponent<RawImage>();
            rawImg.texture = rt;
            if (customMt)
                rawImg.material = customMt;
            
            // play
            videoPlayer.Play();

            DelayAction((float)videoClip.length, () =>
            {
                if (go != null) Object.Destroy(go);
            });

            return rawImg;
        }
    }
}