# Save - 存档管理工具

通用存档管理模块，支持多种存储策略和加密方式，提供简单易用的 API 和高度可扩展的架构。

## 架构设计

```
Save/
├── SaveUtil.cs              # 核心工具类 (策略模式 + Builder)
├── SaveUtilImplementation.cs # 内置实现 (文件存储、AES加密)
├── SimpleSave.cs             # 静态工具类 (快速使用)
└── ReadMe.md
```

## 使用方式选择

| 方式 | 适用场景 | 特点 |
|------|----------|------|
| **SimpleSave** | 快速原型、简单需求 | 静态方法，开箱即用 |
| **SaveUtil** | 生产环境、复杂需求 | 依赖注入，高度可定制 |

---

## SimpleSave - 快速使用

静态工具类，无需实例化，支持三种存储模式。

### 存储模式

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| `PlayerPrefs` | Unity 内置键值存储 | 小型数据，快速读写 |
| `JsonFile` | 本地明文 JSON 文件 | 调试阶段，需要可读性 |
| `EncryptedJsonFile` | 本地加密文件 | 正式发布，防篡改 |

### 基础用法

```csharp
using AxuFrameKit.Common.Utility.Save;

// 存储对象
var playerData = new PlayerData { Level = 10, Gold = 1000 };
SimpleSave.SaveData("player", playerData, SimpleSave.SaveMode.JsonFile);

// 加载对象
var loaded = SimpleSave.LoadData<PlayerData>("player", SimpleSave.SaveMode.JsonFile);

// 存储字符串
SimpleSave.SaveString("token", "abc123", SimpleSave.SaveMode.EncryptedJsonFile);

// 加载字符串
string token = SimpleSave.LoadString("token", SimpleSave.SaveMode.EncryptedJsonFile);

// 删除存档
SimpleSave.DeleteSave("player", SimpleSave.SaveMode.JsonFile);
```

### 注意事项

- 使用 `Unity.JsonUtility` 序列化，不支持 Dictionary 等复杂数据结构
- 加密使用内置 XOR 算法，适合防简单篡改，不适合高安全需求

---

## SaveUtil - 高级使用

基于策略模式的核心工具类，支持缓存、变更监听、延迟保存等高级功能。

### 创建实例

```csharp
using AxuFrameKit.Common.Utility.Save;
using AxuFrameKit.Common.Utility.Serializer;

// 方式1: 使用 Builder
var save = new SaveUtilBuilder()
    .UseSerializer(new UnityJsonSerializer())
    .UseStorage(new LocalFileStorage())
    .UseEncryption(new AesCrypto("MySecretKey123"))
    .UseKeyPrefix("MyGame")
    .Build();

// 方式2: 直接构造
var save = new SaveUtil(
    new UnityJsonSerializer(),
    new LocalFileStorage(),
    new AesCrypto("MySecretKey123"),
    "MyGame"
);
```

### 基础读写

```csharp
// 加载数据（带缓存）
var data = save.Load<PlayerData>("player", new PlayerData());

// 保存数据（立即写入）
save.Save("player", data);

// 检查是否存在
bool exists = save.Exists("player");

// 删除数据
save.Delete("player");
```

### 延迟保存

批量修改时减少 IO 操作：

```csharp
// 仅更新缓存，不立即写入
save.SaveDelayed("audio", audioConfig);
save.SaveDelayed("video", videoConfig);
save.SaveDelayed("input", inputConfig);

// 统一写入存储
save.SaveAll();
```

### 批量操作

```csharp
var configs = new Dictionary<string, GameConfig>
{
    { "config1", config1 },
    { "config2", config2 },
    { "config3", config3 }
};

// 批量设置并统一写入
save.SaveBatch(configs);
```

### 变更监听

```csharp
// 注册回调
save.RegisterCallback<AudioConfig>("audio", (oldValue, newValue) => {
    Debug.Log($"音量变更: {oldValue.MasterVolume} -> {newValue.MasterVolume}");
});

// 取消注册
save.UnregisterCallback("audio", callback);
```

### 缓存管理

```csharp
// 清空缓存（不删除存储数据）
save.ClearCache();

// 清空缓存和回调
save.ClearAll();
```

---

## 内置实现

### 存储实现 (IStorage)

| 类名 | 说明 |
|------|------|
| `LocalFileStorage` | 本地文件存储，可自定义扩展名 |

### 加密实现 (ICrypto)

| 类名 | 算法 | 安全级别 |
|------|------|----------|
| `AesCrypto` | AES (Rijndael) | 高 |
| SimpleSave 内置 | XOR + Base64 | 低 (防简单篡改) |

### 序列化 (ISerializer)

序列化器位于 `AxuFrameKit.Common.Utility.Serializer` 命名空间：

| 类名 | 说明 |
|------|------|
| `UnityJsonSerializer` | Unity JsonUtility |

---

## 自定义扩展

### 自定义存储

```csharp
public class PlayerPrefsStorage : IStorage
{
    public void Write(string key, string data)
    {
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();
    }

    public string Read(string key) => PlayerPrefs.GetString(key);

    public void Delete(string key) => PlayerPrefs.DeleteKey(key);

    public bool Exists(string key) => PlayerPrefs.HasKey(key);
}
```

### 自定义加密

```csharp
public class CustomCrypto : ICrypto
{
    public string Encrypt(string input) { /* ... */ }
    public string Decrypt(string input) { /* ... */ }
}
```

---

## API 参考

### SaveUtil

| 方法 | 说明 |
|------|------|
| `Load<T>(key, default)` | 加载数据（带缓存） |
| `Save<T>(key, data)` | 保存数据（立即写入） |
| `SaveDelayed<T>(key, data)` | 延迟保存（仅更新缓存） |
| `SaveAll()` | 保存所有缓存数据 |
| `SaveBatch<T>(dict)` | 批量保存 |
| `Delete(key)` | 删除数据 |
| `Exists(key)` | 检查是否存在 |
| `ClearCache()` | 清空缓存 |
| `ClearAll()` | 清空缓存和回调 |
| `RegisterCallback<T>(key, callback)` | 注册变更回调 |
| `UnregisterCallback<T>(key, callback)` | 取消注册回调 |

### SaveUtilBuilder

| 方法 | 说明 |
|------|------|
| `UseSerializer(serializer)` | 设置序列化器 |
| `UseStorage(storage)` | 设置存储实现 |
| `UseEncryption(crypto)` | 设置加密器（可选） |
| `UseKeyPrefix(prefix)` | 设置键名前缀（可选） |
| `Build()` | 构建 SaveUtil 实例 |

---

## 命名空间

```csharp
using AxuFrameKit.Common.Utility.Save;
using AxuFrameKit.Common.Utility.Serializer; // 序列化器
```
