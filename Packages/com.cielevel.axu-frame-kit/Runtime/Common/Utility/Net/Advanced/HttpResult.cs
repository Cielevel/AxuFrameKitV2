using System;
using UnityEngine.Networking;

namespace AxuFrameKit.Common.Utility.Net
{
    /// <summary>
    /// HTTP请求结果包装类
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class HttpResult<T>
    {
        /// <summary>
        /// 请求是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 反序列化后的数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 原始响应文本
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public long StatusCode { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// UnityWebRequest的结果类型
        /// </summary>
        public UnityWebRequest.Result Result { get; set; }

        /// <summary>
        /// 请求耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 响应头信息
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> ResponseHeaders { get; set; }

        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static HttpResult<T> Success(T data, string rawResponse, long statusCode, long elapsedMs = 0)
        {
            return new HttpResult<T>
            {
                IsSuccess = true,
                Data = data,
                RawResponse = rawResponse,
                StatusCode = statusCode,
                Result = UnityWebRequest.Result.Success,
                ElapsedMilliseconds = elapsedMs,
                ResponseHeaders = new System.Collections.Generic.Dictionary<string, string>()
            };
        }

        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static HttpResult<T> Failure(string errorMessage, long statusCode, UnityWebRequest.Result result, long elapsedMs = 0)
        {
            return new HttpResult<T>
            {
                IsSuccess = false,
                Data = default,
                RawResponse = string.Empty,
                StatusCode = statusCode,
                ErrorMessage = errorMessage,
                Result = result,
                ElapsedMilliseconds = elapsedMs,
                ResponseHeaders = new System.Collections.Generic.Dictionary<string, string>()
            };
        }

        /// <summary>
        /// 创建反序列化失败的结果
        /// </summary>
        public static HttpResult<T> DeserializationFailure(string rawResponse, long statusCode, string errorMessage, long elapsedMs = 0)
        {
            return new HttpResult<T>
            {
                IsSuccess = false,
                Data = default,
                RawResponse = rawResponse,
                StatusCode = statusCode,
                ErrorMessage = $"反序列化失败: {errorMessage}",
                Result = UnityWebRequest.Result.DataProcessingError,
                ElapsedMilliseconds = elapsedMs,
                ResponseHeaders = new System.Collections.Generic.Dictionary<string, string>()
            };
        }

        /// <summary>
        /// 获取友好的错误描述
        /// </summary>
        public string GetErrorDescription()
        {
            if (IsSuccess)
                return "请求成功";

            return $"[{Result}] Code: {StatusCode}, Message: {ErrorMessage}";
        }

        public override string ToString()
        {
            if (IsSuccess)
                return $"Success - Code: {StatusCode}, Data: {Data}, Time: {ElapsedMilliseconds}ms";

            return $"Failure - {GetErrorDescription()}, Time: {ElapsedMilliseconds}ms";
        }
    }
}
