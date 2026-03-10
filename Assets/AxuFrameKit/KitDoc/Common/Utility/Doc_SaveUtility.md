# 存档模块文档 (Save Utility)

## 1. 简介
本模块位于 `Assets/AxuFrameKit/Common/Utility/Save`，提供了两套不同层级的存档管理方案，旨在覆盖从快速原型开发到正式产品级功能的多种需求。

- **`SimpleSave`**: 一个**静态**工具类，提供开箱即用的 API，适合简单、快速的数据持久化。
- **`SaveUtil`**: 一个**模块化**的存档系统，基于策略模式与依赖注入，支持自定义序列化、存储介质和加密算法，适合高灵活性需求。

---

## 2. 方案一：SimpleSave (快速上手)

### A. 功能说明
1.  **静态调用**：无需实例化对象，直接通过 `SimpleSave.Method(...)` 调用。
2.  **三种存储模式**：
    *   `PlayerPrefs`: 利用 Unity 内置的键值对存储（适合配置项）。
    *   `JsonFile`: 本地生成的明文 JSON 文件（适合调试）。
    *   `EncryptedJsonFile`: 使用简单的异或+Base64加密的文件（适合一般数据保护）。
3.  **数据支持**：原生支持 `string` 及任何 `JsonUtility` 可序列化的泛型对象。

### B. 如何使用

**1. 存储配置或简单数据：**
```csharp
using AxuFrameKit.Common.Utility.Save;

// 存入 PlayerPrefs
SimpleSave.SaveString("BGMVolume", "0.8", SimpleSave.SaveMode.PlayerPrefs);
```

**2. 存储复杂对象（推荐使用加密模式）：**
```csharp
[Serializable]
public class UserData
{
    public string Name;
    public int Coins;
}

var myData = new UserData { Name = "Player1", Coins = 100 };

// 存入加密文件 (路径：Application.persistentDataPath/save01.sav)
SimpleSave.SaveData("save01", myData, SimpleSave.SaveMode.EncryptedJsonFile);
```

**3. 读取数据：**
```csharp
// 如果文件不存在，会自动返回 new UserData()，避免空指针
var data = SimpleSave.LoadData<UserData>("save01", SimpleSave.SaveMode.EncryptedJsonFile);
Debug.Log($"Coins: {data.Coins}");
```

### C. 优缺点
*   **优点**：
    *   **极简 API**：单行代码即可完成读写。
    *   **零配置**：无需预先 Setup，拿来即用。
    *   **方便调试**：可随时切换到明文 JsonFile 模式查看数据。
*   **缺点**：
    *   **灵活性低**：内部强依赖 `JsonUtility`，不支持复杂的字典或多态序列化。
    *   **加密较弱**：仅使用 XOR 混淆，安全性有限。
    *   **扩展困难**：无法在不修改源码的情况下新增存储介质（如云端）。

---

## 3. 方案二：SaveUtil (进阶模块化)

### A. 功能说明
1.  **解耦设计**：核心逻辑不依赖具体的实现，而是通过接口交互。
    *   `ISerializer`: 负责数据 <-> 字符串转换。
    *   `IStorage`: 负责 字符串 <-> 硬盘/网络 存取。
    *   `ICrypto`: 负责 加密 <-> 解密。
2.  **默认实现**：内置了 `UnityJsonSerializer` (JsonUtility), `LocalFileStorage` (本地文件), `AesCrypto` (AES加密)。
3.  **构建器支持**：提供 `SaveUtilBuilder` 用于链式组装。

### B. 如何使用

**1. 初始化与组装：**
```csharp
using AxuFrameKit.Common.Utility.Save;

// 组装一个高安全性的存档器
// 1. 使用 UnityJson 序列化
// 2. 存储在本地文件，后缀名为 .dat
// 3. 使用 AES 对数据进行加密
var saveSystem = new SaveUtilBuilder()
    .UseSerializer(new UnityJsonSerializer())
    .UseStorage(new LocalFileStorage(extension: ".dat"))
    .UseEncryption(new AesCrypto("12345678123456781234567812345678")) // 必须是32字符长度的Key（但内部有长度的修正，所以可以传入任意长度的Key）
    .Build();
```

**2. 存取操作：**
```csharp
// 存储
saveSystem.Save("game_archive", new GameArchiveData { Level = 5 });

// 读取 (支持默认值)
var archive = saveSystem.Load<GameArchiveData>("game_archive");
```

**3. 自定义扩展 (例如改用 Newtonsoft.Json)：**
无需修改框架源码，只需新建脚本实现 `ISerializer` 接口：
```csharp
public class MyJsonSerializer : ISerializer 
{
    public string Serialize<T>(T data) => JsonConvert.SerializeObject(data);
    public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data);
}

// 构建时注入
var mySaver = new SaveUtilBuilder()
    .UseSerializer(new MyJsonSerializer()) // <--- 替换这里
    .UseStorage(new LocalFileStorage())
    .Build();
```

### C. 优缺点
*   **优点**：
    *   **高度可扩展**：任意更换序列化库（如 OdinSerializer）或存储后端（如 SQL、WebAPI）。
    *   **安全性高**：默认集成 AES 加密标准。
    *   **可测试性**：便于进行单元测试与 Mock。
*   **缺点**：
    *   **初始化繁琐**：相比 SimpleSave，需要编写初始化构建代码。
    *   **多文件**：涉及接口和多个类，理解成本稍高。
