# 进阶级HTTP请求系统 - 项目总览

## 📁 文件结构

```
Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/
├── Serializer/                          # 序列化工具
│   ├── ISerializer.cs                   # 序列化器接口
│   └── UnityJsonSerializer.cs           # Unity内置JSON序列化器实现
│
└── Net/                                 # 网络工具
    ├── HttpUtil.cs                      # 原有的简单HTTP工具（保留）
    ├── HttpResult.cs                    # HTTP结果包装类
    ├── HttpClient.cs                    # 专业级HTTP客户端
    ├── HttpClientExample.cs             # 使用示例
    └── HttpClient_README.md             # 详细文档
```

## 🎯 核心组件

### 1. ISerializer（序列化器接口）
**位置**: `Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/Serializer/ISerializer.cs`

定义了序列化和反序列化的标准接口，支持自定义实现。

**主要方法**:
- `string Serialize(object obj)` - 序列化对象
- `T Deserialize<T>(string data)` - 反序列化字符串
- `bool TryDeserialize<T>(string data, out T result)` - 安全反序列化

### 2. UnityJsonSerializer（默认序列化器）
**位置**: `Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/Serializer/UnityJsonSerializer.cs`

使用Unity的`JsonUtility`实现的默认序列化器。

**特性**:
- ✅ 支持美化输出（PrettyPrint）
- ✅ 完善的错误处理和日志
- ✅ 零依赖，开箱即用

### 3. HttpResult<T>（结果包装类）
**位置**: `Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/Net/HttpResult.cs`

封装HTTP请求的完整结果信息。

**核心属性**:
```csharp
public class HttpResult<T>
{
    public bool IsSuccess                    // 是否成功
    public T Data                            // 反序列化的数据
    public string RawResponse                // 原始响应文本
    public long StatusCode                   // HTTP状态码
    public string ErrorMessage               // 错误信息
    public UnityWebRequest.Result Result     // Unity请求结果
    public long ElapsedMilliseconds          // 请求耗时
    public Dictionary<string, string> ResponseHeaders  // 响应头
}
```

### 4. HttpClient（专业级HTTP客户端）
**位置**: `Packages/com.cielevel.axu-frame-kit/Runtime/Common/Utility/Net/HttpClient.cs`

核心HTTP请求工具，提供完整的RESTful API支持。

**支持的HTTP方法**:
- `GetAsync<T>()` - GET请求
- `PostAsync<TRequest, TResponse>()` - POST请求
- `PutAsync<TRequest, TResponse>()` - PUT请求
- `DeleteAsync<T>()` - DELETE请求
- `PatchAsync<TRequest, TResponse>()` - PATCH请求

**核心特性**:
- ✅ 泛型支持，自动序列化/反序列化
- ✅ 可配置的序列化器
- ✅ 默认请求头管理
- ✅ 超时控制
- ✅ 详细日志选项
- ✅ 完整的错误处理
- ✅ 响应头访问
- ✅ 请求耗时统计
- ✅ 同时支持UniTask和Task

## 🚀 快速开始

### 基础用法

```csharp
using AxuFrameKit.Common.Utility.Net;

// 1. 创建客户端
var httpClient = new HttpClient();

// 2. 发送GET请求
var result = await httpClient.GetAsync<UserData>("https://api.example.com/user/123");

// 3. 处理结果
if (result.IsSuccess)
{
    Debug.Log($"用户名: {result.Data.Name}");
}
else
{
    Debug.LogError($"错误: {result.ErrorMessage}");
}
```

### 配置客户端

```csharp
var httpClient = new HttpClient();

// 添加默认请求头
httpClient.AddDefaultHeader("Authorization", "Bearer token");

// 设置超时（秒）
httpClient.Timeout = 60;

// 启用详细日志
httpClient.VerboseLogging = true;
```

### 使用自定义序列化器

```csharp
// 实现ISerializer接口
public class CustomSerializer : ISerializer
{
    public string Serialize(object obj) { /* ... */ }
    public T Deserialize<T>(string data) { /* ... */ }
    public bool TryDeserialize<T>(string data, out T result) { /* ... */ }
}

// 使用自定义序列化器
var httpClient = new HttpClient(new CustomSerializer());
```

## 📊 与HttpUtil对比

| 特性 | HttpUtil | HttpClient |
|------|----------|-----------|
| 返回类型 | `string` | `HttpResult<T>` |
| 自动反序列化 | ❌ | ✅ |
| 泛型支持 | ❌ | ✅ |
| 可配置序列化器 | ❌ | ✅ |
| 错误详情 | 简单 | 详细 |
| 超时控制 | ❌ | ✅ |
| 默认请求头 | ❌ | ✅ |
| PUT/DELETE/PATCH | ❌ | ✅ |
| 响应头访问 | ❌ | ✅ |
| 耗时统计 | ❌ | ✅ |
| 适用场景 | 简单请求 | 专业项目 |

## 💡 使用建议

### 何时使用HttpUtil
- 简单的一次性请求
- 只需要获取原始字符串响应
- 项目规模较小

### 何时使用HttpClient
- 需要类型安全的API调用
- 复杂的RESTful API交互
- 需要详细的错误处理
- 需要统一的请求配置
- 专业级项目开发

## 📖 详细文档

完整的使用文档请查看: `HttpClient_README.md`

示例代码请查看: `HttpClientExample.cs`

## 🔧 扩展性

### 自定义序列化器示例

支持使用第三方序列化库（如Newtonsoft.Json）:

```csharp
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
```

### 添加拦截器/中间件

可以通过继承HttpClient来添加请求/响应拦截器:

```csharp
public class InterceptedHttpClient : HttpClient
{
    protected override async Task<HttpResult<T>> SendRequestAsync<T>(...)
    {
        // 请求前拦截
        OnBeforeRequest(url, method);
        
        var result = await base.SendRequestAsync<T>(...);
        
        // 响应后拦截
        OnAfterResponse(result);
        
        return result;
    }
}
```

## ✅ 已完成功能

- [x] 序列化器接口定义
- [x] Unity内置JSON序列化器实现
- [x] HttpResult<T>结果包装类
- [x] HttpClient核心实现
- [x] GET/POST/PUT/DELETE/PATCH支持
- [x] UniTask和Task双支持
- [x] 完整的错误处理
- [x] 详细文档和示例
- [x] 响应头访问
- [x] 请求耗时统计

## 🎓 学习路径

1. **入门**: 阅读`HttpClient_README.md`的"快速开始"部分
2. **实践**: 运行`HttpClientExample.cs`中的示例
3. **进阶**: 学习自定义序列化器和错误处理
4. **掌握**: 在实际项目中应用最佳实践

## 📝 注意事项

1. Unity的JsonUtility有一些限制（不支持字典、多维数组等），如需支持请使用自定义序列化器
2. 所有异步方法都支持UniTask（如果安装）和标准Task
3. 建议在项目中使用单例模式管理HttpClient实例
4. 生产环境建议关闭VerboseLogging以提高性能

## 🔄 版本信息

- **版本**: 1.0.0
- **创建日期**: 2026-02-07
- **兼容性**: Unity 2019.4+
- **依赖**: 
  - UnityEngine.Networking
  - UniTask (可选)

---

**作者**: AxuFrameKit Team  
**许可**: MIT License
