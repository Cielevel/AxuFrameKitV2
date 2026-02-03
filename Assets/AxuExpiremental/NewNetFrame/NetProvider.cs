using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AxuExpiremental.NewNetFrame
{
    // ------------------------------------------------
    // 1. JSON 序列化实现 (使用 Unity JsonUtility)
    // ------------------------------------------------
    public class UnityJsonNetSerializer : INetSerializer
    {
        public string Serialize<T>(T data) => JsonUtility.ToJson(data);
        public T Deserialize<T>(string json) => JsonUtility.FromJson<T>(json);
    }

    // ------------------------------------------------
    // 2. HTTP 实现 (使用 UnityWebRequest)
    // ------------------------------------------------
    public class UnityHttpProvider : IHttpProvider
    {
        public async Task<NetResult<string>> GetAsync(string url, Dictionary<string, string> headers = null)
        {
            using var uwr = UnityWebRequest.Get(url);
            if (headers != null)
                foreach (var kv in headers) 
                    uwr.SetRequestHeader(kv.Key, kv.Value);

            // 发送请求并等待
            var operation = uwr.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                return NetResult<string>.Failure(uwr.error, uwr.responseCode);
            }

            return NetResult<string>.Success(uwr.downloadHandler.text, uwr.responseCode);
        }

        public async Task<NetResult<string>> PostAsync(string url, string bodyJson, Dictionary<string, string> headers = null)
        {
            using UnityWebRequest uwr = new UnityWebRequest(url, "POST");

            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(bodyJson);
            
            uwr.uploadHandler   = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (headers != null)
            {
                foreach (var kv in headers) uwr.SetRequestHeader(kv.Key, kv.Value);
            }

            var operation = uwr.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                return NetResult<string>.Failure(uwr.error, uwr.responseCode);
            }

            return NetResult<string>.Success(uwr.downloadHandler.text, uwr.responseCode);
        }
    }

    // ------------------------------------------------
    // 3. Socket 实现 (使用 C# ClientWebSocket)
    // ------------------------------------------------
    public class NativeWebSocketProvider : ISocketProvider
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;

        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public async Task ConnectAsync(string url)
        {
            if (_ws != null) await DisconnectAsync();

            _ws  = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                OnConnected?.Invoke();
            
                // 开始接收循环 (不等待，让它在后台跑)
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_ws != null)
            {
                try
                {
                    // 优雅关闭
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client Closing", CancellationToken.None);
                }
                catch { /* 忽略关闭错误 */ }
                finally 
                {
                    _ws.Dispose();
                    _ws = null;
                    _cts?.Cancel();
                    OnDisconnected?.Invoke();
                }
            }
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected) return;

            var buffer = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4]; // 4KB 缓冲区

            while (IsConnected && !_cts.IsCancellationRequested)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        break;
                    }

                    // 简单的消息处理 (注：这里假设消息很短，一帧能收完。
                    // 生产环境需要处理 result.EndOfMessage 来拼接分包)
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        OnError?.Invoke(ex.Message);
                        await DisconnectAsync(); // 出错则断开
                    }
                    break;
                }
            }
        }
    }
}