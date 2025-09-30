using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AxuFrameKit._Core.Extension;
using UnityEngine;
using UnityEngine.Networking;
#if UNI_TASK
using Cysharp.Threading.Tasks;
#endif

namespace AxuFrameKit.Common.Utility.Net
{
    public static class HttpX
    {
        private const string PostMethod = "POST";
        private const string GetMethod = "GET";
        
#if UNI_TASK
        /// <summary>
        /// 发送Post请求，将发送一个json，返回json字符串
        /// </summary>
        /// <param name="url"></param>
        /// <param name="jsonData"></param>
        public static async UniTask<string> Post(string url, string jsonData)
        {
            using UnityWebRequest uwr = new (url, PostMethod);
            uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            
            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(PostMethod, url, uwr);
            return string.Empty;
        }

        /// <summary>
        /// 发送Post请求，将发送一个json，返回json字符串
        /// </summary>
        public static async UniTask<string> Post(string url, string jsonData = null, params (string headName, string headValue)[] @params)
        {
            VerifyHeader(url, PostMethod, @params);
            
            using UnityWebRequest uwr = new(url, PostMethod);

            if (jsonData != null && jsonData.IsNotNullOrEmpty())
            {
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                uwr.SetRequestHeader("Content-Type", "application/json");
            }
            
            for (int i = 0; i < @params.Length; i++)
                uwr.SetRequestHeader(@params[i].headName, @params[i].headValue);

            uwr.downloadHandler = new DownloadHandlerBuffer();
            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(PostMethod, url, uwr);
            return string.Empty;
        }
        
        /// <summary>
        /// 发送Get请求，自定义header，返回json字符串
        /// </summary>
        public static async UniTask<string> Get(string url, params (string headName, string headValue)[] @params)
        {
            VerifyHeader(url, GetMethod, @params);
            
            using UnityWebRequest uwr = new(url, GetMethod);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            for (int i = 0; i < @params.Length; i++)
                uwr.SetRequestHeader(@params[i].headName, @params[i].headValue);

            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(GetMethod, url, uwr);
            return string.Empty;
        }
#endif

#if !UNI_TASK
        /// <summary>
        /// 发送Post请求，将发送一个json，返回json字符串
        /// </summary>
        /// <param name="url"></param>
        /// <param name="jsonData"></param>
        public static async Task<string> Post(string url, string jsonData)
        {
            using UnityWebRequest uwr = new(url, PostMethod);
            uwr.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(PostMethod, url, uwr);
            return string.Empty;
        }

        /// <summary>
        /// 发送Post请求，将发送一个json，返回json字符串
        /// </summary>
        public static async Task<string> Post(string url, string jsonData = null, params (string headName, string headValue)[] @params)
        {
            VerifyHeader(url, PostMethod, @params);
            
            using UnityWebRequest uwr = new(url, PostMethod);

            if (jsonData != null && jsonData.IsNotNullOrEmpty())
            {
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                uwr.SetRequestHeader("Content-Type", "application/json");
            }
            
            for (int i = 0; i < @params.Length; i++)
                uwr.SetRequestHeader(@params[i].headName, @params[i].headValue);

            uwr.downloadHandler = new DownloadHandlerBuffer();
            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(PostMethod, url, uwr);
            return string.Empty;
        }

        /// <summary>
        /// 发送Get请求，自定义header，返回json字符串
        /// </summary>
        public static async Task<string> Get(string url, params (string headName, string headValue)[] @params)
        {
            VerifyHeader(url, GetMethod, @params);

            using UnityWebRequest uwr = new(url, GetMethod);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            for (int i = 0; i < @params.Length; i++)
                uwr.SetRequestHeader(@params[i].headName, @params[i].headValue);

            await uwr.SendWebRequest();

            if (uwr.result is UnityWebRequest.Result.Success)
                return Encoding.UTF8.GetString(uwr.downloadHandler.data);

            DebugErrorMsg(GetMethod, url, uwr);
            return string.Empty;
        }
#endif

        private static void DebugErrorMsg(string httpType, string url, UnityWebRequest uwr)
        {
            Debug.LogWarning($"{httpType}出错，url为：{url}。Result：{ uwr.result}, code：{uwr.responseCode}");
        }
        
        private static void VerifyHeader(string url, string requestType,
                                         params (string headName, string headValue)[] @params)
        {
            HashSet<string> headNamesHs = new(@params.Length);
            for (int i = 0; i < @params.Length; i++)
            {
                if (!headNamesHs.Add(@params[i].headName))
                    Debug.LogWarning($"对url：[{url}]的{requestType}请求中，键{@params[i].headName}重复了，继续请求将覆盖以及添加进去的值！");

                // 空
                if (string.IsNullOrEmpty(@params[i].headName))
                    Debug.LogError($"对url：[{url}]的{requestType}请求中，有一个键的值为空！");

                // 空格
                if (@params[i].headName.Contains(' '))
                    Debug.LogError($"对url：[{url}]的{requestType}请求中，有一个键的值包含了空格，这是不被允许的！");

                // #,$,%,&
                if (@params[i].headName.Contains('#') || @params[i].headName.Contains('$') ||
                    @params[i].headName.Contains('%') || @params[i].headName.Contains('&'))
                    Debug.LogError($"对url：[{url}]的{requestType}请求中，有一个键的值包含了#,$,%,&，这是不被允许的！");

                if (IsReservedHeader(@params[i].headName))
                    Debug.LogError($"对url：[{url}]的{requestType}请求中，有一个键的值是保留的头信息！");
            }

            bool IsReservedHeader(string headerName)
            {
                List<string> reservedHeaders = new()
                    { "host", "content-length", "connection", "transfer-encoding", "connection", "upgrade" };
                return reservedHeaders.Contains(headerName.ToLower());
            }

            // TODO 小写化 键的验证（因为服务器将按照小写来统一处理，并不区分大小写）
        }
    }
}