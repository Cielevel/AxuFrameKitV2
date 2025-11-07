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

    public class UIVideoConfig
    {
        public RectTransform Parent { get; set; }
        
        public VideoClip VideoClip { get; set; }

        public Material Material { get; set; } = null;

        public Shader Shader { get; set; } = null;

        public float PlaybackSpeed { get; set; } = 1;

        public float FadeInDuration { get; set; } = 0f;
        
        public float FadeOutDuration { get; set; } = 0f;
    }
    
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

        /// <summary>
        /// 简单播放视频，依赖于parent的比例，播放时，rawImg的大小与parent完全一致。
        /// 可以处理一般情况的视频播放（常规分辨率与常规时长）；操作不会计入对象池
        /// 返回创建的RawImage组件，以提供一些材质修改的宽裕
        /// </summary>
        public static (RawImage img, VideoPlayer videoPlayer) PlayVideo(UIVideoConfig config)
        {
            if (config.Parent == null || config.VideoClip == null)
            {
                Debug.Log($"UIVideoKit: Parent->{config.Parent}, VideoClip->{config.VideoClip}");
                return (null, null);
            }

            var go = new GameObject("[UI PlayVideo]");
            go.transform.SetParent(config.Parent);

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
            videoPlayer.clip        = config.VideoClip;
            var videoSize = new Vector2(config.VideoClip.width, config.VideoClip.height);

            // RenderTexture
            var rt = new RenderTexture((int)videoSize.x, (int)videoSize.y, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt);
            videoPlayer.targetTexture = rt;

            // rawImage
            var rawImg = go.AddComponent<RawImage>();
            rawImg.texture = rt;
            if (config.Material != null)
                rawImg.material = config.Material;
            if (config.Shader != null)
                rawImg.material = new Material(config.Shader);

            // play
            videoPlayer.Play();

            if (0 < config.FadeInDuration)
            {
                rawImg.canvasRenderer.SetAlpha(0f);
                rawImg.CrossFadeAlpha(1f, config.FadeInDuration, false);
            }

            float totalDuration = (float)config.VideoClip.length / config.PlaybackSpeed;
            
            if (0 < config.FadeOutDuration)
            {
                config.FadeOutDuration = Mathf.Min(totalDuration, config.FadeOutDuration);
                
                DelayAction(totalDuration - config.FadeOutDuration, () =>
                {
                    rawImg.canvasRenderer.SetAlpha(1f);
                    rawImg.CrossFadeAlpha(0f, config.FadeOutDuration, false);
                });
            }
            
            DelayAction(totalDuration, () =>
            {
                if (go != null) Object.Destroy(go);
            });

            return (rawImg, videoPlayer);
        }
    }
}