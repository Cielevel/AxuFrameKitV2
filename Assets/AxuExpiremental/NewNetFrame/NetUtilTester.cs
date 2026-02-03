using UnityEngine;

namespace AxuExpiremental.NewNetFrame
{
    // 1. 定义与后端约定的数据结构
    [System.Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string token;
        public int userId;
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string msgType; // "chat"
        public string content;
    }

    public class NetUtilTester : MonoBehaviour
    {
        private NetClient _client;

        void Start()
        {
            // ------------------------------------
            // 1. 组装 (Builder Pattern)
            // ------------------------------------
            _client = new NetBuilder()
                      .UseSerializer(new UnityJsonNetSerializer()) // 注入序列化
                      .UseHttp(new UnityHttpProvider())            // 注入HTTP实现
                      .UseSocket(new NativeWebSocketProvider())    // 注入Socket实现
                      .Build();

            Debug.Log("Network Client Initialized!");
        
            // 绑定 Socket 监听
            _client.OnSocketMessageRaw += HandleSocketMessage;
            _client.OnSocketConnected  += () => Debug.Log("Socket Connected!");
        }

        // ------------------------------------
        // 2. 测试 HTTP (类型安全 + 过程安全)
        // ------------------------------------
        public async void TestLogin()
        {
            var req = new LoginRequest { username = "Player1", password = "123" };

            string url = "https://api.example.com/login";

            Debug.Log("Sending Login...");

            // 调用 Post<Req, Res>，完全强类型
            NetResult<LoginResponse> result = await _client.Post<LoginRequest, LoginResponse>(url, req);

            // 过程安全：不用 try-catch，直接判断 IsSuccess
            if (result.IsSuccess)
            {
                Debug.Log($"Login Success! Token: {result.Data.token}");
            }
            else
            {
                Debug.LogError($"Login Failed: {result.ErrorMessage} (Code: {result.StatusCode})");
            }
        }

        // ------------------------------------
        // 3. 测试 Socket
        // ------------------------------------
        public async void TestSocket()
        {
            // 连接
            await _client.SocketConnect("wss://echo.websocket.org");

            // 发送 (强类型)
            var chat = new ChatMessage { msgType = "chat", content = "Hello Server!" };
            await _client.SocketSend(chat);
        }

        // 处理 Socket 消息
        private void HandleSocketMessage(string json)
        {
            Debug.Log($"Socket Recv: {json}");
        
            // 可以在这里手动反序列化，或者根据 msgType 字段分发
            // ChatMessage msg = _client.SocketParse<ChatMessage>(json);
        }
    
        private void OnDestroy()
        {
            // 记得清理
            // 实际项目中可以在 NetClient 里加 Dispose 方法
        }
    }
}