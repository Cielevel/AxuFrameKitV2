using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#if UNI_TASK
using Cysharp.Threading.Tasks;
#endif

namespace AxuFrameKit.Common.Utility.Net
{
    public class Downloader
    {
        // 概念与AddressLoader一致，提供可直接调用的异步方法，也提供返回Task的异步任务，方便AsyncHandler集中处理

        private static readonly Vector2 _pivot = new Vector2(0.5f, 0.5f);

        /// <summary> 请求超时（秒），防止挂起的下载永久占用调用方 </summary>
        private const int DefaultTimeoutSeconds = 30;

#if UNI_TASK
        private static async Task<Texture2D> DownloadTextureTask(string url, CancellationTokenSource tokenSource = null)
        {
            using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
            uwr.timeout = DefaultTimeoutSeconds;
            var operation = uwr.SendWebRequest();

            if (tokenSource != null)
                await operation.WithCancellation(tokenSource.Token);
            else
                await operation;
            
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                return texture;
            }
            if (uwr.result == UnityWebRequest.Result.ConnectionError
                        || uwr.result == UnityWebRequest.Result.DataProcessingError
                        || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("下载头像失败，msg：" + uwr.error + "，头像地址为：" + url);
                return null;
            }
            return null;
        }

        public static async Task<Sprite> DownloadSpriteAsync(string url, CancellationTokenSource tokenSource = null)
        {
            var texture = await DownloadTextureTask(url, tokenSource);
            return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), _pivot);
        }
#endif

        public static async Task<Sprite> DownloadSpriteAsync(string url, float pixelPerUnit)
        {
            var texture = await DownloadTextureTask(url);
            return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), _pivot, pixelPerUnit);
        }

        private static async Task<Texture2D> DownloadTextureTask(string url)
        {
            using var uwr = UnityWebRequestTexture.GetTexture(url);
            uwr.timeout = DefaultTimeoutSeconds;

            var operation = uwr.SendWebRequest();

            await operation;

            switch (uwr.result)
            {
                case UnityWebRequest.Result.Success:
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                        return texture;
                    }
                case UnityWebRequest.Result.ConnectionError
                    or UnityWebRequest.Result.DataProcessingError
                    or UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning("下载头像失败，msg：" + uwr.error + "，头像地址为：" + url);
                    break;
            }

            return null;
        }

        public IEnumerator DownloadSprite(string url, Action<Sprite> callback, float pixelPerUnit = 100f) // 始终提供一个协程版本
        {
            using var uwr = UnityWebRequestTexture.GetTexture(url);
            uwr.timeout = DefaultTimeoutSeconds;
            yield return uwr.SendWebRequest();

            switch (uwr.result)
            {
                case UnityWebRequest.Result.Success:
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), _pivot, pixelPerUnit);
                        callback(sprite);
                        break;
                    }
                case UnityWebRequest.Result.ConnectionError
                    or UnityWebRequest.Result.DataProcessingError
                    or UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning("下载头像失败，msg：" + uwr.error + "，头像地址为：" + url);
                    break;
            }
        }
    }
}