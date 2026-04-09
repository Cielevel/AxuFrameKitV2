using System;
using System.Text;
using UnityEngine;
using AxuFrameKit.Common.Utility.Net;
using AxuFrameKit.Common.CustomAttribute;

namespace TestAndSample.Sample_Common.Utility.Net
{
    /// <summary>
    /// SocketClient 使用示例。
    /// 请继承 CustomMonoBehaviour 以在 Inspector 中启用 [InsButton] 按钮。
    /// </summary>
    public class SocketExample : CustomMonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("服务器地址，格式为 host:port (例如 127.0.0.1:8080)")]
        [SerializeField] private string url = "127.0.0.1:8080";
        
        [SerializeField] private bool autoConnectOnStart = false;
        
        [Header("Runtime Status")]
        [SerializeField] private SocketConnectionState connectionState;

        private SocketClient _client;

        private void Start()
        {
            // 初始化 SocketClientOptions
            var options = new SocketClientOptions
            {
                VerboseLogging = true,                      // 启用详细日志，在控制台查看底层输出
                AutoReconnect = true,                        // 断开时自动尝试重连
                HeartbeatEnabled = true,                     // 启用心跳检查（需要服务端配合）
                DispatchEventsOnUnityMainThread = true       // 核心设置：确保回调在 Unity 主线程执行，避免非主线程调用 Unity API 报错
            };

            // 创建 SocketClient 实例
            // 默认 codec 为 LengthPrefixedPacketCodec (4字节长度头 + payload)
            _client = new SocketClient(options);

            // 注册事件
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
            _client.MessageReceived += OnMessageReceived;
            _client.Error += OnError;
            _client.StateChanged += OnStateChanged;

            if (autoConnectOnStart)
            {
                Connect();
            }
        }

        private void OnDestroy()
        {
            // 销毁时务必释放资源
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        [InsButton("Connect To Server")]
        public async void Connect()
        {
            if (_client == null || _client.IsConnected) return;

            try
            {
                var (host, port) = ParseUrl(url);
                Debug.Log($"[SocketExample] Attempting to connect to {host}:{port}...");
                
                // 异步连接
                await _client.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketExample] Connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        [InsButton("Disconnect Server")]
        public async void Disconnect()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
            }
        }

        /// <summary>
        /// 发送文字消息
        /// </summary>
        [InsButton("Send Test Message")]
        public async void SendTestMessage()
        {
            if (_client != null && _client.IsConnected)
            {
                string msg = $"Hello from AxuFrameKit! [{DateTime.Now:HH:mm:ss}]";
                
                // 将文本编码为字节流并发送
                await _client.SendTextAsync(msg);
                
                Debug.Log($"[SocketExample] Sent: {msg}");
            }
            else
            {
                Debug.LogWarning("[SocketExample] Send failed: Socket is not connected.");
            }
        }

        #region Callbacks

        private void OnConnected()
        {
            Debug.Log("<color=green>[SocketExample] Connected to server successfully.</color>");
        }

        private void OnDisconnected(SocketDisconnectedEvent ev)
        {
            Debug.Log($"<color=orange>[SocketExample] Disconnected. Reason: {ev.Reason}, Remote: {ev.IsRemote}, AutoReconnect: {ev.WillReconnect}</color>");
        }

        private void OnMessageReceived(byte[] payload)
        {
            // 将字节流解析为字符串
            string message = Encoding.UTF8.GetString(payload);
            Debug.Log($"<color=cyan>[SocketExample] Received: {message}</color>");
        }

        private void OnError(Exception ex)
        {
            Debug.LogError($"[SocketExample] Socket Error: {ex.Message}");
        }

        private void OnStateChanged(SocketConnectionState state)
        {
            connectionState = state;
            Debug.Log($"[SocketExample] Connection State Changed: {state}");
        }

        #endregion

        /// <summary>
        /// 解析 url 字符串 (host:port)
        /// </summary>
        private (string host, int port) ParseUrl(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return ("127.0.0.1", 8080);
            
            var parts = address.Split(':');
            string h = parts[0];
            int p = 8080;
            
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
            {
                p = parsedPort;
            }
            
            return (h, p);
        }
    }
}
