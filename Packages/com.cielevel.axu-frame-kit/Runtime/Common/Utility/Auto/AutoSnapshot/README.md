# AutoSnapshot - 快照回放系统

## 概述

AutoSnapshot 是一个基于时间的泛型快照记录与回放系统。它允许你记录任意类型的数据及其时间戳，并可以按照时间顺序回放这些数据。

## 核心特性

- **泛型支持**：可以记录任意类型的数据
- **时间缩放模式**：
  - `Scaled`：受 `Time.timeScale` 影响的时间
  - `Unscaled`：不受时间缩放影响的真实时间
- **时间记录模式**：
  - `RecordFromGameStart`：使用游戏启动后的绝对时间
  - `RecordFromInit`：使用记录器初始化后的相对时间
- **元数据自动处理**：框架内部自动序列化/反序列化时间模式等元数据
- **统一序列化接口**：使用 `AxuFrameKit.Common.Utility.Serializer.ISerializer` 接口，与整个框架统一
- **完整的回放控制**：播放、暂停、恢复、停止、重置

## 快速开始

### 1. 记录数据

```csharp
using AxuFrameKit.Common.Utility.Auto.AutoSnapshot;

// 使用 Builder 模式创建记录器
var recorder = SnapshotFactory.CreateRecorder<PlayerPosition>()
    .UseScaledTime()
    .RecordFromInit()
    .WithCapacity(100)
    .Build();

// 或使用快捷方法
var recorder = SnapshotFactory.CreateDefaultRecorder<PlayerPosition>();

// 记录数据
recorder.Record(new PlayerPosition { x = 0, y = 0 });
recorder.Record(new PlayerPosition { x = 1, y = 2 });
```

### 2. 序列化导出

```csharp
using AxuFrameKit.Common.Utility.Serializer;

// 使用框架提供的默认序列化器
var serializer = new UnityJsonSerializer();
string json = recorder.Serialize(serializer);

// 或使用自定义序列化器（实现 ISerializer 接口）
public class NewtonsoftJsonSerializer : ISerializer
{
    public string Serialize(object obj) => JsonConvert.SerializeObject(obj);
    public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data);
    public bool TryDeserialize<T>(string data, out T result)
    {
        try
        {
            result = JsonConvert.DeserializeObject<T>(data);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

var serializer = new NewtonsoftJsonSerializer();
string json = recorder.Serialize(serializer);
```

### 3. 回放数据

```csharp
// 从 JSON 创建播放器（元数据由框架自动处理）
var player = SnapshotPlayer<PlayerPosition>.FromJson(json, serializer);

// 设置回调
player
    .OnSnapshotOutput((data, timestamp) => 
    {
        Debug.Log($"Time: {timestamp}, Position: ({data.x}, {data.y})");
    })
    .OnPlaybackComplete(() => 
    {
        Debug.Log("Playback completed!");
    });

// 开始回放 (需要 MonoBehaviour 来运行协程)
player.Play(this);
```

### 4. 使用接口处理器

```csharp
public class MyPlaybackHandler : ISnapshotPlaybackHandler<PlayerPosition>
{
    public void OnSnapshotOutput(PlayerPosition data, float timestamp)
    {
        // 处理快照输出
    }

    public void OnPlaybackComplete()
    {
        // 回放完成
    }
}

player.UseHandler(new MyPlaybackHandler());
player.Play(this);
```

## 序列化格式

序列化后的 JSON 格式如下（元数据由框架自动处理）：

```json
{
    "Metadata": {
        "ScaleMode": 0,
        "RecordMode": 1,
        "CreatedAtUtc": "2026-01-19T08:00:00.0000000Z",
        "InitTime": 1.5,
        "SnapshotCount": 3
    },
    "Snapshots": [
        { "Timestamp": 0.0, "Data": "{\"X\":0,\"Y\":0,\"Z\":0}" },
        { "Timestamp": 0.5, "Data": "{\"X\":1,\"Y\":2,\"Z\":0}" },
        { "Timestamp": 1.0, "Data": "{\"X\":2,\"Y\":4,\"Z\":0}" }
    ]
}
```

## 时间模式说明

### RecordFromGameStart (绝对时间)

- 记录时使用 `Time.time` 或 `Time.unscaledTime`
- 回放时，如果当前游戏时间已超过某些快照的时间戳，这些快照会被**跳过**
- 适用于：需要与游戏时间同步的场景

### RecordFromInit (相对时间)

- 记录时使用从记录器初始化开始的相对时间
- 回放时从启动回放的时刻开始计时
- 适用于：可重复播放的录像、教程回放等

## API 参考

### ISerializer (统一序列化接口)

框架使用统一的序列化接口 `AxuFrameKit.Common.Utility.Serializer.ISerializer`：

```csharp
public interface ISerializer
{
    string Serialize(object obj);           // 序列化对象
    T Deserialize<T>(string data);          // 反序列化为指定类型
    bool TryDeserialize<T>(string data, out T result);  // 尝试反序列化
}
```

### SnapshotRecorder<T>

| 方法 | 说明 |
|------|------|
| `Record(T data)` | 记录一个快照 |
| `RecordWithTimestamp(T data, float timestamp)` | 使用自定义时间戳记录 |
| `StartRecording()` / `StopRecording()` | 控制记录状态 |
| `Clear()` | 清空所有快照 |
| `GetSnapshots()` | 获取所有快照 (只读) |
| `GetSnapshotsInRange(float start, float end)` | 获取时间范围内的快照 |
| `ExportData()` | 导出为 SnapshotData |
| `Serialize(ISerializer)` | 序列化为 JSON |

### SnapshotPlayer<T>

| 方法 | 说明 |
|------|------|
| `FromJson(string json, ISerializer)` | 从 JSON 创建播放器 |
| `Play(MonoBehaviour host)` | 开始回放 |
| `Pause()` | 暂停回放 |
| `Resume(MonoBehaviour host)` | 恢复回放 |
| `Stop(MonoBehaviour host)` | 停止回放 |
| `Reset()` | 重置到初始状态 |
| `FlushAll()` | 立即输出所有快照 |
| `OnSnapshotOutput(Action)` | 设置快照输出回调 |
| `OnPlaybackComplete(Action)` | 设置完成回调 |

## 注意事项

1. **协程依赖**：回放功能需要 `MonoBehaviour` 来运行协程。

2. **时间精度**：时间戳使用 `float` 类型，长时间运行可能存在精度问题。

3. **数据类型要求**：需要序列化的数据类型必须标记 `[Serializable]` 特性（或序列化器能够处理）。

## 文件结构

```
AutoSnapshot/
├── SnapshotRecorder.cs      # 核心记录器、枚举、接口、数据结构
├── SnapshotPlayer.cs        # 回放器实现
├── SnapshotRecorderBuilder.cs # Builder 模式和工厂类
├── DefaultSerializers.cs    # 序列化器使用说明（实际实现已统一到 Serializer 命名空间）
└── README.md               # 本文档
```

