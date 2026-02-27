using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AxuExperimental.NewNetFrame
{
    // ==========================================
    // 1. 基础数据结构 (Data Structures)
    // ==========================================

    /// <summary>
    /// 统一的网络结果包裹类 (过程安全的核心)
    /// 无论成功还是失败，都返回这个对象，而不是抛出异常
    /// </summary>
    public class NetResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public long StatusCode { get; private set; } // HTTP 状态码或自定义错误码

        public static NetResult<T> Success(T data, long code = 200)
        {
            return new NetResult<T> { IsSuccess = true, Data = data, StatusCode = code };
        }

        public static NetResult<T> Failure(string error, long code = -1)
        {
            return new NetResult<T> { IsSuccess = false, ErrorMessage = error, StatusCode = code };
        }
    }

    // ==========================================
    // 2. 接口定义 (Abstractions)
    // ==========================================

    /// <summary>
    /// 序列化策略：负责将网络包转为对象，或反之
    /// </summary>
    public interface INetSerializer
    {
        string Serialize<T>(T data);
        T Deserialize<T>(string json);
    }

    /// <summary>
    /// HTTP/短连接 策略接口
    /// </summary>
    public interface IHttpProvider
    {
        // 定义通用的 Get/Post 行为，返回原始字符串，类型转换交给 Core 层
        Task<NetResult<string>> GetAsync(string url, Dictionary<string, string> headers = null);
        Task<NetResult<string>> PostAsync(string url, string bodyJson, Dictionary<string, string> headers = null);
    }

    /// <summary>
    /// Socket/长连接 策略接口
    /// </summary>
    public interface ISocketProvider
    {
        // 连接状态
        bool IsConnected { get; }

        // 生命周期
        Task ConnectAsync(string url);
        Task DisconnectAsync();

        // 发送与接收
        Task SendAsync(string message); // 底层只管发字符串或字节

        // 事件回调
        event Action<string> OnMessageReceived;
        event Action<string> OnError;
        event Action OnConnected;
        event Action OnDisconnected;
    }

    // ==========================================
    // 3. 核心客户端 (The Core Client)
    // ==========================================

    /// <summary>
    /// NetClient：核心协调者
    /// 泛型方法提供 "类型安全"，try-catch 内部处理提供 "过程安全"
    /// </summary>
    public class NetClient
    {
        private readonly INetSerializer _serializer;
        private readonly IHttpProvider _http;
        private readonly ISocketProvider _socket;

        // 构造函数注入
        public NetClient(INetSerializer serializer, IHttpProvider http, ISocketProvider socket = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _http       = http ?? throw new ArgumentNullException(nameof(http));
            _socket     = socket; // Socket是可选的
        }

        #region HTTP / 短连接功能

        /// <summary>
        /// 类型安全的 GET 请求
        /// </summary>
        public async Task<NetResult<TResponse>> Get<TResponse>(string url)
        {
            // 1. 调用底层 provider
            var rawResult = await _http.GetAsync(url);

            // 2. 处理错误
            if (!rawResult.IsSuccess)
                return NetResult<TResponse>.Failure(rawResult.ErrorMessage, rawResult.StatusCode);

            // 3. 尝试反序列化 (类型安全转换)
            try
            {
                TResponse data = _serializer.Deserialize<TResponse>(rawResult.Data);
                return NetResult<TResponse>.Success(data, rawResult.StatusCode);
            }
            catch (Exception ex)
            {
                return NetResult<TResponse>.Failure($"Deserialize Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 类型安全的 POST 请求
        /// </summary>
        public async Task<NetResult<TResponse>> Post<TRequest, TResponse>(string url, TRequest body)
        {
            string jsonBody;
            try
            {
                jsonBody = _serializer.Serialize(body);
            }
            catch (Exception ex)
            {
                return NetResult<TResponse>.Failure($"Serialize Request Error: {ex.Message}");
            }

            var rawResult = await _http.PostAsync(url, jsonBody);

            if (!rawResult.IsSuccess)
                return NetResult<TResponse>.Failure(rawResult.ErrorMessage, rawResult.StatusCode);

            try
            {
                TResponse data = _serializer.Deserialize<TResponse>(rawResult.Data);
                return NetResult<TResponse>.Success(data, rawResult.StatusCode);
            }
            catch (Exception ex)
            {
                return NetResult<TResponse>.Failure($"Deserialize Response Error: {ex.Message}");
            }
        }

        #endregion

        #region Socket / 长连接功能

        // 转发 Socket 事件
        public event Action<string> OnSocketMessageRaw
        {
            add
            {
                if (_socket != null) _socket.OnMessageReceived += value;
            }
            remove
            {
                if (_socket != null) _socket.OnMessageReceived -= value;
            }
        }

        public event Action OnSocketConnected
        {
            add
            {
                if (_socket != null) _socket.OnConnected += value;
            }
            remove
            {
                if (_socket != null) _socket.OnConnected -= value;
            }
        }

        public async Task SocketConnect(string url)
        {
            if (_socket == null) throw new InvalidOperationException("Socket provider not configured!");
            await _socket.ConnectAsync(url);
        }

        /// <summary>
        /// 类型安全的 Socket 发送
        /// </summary>
        public async Task SocketSend<T>(T packet)
        {
            if (_socket == null) return;
            string json = _serializer.Serialize(packet);
            await _socket.SendAsync(json);
        }

        // 辅助方法：解析收到的消息
        public T SocketParse<T>(string rawJson)
        {
            return _serializer.Deserialize<T>(rawJson);
        }

        #endregion
    }

    // ==========================================
    // 4. 构建器 (Builder)
    // ==========================================

    public class NetBuilder
    {
        private INetSerializer _serializer;
        private IHttpProvider _http;
        private ISocketProvider _socket;

        public NetBuilder UseSerializer(INetSerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        public NetBuilder UseHttp(IHttpProvider httpProvider)
        {
            _http = httpProvider;
            return this;
        }

        public NetBuilder UseSocket(ISocketProvider socketProvider)
        {
            _socket = socketProvider;
            return this;
        }

        public NetClient Build()
        {
            if (_serializer == null) throw new Exception("Serializer is missing!");
            if (_http == null) throw new Exception("Http Provider is missing!");

            return new NetClient(_serializer, _http, _socket);
        }
    }
}