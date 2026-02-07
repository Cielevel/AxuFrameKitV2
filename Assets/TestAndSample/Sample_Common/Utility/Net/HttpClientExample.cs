using System.Collections.Generic;
using AxuFrameKit.Common.Utility.Net;
using UnityEngine;

#if UNI_TASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace AxuFrameKit.Common.Utility.Net.Examples
{
    /// <summary>
    /// HttpClient使用示例
    /// 演示了各种常见的HTTP请求场景
    /// </summary>
    public class HttpClientExample : MonoBehaviour
    {
        private HttpClient _httpClient;

        void Start()
        {
            // 初始化HttpClient
            _httpClient = new HttpClient();

            // 配置默认请求头
            _httpClient.AddDefaultHeader("X-API-Key", "your-api-key");

            // 设置超时时间
            _httpClient.Timeout = 30;

            // 启用详细日志（开发时使用）
            _httpClient.VerboseLogging = true;

            // 运行示例
            RunExamples();
        }

#if UNI_TASK
        async void RunExamples()
        {
            // 示例1：GET请求
            await Example1_GetRequest();
            
            // 示例2：POST请求
            await Example2_PostRequest();
            
            // 示例3：带自定义请求头的请求
            await Example3_CustomHeaders();
            
            // 示例4：错误处理
            await Example4_ErrorHandling();
            
            // 示例5：获取原始字符串响应
            await Example5_RawResponse();
        }

        /// <summary>
        /// 示例1：简单的GET请求
        /// </summary>
        async UniTask Example1_GetRequest()
        {
            Debug.Log("=== 示例1：GET请求 ===");
            
            // 使用JSONPlaceholder测试API
            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/1"
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 成功获取文章");
                Debug.Log($"   标题: {result.Data.title}");
                Debug.Log($"   内容: {result.Data.body}");
                Debug.Log($"   状态码: {result.StatusCode}");
                Debug.Log($"   耗时: {result.ElapsedMilliseconds}ms");
            }
            else
            {
                Debug.LogError($"❌ 请求失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 示例2：POST请求创建资源
        /// </summary>
        async UniTask Example2_PostRequest()
        {
            Debug.Log("=== 示例2：POST请求 ===");

            var newPost = new Post
            {
                title = "测试文章",
                body = "这是一篇测试文章的内容",
                userId = 1
            };

            var result = await _httpClient.PostAsync<Post, Post>(
                "https://jsonplaceholder.typicode.com/posts",
                newPost
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 成功创建文章");
                Debug.Log($"   ID: {result.Data.id}");
                Debug.Log($"   标题: {result.Data.title}");
            }
            else
            {
                Debug.LogError($"❌ 创建失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 示例3：使用自定义请求头
        /// </summary>
        async UniTask Example3_CustomHeaders()
        {
            Debug.Log("=== 示例3：自定义请求头 ===");

            var headers = new Dictionary<string, string>
            {
                { "Custom-Header", "custom-value" },
                { "X-Request-ID", System.Guid.NewGuid().ToString() }
            };

            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/1",
                headers
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 请求成功");
                
                // 访问响应头
                if (result.ResponseHeaders.Count > 0)
                {
                    Debug.Log("响应头:");
                    foreach (var header in result.ResponseHeaders)
                    {
                        Debug.Log($"   {header.Key}: {header.Value}");
                    }
                }
            }
        }

        /// <summary>
        /// 示例4：完整的错误处理
        /// </summary>
        async UniTask Example4_ErrorHandling()
        {
            Debug.Log("=== 示例4：错误处理 ===");

            // 故意请求一个不存在的资源
            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/99999"
            );

            if (!result.IsSuccess)
            {
                Debug.LogWarning($"预期的错误: {result.GetErrorDescription()}");
                
                // 根据不同的错误类型进行处理
                switch (result.Result)
                {
                    case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
                        Debug.Log("处理网络连接错误");
                        break;
                    case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
                        Debug.Log($"处理HTTP协议错误，状态码: {result.StatusCode}");
                        break;
                    case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
                        Debug.Log("处理数据处理错误");
                        break;
                }
            }
        }

        /// <summary>
        /// 示例5：获取原始字符串响应
        /// </summary>
        async UniTask Example5_RawResponse()
        {
            Debug.Log("=== 示例5：原始字符串响应 ===");

            // 当泛型参数为string时，返回原始响应
            var result = await _httpClient.GetAsync<string>(
                "https://jsonplaceholder.typicode.com/posts/1"
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 原始JSON响应:");
                Debug.Log(result.Data);
            }
        }
#else
        async void RunExamples()
        {
            // 示例1：GET请求
            await Example1_GetRequest();

            // 示例2：POST请求
            await Example2_PostRequest();

            // 示例3：带自定义请求头的请求
            await Example3_CustomHeaders();

            // 示例4：错误处理
            await Example4_ErrorHandling();

            // 示例5：获取原始字符串响应
            await Example5_RawResponse();
        }

        /// <summary>
        /// 示例1：简单的GET请求
        /// </summary>
        async Task Example1_GetRequest()
        {
            Debug.Log("=== 示例1：GET请求 ===");

            // 使用JSONPlaceholder测试API
            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/1"
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 成功获取文章");
                Debug.Log($"   标题: {result.Data.title}");
                Debug.Log($"   内容: {result.Data.body}");
                Debug.Log($"   状态码: {result.StatusCode}");
                Debug.Log($"   耗时: {result.ElapsedMilliseconds}ms");
            }
            else
            {
                Debug.LogError($"❌ 请求失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 示例2：POST请求创建资源
        /// </summary>
        async Task Example2_PostRequest()
        {
            Debug.Log("=== 示例2：POST请求 ===");

            var newPost = new Post
            {
                title = "测试文章",
                body = "这是一篇测试文章的内容",
                userId = 1
            };

            var result = await _httpClient.PostAsync<Post, Post>(
                "https://jsonplaceholder.typicode.com/posts",
                newPost
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 成功创建文章");
                Debug.Log($"   ID: {result.Data.id}");
                Debug.Log($"   标题: {result.Data.title}");
            }
            else
            {
                Debug.LogError($"❌ 创建失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 示例3：使用自定义请求头
        /// </summary>
        async Task Example3_CustomHeaders()
        {
            Debug.Log("=== 示例3：自定义请求头 ===");

            var headers = new Dictionary<string, string>
            {
                { "Custom-Header", "custom-value" },
                { "X-Request-ID", System.Guid.NewGuid().ToString() }
            };

            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/1",
                headers
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 请求成功");

                // 访问响应头
                if (result.ResponseHeaders.Count > 0)
                {
                    Debug.Log("响应头:");
                    foreach (var header in result.ResponseHeaders)
                    {
                        Debug.Log($"   {header.Key}: {header.Value}");
                    }
                }
            }
        }

        /// <summary>
        /// 示例4：完整的错误处理
        /// </summary>
        async Task Example4_ErrorHandling()
        {
            Debug.Log("=== 示例4：错误处理 ===");

            // 故意请求一个不存在的资源
            var result = await _httpClient.GetAsync<Post>(
                "https://jsonplaceholder.typicode.com/posts/99999"
            );

            if (!result.IsSuccess)
            {
                Debug.LogWarning($"预期的错误: {result.GetErrorDescription()}");

                // 根据不同的错误类型进行处理
                switch (result.Result)
                {
                    case UnityEngine.Networking.UnityWebRequest.Result.ConnectionError:
                        Debug.Log("处理网络连接错误");
                        break;
                    case UnityEngine.Networking.UnityWebRequest.Result.ProtocolError:
                        Debug.Log($"处理HTTP协议错误，状态码: {result.StatusCode}");
                        break;
                    case UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError:
                        Debug.Log("处理数据处理错误");
                        break;
                }
            }
        }

        /// <summary>
        /// 示例5：获取原始字符串响应
        /// </summary>
        async Task Example5_RawResponse()
        {
            Debug.Log("=== 示例5：原始字符串响应 ===");

            // 当泛型参数为string时，返回原始响应
            var result = await _httpClient.GetAsync<string>(
                "https://jsonplaceholder.typicode.com/posts/1"
            );

            if (result.IsSuccess)
            {
                Debug.Log($"✅ 原始JSON响应:");
                Debug.Log(result.Data);
            }
        }
#endif

        /// <summary>
        /// 文章数据模型（用于JSONPlaceholder API）
        /// </summary>
        [System.Serializable]
        public class Post
        {
            public int userId;
            public int id;
            public string title;
            public string body;
        }
    }
}
