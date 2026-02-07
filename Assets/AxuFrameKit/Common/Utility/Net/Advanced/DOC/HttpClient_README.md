# HttpClient 使用文档

## 概述

`HttpClient` 是一个进阶级的HTTP请求工具类，提供了以下特性：

- ✅ **泛型支持**：自动序列化请求和反序列化响应
- ✅ **可配置序列化器**：支持自定义序列化实现
- ✅ **完整的HTTP方法**：GET、POST、PUT、DELETE、PATCH
- ✅ **详细的结果信息**：包含状态码、耗时、响应头等
- ✅ **错误处理**：完善的异常处理和错误信息
- ✅ **超时控制**：可配置请求超时时间
- ✅ **默认请求头**：支持全局默认请求头
- ✅ **详细日志**：可选的详细请求/响应日志

## 快速开始

### 1. 基本GET请求

```csharp
using AxuFrameKit.Common.Utility.Net;
using UnityEngine;

public class Example : MonoBehaviour
{
    private HttpClient _httpClient;

    void Start()
    {
        _httpClient = new HttpClient();
        GetUserData();
    }

    async void GetUserData()
    {
        var result = await _httpClient.GetAsync<UserData>("https://api.example.com/user/123");
        
        if (result.IsSuccess)
        {
            Debug.Log($"用户名: {result.Data.Name}");
            Debug.Log($"请求耗时: {result.ElapsedMilliseconds}ms");
        }
        else
        {
            Debug.LogError($"请求失败: {result.ErrorMessage}");
        }
    }
}

[System.Serializable]
public class UserData
{
    public string Name;
    public int Age;
    public string Email;
}
```

### 2. POST请求

```csharp
async void CreateUser()
{
    var newUser = new UserData
    {
        Name = "张三",
        Age = 25,
        Email = "zhangsan@example.com"
    };

    var result = await _httpClient.PostAsync<UserData, ApiResponse>(
        "https://api.example.com/users",
        newUser
    );

    if (result.IsSuccess)
    {
        Debug.Log($"创建成功: {result.Data.Message}");
    }
}

[System.Serializable]
public class ApiResponse
{
    public bool Success;
    public string Message;
}
```

### 3. 自定义请求头

```csharp
async void RequestWithHeaders()
{
    var headers = new Dictionary<string, string>
    {
        { "Authorization", "Bearer your-token-here" },
        { "X-Custom-Header", "custom-value" }
    };

    var result = await _httpClient.GetAsync<UserData>(
        "https://api.example.com/user/123",
        headers
    );
}
```

### 4. 设置默认请求头

```csharp
void ConfigureClient()
{
    _httpClient = new HttpClient();
    
    // 添加默认的认证头
    _httpClient.AddDefaultHeader("Authorization", "Bearer your-token");
    
    // 添加API版本头
    _httpClient.AddDefaultHeader("X-API-Version", "v1");
    
    // 设置超时时间（秒）
    _httpClient.Timeout = 60;
    
    // 启用详细日志
    _httpClient.VerboseLogging = true;
}
```

## 高级用法

### 1. 使用自定义序列化器

```csharp
using AxuFrameKit.Common.Utility.Serializer;

// 创建自定义序列化器（例如使用Newtonsoft.Json）
public class NewtonsoftJsonSerializer : ISerializer
{
    public string Serialize(object obj)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
    }

    public T Deserialize<T>(string data)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
    }

    public bool TryDeserialize<T>(string data, out T result)
    {
        try
        {
            result = Deserialize<T>(data);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

// 使用自定义序列化器
var httpClient = new HttpClient(new NewtonsoftJsonSerializer());
```

### 2. 完整的错误处理

```csharp
async void RobustRequest()
{
    var result = await _httpClient.GetAsync<UserData>("https://api.example.com/user/123");

    if (result.IsSuccess)
    {
        // 成功处理
        Debug.Log($"数据: {result.Data}");
        Debug.Log($"状态码: {result.StatusCode}");
        Debug.Log($"耗时: {result.ElapsedMilliseconds}ms");
        
        // 访问响应头
        if (result.ResponseHeaders.ContainsKey("X-Rate-Limit"))
        {
            Debug.Log($"剩余请求次数: {result.ResponseHeaders["X-Rate-Limit"]}");
        }
    }
    else
    {
        // 错误处理
        switch (result.Result)
        {
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError("网络连接错误");
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError($"HTTP错误 {result.StatusCode}: {result.ErrorMessage}");
                break;
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError($"数据处理错误: {result.ErrorMessage}");
                // 可以访问原始响应
                Debug.Log($"原始响应: {result.RawResponse}");
                break;
        }
    }
}
```

### 3. PUT、DELETE、PATCH请求

```csharp
// PUT请求
async void UpdateUser()
{
    var updatedUser = new UserData { Name = "李四", Age = 30 };
    var result = await _httpClient.PutAsync<UserData, ApiResponse>(
        "https://api.example.com/user/123",
        updatedUser
    );
}

// DELETE请求
async void DeleteUser()
{
    var result = await _httpClient.DeleteAsync<ApiResponse>(
        "https://api.example.com/user/123"
    );
}

// PATCH请求
async void PatchUser()
{
    var patch = new { Age = 31 };
    var result = await _httpClient.PatchAsync<object, ApiResponse>(
        "https://api.example.com/user/123",
        patch
    );
}
```

### 4. 获取原始字符串响应

```csharp
async void GetRawResponse()
{
    // 当泛型参数为string时，返回原始响应文本
    var result = await _httpClient.GetAsync<string>("https://api.example.com/data");
    
    if (result.IsSuccess)
    {
        Debug.Log($"原始响应: {result.Data}");
    }
}
```

## HttpResult<T> 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsSuccess` | bool | 请求是否成功 |
| `Data` | T | 反序列化后的数据 |
| `RawResponse` | string | 原始响应文本 |
| `StatusCode` | long | HTTP状态码 |
| `ErrorMessage` | string | 错误信息 |
| `Result` | UnityWebRequest.Result | Unity请求结果类型 |
| `ElapsedMilliseconds` | long | 请求耗时（毫秒） |
| `ResponseHeaders` | Dictionary<string, string> | 响应头信息 |

## 最佳实践

### 1. 单例模式使用

```csharp
public class ApiManager : MonoBehaviour
{
    private static ApiManager _instance;
    public static ApiManager Instance => _instance;

    private HttpClient _httpClient;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeHttpClient();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeHttpClient()
    {
        _httpClient = new HttpClient();
        _httpClient.AddDefaultHeader("Authorization", "Bearer " + GetAuthToken());
        _httpClient.Timeout = 30;
    }

    public async Task<HttpResult<T>> Get<T>(string endpoint)
    {
        return await _httpClient.GetAsync<T>($"https://api.example.com/{endpoint}");
    }

    private string GetAuthToken()
    {
        // 从PlayerPrefs或其他地方获取token
        return PlayerPrefs.GetString("AuthToken", "");
    }
}
```

### 2. 统一错误处理

```csharp
public static class HttpResultExtensions
{
    public static void HandleError<T>(this HttpResult<T> result, string context = "")
    {
        if (result.IsSuccess) return;

        string message = $"[{context}] {result.GetErrorDescription()}";
        
        // 根据状态码进行不同处理
        switch (result.StatusCode)
        {
            case 401:
                Debug.LogError($"{message} - 未授权，需要重新登录");
                // 触发重新登录流程
                break;
            case 404:
                Debug.LogWarning($"{message} - 资源不存在");
                break;
            case 500:
                Debug.LogError($"{message} - 服务器错误");
                break;
            default:
                Debug.LogError(message);
                break;
        }
    }
}

// 使用
var result = await _httpClient.GetAsync<UserData>(url);
result.HandleError("获取用户数据");
```

### 3. 重试机制

```csharp
async Task<HttpResult<T>> RequestWithRetry<T>(
    Func<Task<HttpResult<T>>> requestFunc,
    int maxRetries = 3,
    int delayMs = 1000)
{
    HttpResult<T> result = null;
    
    for (int i = 0; i < maxRetries; i++)
    {
        result = await requestFunc();
        
        if (result.IsSuccess)
            return result;
        
        if (i < maxRetries - 1)
        {
            Debug.LogWarning($"请求失败，{delayMs}ms后重试... ({i + 1}/{maxRetries})");
            await Task.Delay(delayMs);
        }
    }
    
    return result;
}

// 使用
var result = await RequestWithRetry(
    () => _httpClient.GetAsync<UserData>("https://api.example.com/user/123")
);
```

## 注意事项

1. **序列化限制**：Unity的`JsonUtility`不支持字典、多维数组等复杂类型，如需支持请使用自定义序列化器
2. **线程安全**：`HttpClient`实例不是线程安全的，建议每个线程使用独立实例或添加锁
3. **内存管理**：大量请求时注意及时释放资源，避免内存泄漏
4. **UniTask支持**：如果项目中安装了UniTask，会自动使用UniTask而非Task

## 与HttpUtil的对比

| 特性 | HttpUtil | HttpClient |
|------|----------|-----------|
| 泛型支持 | ❌ | ✅ |
| 自动反序列化 | ❌ | ✅ |
| 可配置序列化器 | ❌ | ✅ |
| 详细结果信息 | ❌ | ✅ |
| 超时控制 | ❌ | ✅ |
| 默认请求头 | ❌ | ✅ |
| PUT/DELETE/PATCH | ❌ | ✅ |
| 响应头访问 | ❌ | ✅ |
| 请求耗时统计 | ❌ | ✅ |

## 总结

`HttpClient`提供了更专业、更易用的HTTP请求解决方案，适合需要复杂网络交互的项目使用。
