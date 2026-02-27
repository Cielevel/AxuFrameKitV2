# Config - 配置框架

## 功能概述

通用的配置管理框架，支持多种存储策略和序列化方式，完全独立可导出。

## 架构设计

```
Config/
├── Interface/              # 接口定义层
│   ├── IConfigStorage     # 存储策略接口
│   ├── IConfigSerializer  # 序列化策略接口
│   └── IConfigCrypto      # 加密策略接口
├── Core/                   # 核心协调层
│   ├── ConfigManager      # 配置管理器
│   └── ConfigManagerBuilder # Builder 构建器
├── Implementation/         # 具体实现层
│   ├── Storage/           # 存储实现
│   ├── Serializer/        # 序列化实现
│   └── Crypto/            # 加密实现
└── Model/                  # 数据模型层
    └── ConfigEntry        # 配置条目
```

## 快速开始

### 1. 使用默认配置（PlayerPrefs + Unity JSON）

```csharp
using AxuFrameKit.Modules.Config.Core;

// 创建配置管理器
var config = ConfigManagerBuilder.CreateDefault("MyGame");

// 读取配置
var audioConfig = config.Get("audio", new AudioConfig());

// 写入配置
config.Set("audio", audioConfig);
```

### 2. 自定义配置组合

```csharp
var config = new ConfigManagerBuilder()
    .UseJsonFile()                    // 使用 JSON 文件存储
    .UseUnityJsonSerializer(true)     // 美化 JSON 输出
    .UseKeyPrefix("MyGame")           // 键名前缀
    .Build();
```

### 3. 加密存储

```csharp
var config = new ConfigManagerBuilder()
    .UsePlayerPrefs()
    .UseUnityJsonSerializer()
    .UseXorCrypto("MySecretKey123")   // 异或加密
    .UseKeyPrefix("MyGame")
    .Build();
```

## 存储策略

| 策略 | 说明 | 适用场景 |
|------|------|----------|
| `PlayerPrefsStorage` | Unity 内置键值存储 | 小型配置，快速读写 |
| `JsonFileStorage` | JSON 文件存储 | 需要可读性，便于调试 |
| `BinaryFileStorage` | 二进制文件存储 | 紧凑存储，稍高性能 |

## 序列化策略

| 策略 | 说明 |
|------|------|
| `UnityJsonSerializer` | Unity JsonUtility，支持 prettyPrint |
| `BinarySerializer` | .NET BinaryFormatter + Base64 |

## 加密策略

| 策略 | 说明 |
|------|------|
| `XorCrypto` | 简单异或加密，防简单篡改 |

## 核心功能

### 基础读写
```csharp
T Get<T>(string key, T defaultValue = default)
void Set<T>(string key, T value)
void Delete(string key)
bool Exists(string key)
void Clear()
```

### 延迟保存
```csharp
// 仅更新缓存，不立即写入存储
config.SetDelayed("audio", audioConfig);

// 统一保存所有缓存的配置
config.SaveAll();
```

### 变更监听
```csharp
// 注册回调
config.RegisterCallback<AudioConfig>("audio", (oldValue, newValue) => {
    Debug.Log($"音量变更: {oldValue.MasterVolume} -> {newValue.MasterVolume}");
});

// 取消注册
config.UnregisterCallback<AudioConfig>("audio", callback);
```

### 批量操作
```csharp
var configs = new Dictionary<string, AudioConfig> {
    { "audio1", config1 },
    { "audio2", config2 }
};
config.SetBatch(configs);
```

## 配置数据模型示例

```csharp
[Serializable]
public class AudioConfig
{
    public float MasterVolume = 1.0f;
    public float BgmVolume = 0.8f;
    public float SfxVolume = 1.0f;
    public bool IsMuted = false;
}
```

## 独立性

本模块完全独立，不依赖任何其他模块代码，可作为独立模块导出使用。

## 命名空间

- `AxuFrameKit.Modules.Config.Interface` - 接口
- `AxuFrameKit.Modules.Config.Core` - 核心类
- `AxuFrameKit.Modules.Config.Implementation.Storage` - 存储实现
- `AxuFrameKit.Modules.Config.Implementation.Serializer` - 序列化实现
- `AxuFrameKit.Modules.Config.Implementation.Crypto` - 加密实现
- `AxuFrameKit.Modules.Config.Model` - 数据模型
