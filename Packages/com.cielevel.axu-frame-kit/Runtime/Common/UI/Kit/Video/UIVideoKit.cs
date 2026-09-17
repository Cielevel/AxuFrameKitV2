using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.UI.Kit.Video
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

        // 每次播放的清理动作登记表：VideoPlayer -> 清理回调（到期或 Stop 时执行，并自移除保证幂等）
        private static readonly Dictionary<VideoPlayer, Action> _teardowns = new();

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
        
        private static Coroutine DelayAction(float time, Action action)
        {
            return Handler.StartCoroutine(IEDelayAction());

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
            // 修复：totalDuration 一直按 PlaybackSpeed 计算，但播放器从未应用该速度；此处保持口径一致
            // （速度必须为正，非正值回退为 1 并告警，避免 totalDuration 计算除零）
            if (config.PlaybackSpeed > 0f)
                videoPlayer.playbackSpeed = config.PlaybackSpeed;
            else
                Debug.LogWarning($"UIVideoKit: PlaybackSpeed 非法（{config.PlaybackSpeed}），按 1 处理。");
            var videoSize = new Vector2(config.VideoClip.width, config.VideoClip.height);

            // RenderTexture
            var rt = new RenderTexture((int)videoSize.x, (int)videoSize.y, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt);
            videoPlayer.targetTexture = rt;

            // rawImage
            var rawImg = go.AddComponent<RawImage>();
            rawImg.texture = rt;
            if (config.Material != null)
                rawImg.material = config.Material;
            // 运行时用 Shader new 出来的材质需要自己销毁；config.Material 由外部管理，不归本工具释放
            Material runtimeMaterial = null;
            if (config.Shader != null)
            {
                runtimeMaterial = new Material(config.Shader);
                rawImg.material = runtimeMaterial;
            }

            // play
            videoPlayer.Play();

            if (0 < config.FadeInDuration)
            {
                rawImg.canvasRenderer.SetAlpha(0f);
                rawImg.CrossFadeAlpha(1f, config.FadeInDuration, false);
            }

            float totalDuration = (float)config.VideoClip.length / videoPlayer.playbackSpeed;

            Coroutine fadeOutCoroutine = null;
            Action teardown = () =>
            {
                // 已清理过则直接返回（到期回调与外部 Stop 可能重复触发），保证幂等
                if (_teardowns.Remove(videoPlayer) == false)
                    return;

                // 清理尚未执行的淡出回调协程（到期路径执行时它早已结束，重复 Stop 无害）
                if (fadeOutCoroutine != null && _handler != null)
                {
                    _handler.StopCoroutine(fadeOutCoroutine);
                    fadeOutCoroutine = null;
                }

                if (videoPlayer != null)
                {
                    videoPlayer.Stop();
                    // 解除对 RT 的持有后再释放，避免播放器继续向已释放的 RT 写入
                    videoPlayer.targetTexture = null;
                }

                if (rawImg != null)
                    rawImg.texture = null;

                // 修复泄漏：每次播放 new 的 RenderTexture 必须显式释放并销毁
                if (rt != null)
                {
                    rt.Release();
                    Object.Destroy(rt);
                }

                // 只销毁运行时创建的材质，配置传入的 Material 不动
                if (runtimeMaterial != null)
                    Object.Destroy(runtimeMaterial);

                if (go != null)
                    Object.Destroy(go);
            };

            if (0 < config.FadeOutDuration)
            {
                config.FadeOutDuration = Mathf.Min(totalDuration, config.FadeOutDuration);

                fadeOutCoroutine = DelayAction(totalDuration - config.FadeOutDuration, () =>
                {
                    if (rawImg == null) return; // 目标已被销毁（如外部 Stop），直接跳过
                    rawImg.canvasRenderer.SetAlpha(1f);
                    rawImg.CrossFadeAlpha(0f, config.FadeOutDuration, false);
                });
            }

            _teardowns[videoPlayer] = teardown;
            DelayAction(totalDuration, teardown);

            return (rawImg, videoPlayer);
        }

        /// <summary>
        /// 停止一次 PlayVideo 播放：停止播放器、清理延迟回调协程、释放 RT 与运行时材质（幂等，可重复调用）
        /// </summary>
        public static void Stop(VideoPlayer videoPlayer)
        {
            if (videoPlayer == null)
                return;

            if (_teardowns.TryGetValue(videoPlayer, out var teardown))
            {
                teardown();
                return;
            }

            // 兜底：非本工具创建的播放器，只做停止
            videoPlayer.Stop();
        }
    }
}