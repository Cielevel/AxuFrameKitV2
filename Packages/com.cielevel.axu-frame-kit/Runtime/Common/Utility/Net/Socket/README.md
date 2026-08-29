# Socket Utility

`AxuFrameKit.Common.Utility.Net.Socket` 提供一套可直接用于生产环境的 TCP Socket 客户端能力：

- 连接/断开管理
- 长度前缀分包（默认 4-byte Big Endian）
- 异步收发
- 连接状态事件
- 心跳检测
- 自动重连（指数退避）
- 主线程事件回调（在 Unity 主线程创建实例时）
- Unity 主线程分发兜底（可配置）
- 可配置最大重连次数与重连失败事件

## 主要类型

- `SocketClient`
- `SocketClientOptions`
- `ISocketPacketCodec`
- `LengthPrefixedPacketCodec`
- `SocketConnectionState`
- `SocketDisconnectReason`

## 快速使用

```csharp
using System;
using System.Text;
using System.Threading.Tasks;
using AxuFrameKit.Common.Utility.Net;

public class NetEntry
{
    private SocketClient _client;

    public async Task StartAsync()
    {
        var options = new SocketClientOptions
        {
            AutoReconnect = true,
            HeartbeatEnabled = true,
            HeartbeatIntervalMs = 5000,
            HeartbeatTimeoutMs = 15000,
            HeartbeatRequestPayload = Encoding.UTF8.GetBytes("ping"),
            HeartbeatResponsePayload = Encoding.UTF8.GetBytes("pong"),
            MaxReconnectAttempts = 10, // 0 表示无限重连
            DispatchEventsOnUnityMainThread = true,
            VerboseLogging = true
        };

        _client = new SocketClient(options);

        _client.Connected += () => UnityEngine.Debug.Log("Socket connected");
        _client.MessageReceived += bytes => UnityEngine.Debug.Log($"Recv: {Encoding.UTF8.GetString(bytes)}");
        _client.Disconnected += evt => UnityEngine.Debug.LogWarning($"Disconnected: {evt.Reason}, Reconnect={evt.WillReconnect}");
        _client.Error += ex => UnityEngine.Debug.LogError(ex);

        await _client.ConnectAsync("127.0.0.1", 9000);
        await _client.SendTextAsync("hello");
    }

    public async Task StopAsync()
    {
        if (_client == null) return;
        await _client.DisconnectAsync();
        _client.Dispose();
        _client = null;
    }
}
```

## 自定义协议

如果服务端不是长度前缀协议，可实现 `ISocketPacketCodec` 并注入到 `SocketClient`：

```csharp
var codec = new MyCodec();
var client = new SocketClient(options, codec);
```

## 注意事项

- `SocketClient` 仅做传输层，业务协议解析建议放在上层（如 ProtoBuf/JSON/MessagePack）。
- `MessageReceived`/`Connected`/`Disconnected`/`Error` 会优先分发到创建实例时捕获的 `SynchronizationContext`，若为空且 `DispatchEventsOnUnityMainThread=true`，会回退到内部 Unity 主线程分发器。
- `MaxReconnectAttempts > 0` 时，超过上限后会触发 `Disconnected`，`Reason = ReconnectFailed`。
- 在 Unity 中推荐在主线程创建实例，并在 `OnDestroy` 中调用 `Dispose()`。
