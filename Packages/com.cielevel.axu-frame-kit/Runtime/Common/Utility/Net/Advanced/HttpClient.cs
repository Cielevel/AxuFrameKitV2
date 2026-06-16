using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AxuFrameKit.Common.Utility.Serializer;
using UnityEngine;
using UnityEngine.Networking;

#if UNI_TASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace AxuFrameKit.Common.Utility.Net
{
    /// <summary>
    /// 专业级HTTP客户端，支持泛型请求和可配置的序列化器
    /// </summary>
    public class HttpClient
    {
        private readonly ISerializer _serializer;
        private readonly Dictionary<string, string> _defaultHeaders;
        private int _timeout = 30; // 默认超时30秒

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => _timeout = Mathf.Max(1, value);
        }

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool VerboseLogging { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serializer">序列化器，默认使用Unity的JSON序列化器</param>
        public HttpClient(ISerializer serializer = null)
        {
            _serializer = serializer ?? new UnityJsonSerializer();
            _defaultHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// 添加默认请求头
        /// </summary>
        public void AddDefaultHeader(string key, string value)
        {
            _defaultHeaders[key] = value;
        }

        /// <summary>
        /// 移除默认请求头
        /// </summary>
        public void RemoveDefaultHeader(string key)
        {
            _defaultHeaders.Remove(key);
        }

        /// <summary>
        /// 清空所有默认请求头
        /// </summary>
        public void ClearDefaultHeaders()
        {
            _defaultHeaders.Clear();
        }

#if UNI_TASK
        /// <summary>
        /// 发送GET请求并返回泛型结果
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含反序列化数据的结果</returns>
        public async UniTask<HttpResult<T>> GetAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers);
        }

        /// <summary>
        /// 发送POST请求并返回泛型结果
        /// </summary>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <typeparam name="TResponse">响应数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="requestData">请求数据对象</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含反序列化数据的结果</returns>
        public async UniTask<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(
            string url, 
            TRequest requestData, 
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers);
        }

        /// <summary>
        /// 发送POST请求（仅发送，不期望响应数据）
        /// </summary>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="requestData">请求数据对象</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含状态信息的结果</returns>
        public async UniTask<HttpResult<string>> PostAsync<TRequest>(
            string url, 
            TRequest requestData, 
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<string>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers);
        }

        /// <summary>
        /// 发送PUT请求并返回泛型结果
        /// </summary>
        public async UniTask<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(
            string url, 
            TRequest requestData, 
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers);
        }

        /// <summary>
        /// 发送DELETE请求并返回泛型结果
        /// </summary>
        public async UniTask<HttpResult<T>> DeleteAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers);
        }

        /// <summary>
        /// 发送PATCH请求并返回泛型结果
        /// </summary>
        public async UniTask<HttpResult<TResponse>> PatchAsync<TRequest, TResponse>(
            string url, 
            TRequest requestData, 
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, "PATCH", jsonData, headers);
        }

        /// <summary>
        /// 核心请求方法
        /// </summary>
        private async UniTask<HttpResult<T>> SendRequestAsync<T>(
            string url, 
            string method, 
            string jsonData = null, 
            Dictionary<string, string> customHeaders = null)
        {
            var stopwatch = Stopwatch.StartNew();

            if (string.IsNullOrEmpty(url))
            {
                return HttpResult<T>.Failure("URL不能为空", 0, UnityWebRequest.Result.DataProcessingError);
            }

            using UnityWebRequest request = new UnityWebRequest(url, method);
            
            // 设置下载处理器
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置上传数据
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            // 设置超时
            request.timeout = _timeout;

            // 应用默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            // 应用自定义请求头（会覆盖默认请求头）
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            if (VerboseLogging)
            {
                UnityEngine.Debug.Log($"[HttpClient] {method} {url}\nBody: {jsonData}");
            }

            // 发送请求
            await request.SendWebRequest();

            stopwatch.Stop();
            long elapsedMs = stopwatch.ElapsedMilliseconds;

            // 获取响应头
            var responseHeaders = new Dictionary<string, string>();
            if (request.GetResponseHeaders() != null)
            {
                foreach (var header in request.GetResponseHeaders())
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            // 处理错误
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"{request.error}";
                
                if (VerboseLogging)
                {
                    UnityEngine.Debug.LogWarning($"[HttpClient] 请求失败 - {method} {url}\n" +
                                                $"Result: {request.result}\n" +
                                                $"Code: {request.responseCode}\n" +
                                                $"Error: {errorMsg}\n" +
                                                $"Time: {elapsedMs}ms");
                }

                var failureResult = HttpResult<T>.Failure(errorMsg, request.responseCode, request.result, elapsedMs);
                failureResult.ResponseHeaders = responseHeaders;
                return failureResult;
            }

            // 获取响应文本
            string responseText = request.downloadHandler.text;

            if (VerboseLogging)
            {
                UnityEngine.Debug.Log($"[HttpClient] 请求成功 - {method} {url}\n" +
                                     $"Code: {request.responseCode}\n" +
                                     $"Response: {responseText}\n" +
                                     $"Time: {elapsedMs}ms");
            }

            // 如果T是string类型，直接返回原始文本
            if (typeof(T) == typeof(string))
            {
                var stringResult = HttpResult<T>.Success(
                    (T)(object)responseText, 
                    responseText, 
                    request.responseCode, 
                    elapsedMs
                );
                stringResult.ResponseHeaders = responseHeaders;
                return stringResult;
            }

            // 反序列化响应
            if (string.IsNullOrEmpty(responseText))
            {
                var emptyResult = HttpResult<T>.Success(default, responseText, request.responseCode, elapsedMs);
                emptyResult.ResponseHeaders = responseHeaders;
                return emptyResult;
            }

            try
            {
                T data = _serializer.Deserialize<T>(responseText);
                var successResult = HttpResult<T>.Success(data, responseText, request.responseCode, elapsedMs);
                successResult.ResponseHeaders = responseHeaders;
                return successResult;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[HttpClient] 反序列化失败: {ex.Message}\n" +
                                          $"类型: {typeof(T).Name}\n" +
                                          $"响应: {responseText}");
                
                var deserializeFailureResult = HttpResult<T>.DeserializationFailure(
                    responseText, 
                    request.responseCode, 
                    ex.Message, 
                    elapsedMs
                );
                deserializeFailureResult.ResponseHeaders = responseHeaders;
                return deserializeFailureResult;
            }
        }
#else
        /// <summary>
        /// 发送GET请求并返回泛型结果
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含反序列化数据的结果</returns>
        public async Task<HttpResult<T>> GetAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbGET, null, headers);
        }

        /// <summary>
        /// 发送POST请求并返回泛型结果
        /// </summary>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <typeparam name="TResponse">响应数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="requestData">请求数据对象</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含反序列化数据的结果</returns>
        public async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(
            string url,
            TRequest requestData,
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers);
        }

        /// <summary>
        /// 发送POST请求（仅发送，不期望响应数据）
        /// </summary>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <param name="url">请求URL</param>
        /// <param name="requestData">请求数据对象</param>
        /// <param name="headers">自定义请求头</param>
        /// <returns>包含状态信息的结果</returns>
        public async Task<HttpResult<string>> PostAsync<TRequest>(
            string url,
            TRequest requestData,
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<string>(url, UnityWebRequest.kHttpVerbPOST, jsonData, headers);
        }

        /// <summary>
        /// 发送PUT请求并返回泛型结果
        /// </summary>
        public async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(
            string url,
            TRequest requestData,
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, UnityWebRequest.kHttpVerbPUT, jsonData, headers);
        }

        /// <summary>
        /// 发送DELETE请求并返回泛型结果
        /// </summary>
        public async Task<HttpResult<T>> DeleteAsync<T>(string url, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync<T>(url, UnityWebRequest.kHttpVerbDELETE, null, headers);
        }

        /// <summary>
        /// 发送PATCH请求并返回泛型结果
        /// </summary>
        public async Task<HttpResult<TResponse>> PatchAsync<TRequest, TResponse>(
            string url,
            TRequest requestData,
            Dictionary<string, string> headers = null)
        {
            string jsonData = _serializer.Serialize(requestData);
            return await SendRequestAsync<TResponse>(url, "PATCH", jsonData, headers);
        }

        /// <summary>
        /// 核心请求方法
        /// </summary>
        private async Task<HttpResult<T>> SendRequestAsync<T>(
            string url,
            string method,
            string jsonData = null,
            Dictionary<string, string> customHeaders = null)
        {
            var stopwatch = Stopwatch.StartNew();

            if (string.IsNullOrEmpty(url))
            {
                return HttpResult<T>.Failure("URL不能为空", 0, UnityWebRequest.Result.DataProcessingError);
            }

            using UnityWebRequest request = new UnityWebRequest(url, method);

            // 设置下载处理器
            request.downloadHandler = new DownloadHandlerBuffer();

            // 设置上传数据
            if (!string.IsNullOrEmpty(jsonData))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            // 设置超时
            request.timeout = _timeout;

            // 应用默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            // 应用自定义请求头（会覆盖默认请求头）
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            if (VerboseLogging)
            {
                UnityEngine.Debug.Log($"[HttpClient] {method} {url}\nBody: {jsonData}");
            }

            // 发送请求
            var asyncOp = request.SendWebRequest();

            // 等待完成
            while (!asyncOp.isDone)
            {
                await Task.Yield();
            }

            stopwatch.Stop();
            long elapsedMs = stopwatch.ElapsedMilliseconds;

            // 获取响应头
            var responseHeaders = new Dictionary<string, string>();
            if (request.GetResponseHeaders() != null)
            {
                foreach (var header in request.GetResponseHeaders())
                {
                    responseHeaders[header.Key] = header.Value;
                }
            }

            // 处理错误
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"{request.error}";

                if (VerboseLogging)
                {
                    UnityEngine.Debug.LogWarning($"[HttpClient] 请求失败 - {method} {url}\n" +
                                                $"Result: {request.result}\n" +
                                                $"Code: {request.responseCode}\n" +
                                                $"Error: {errorMsg}\n" +
                                                $"Time: {elapsedMs}ms");
                }

                var failureResult = HttpResult<T>.Failure(errorMsg, request.responseCode, request.result, elapsedMs);
                failureResult.ResponseHeaders = responseHeaders;
                return failureResult;
            }

            // 获取响应文本
            string responseText = request.downloadHandler.text;

            if (VerboseLogging)
            {
                UnityEngine.Debug.Log($"[HttpClient] 请求成功 - {method} {url}\n" +
                                     $"Code: {request.responseCode}\n" +
                                     $"Response: {responseText}\n" +
                                     $"Time: {elapsedMs}ms");
            }

            // 如果T是string类型，直接返回原始文本
            if (typeof(T) == typeof(string))
            {
                var stringResult = HttpResult<T>.Success(
                    (T)(object)responseText,
                    responseText,
                    request.responseCode,
                    elapsedMs
                );
                stringResult.ResponseHeaders = responseHeaders;
                return stringResult;
            }

            // 反序列化响应
            if (string.IsNullOrEmpty(responseText))
            {
                var emptyResult = HttpResult<T>.Success(default, responseText, request.responseCode, elapsedMs);
                emptyResult.ResponseHeaders = responseHeaders;
                return emptyResult;
            }

            try
            {
                T data = _serializer.Deserialize<T>(responseText);
                var successResult = HttpResult<T>.Success(data, responseText, request.responseCode, elapsedMs);
                successResult.ResponseHeaders = responseHeaders;
                return successResult;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[HttpClient] 反序列化失败: {ex.Message}\n" +
                                          $"类型: {typeof(T).Name}\n" +
                                          $"响应: {responseText}");

                var deserializeFailureResult = HttpResult<T>.DeserializationFailure(
                    responseText,
                    request.responseCode,
                    ex.Message,
                    elapsedMs
                );
                deserializeFailureResult.ResponseHeaders = responseHeaders;
                return deserializeFailureResult;
            }
        }
#endif
    }
}
