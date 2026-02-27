using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using UnityEngine;

namespace AxuExperimental.HtmlCommunicate
{
    public class LocalWebServer : MonoBehaviour
    {
        private HttpListener listener;
        private Thread listenerThread;
        private ConcurrentQueue<GiftRequest> requestQueue = new ConcurrentQueue<GiftRequest>();

        // 定义完整的数据结构
        public struct GiftRequest
        {
            public string GiftName;
            public int Amount;
            // 新增的玩家数据
            public string PlayerId;
            public string PlayerName;
            public string PlayerAvatarUrl;
        }

        void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            listenerThread = new Thread(ListenLoop);
            listenerThread.Start();
            Debug.Log("Web Server Started on Port 8080");
        }

        void Update()
        {
            // 主线程处理队列
            if (requestQueue.TryDequeue(out GiftRequest req))
            {
                HandleRequest(req);
            }
        }

        private void ListenLoop()
        {
            while (listener.IsListening)
            {
                try
                {
                    var context  = listener.GetContext();
                    var request  = context.Request;
                    var response = context.Response;

                    // 允许跨域
                    response.AppendHeader("Access-Control-Allow-Origin", "*");

                    string responseText = "";

                    if (request.Url.AbsolutePath == "/gift")
                    {
                        // 获取 URL 参数
                        var query = request.QueryString;

                        // 解析数据
                        GiftRequest newRequest = new GiftRequest
                        {
                            // 网页传参：giftName, amount, playerId, playerName, playerAvatar
                            GiftName        = query["giftName"],
                            Amount          = int.Parse(query["amount"] ?? "1"),
                            PlayerId        = query["playerId"],
                            PlayerName      = query["playerName"],
                            PlayerAvatarUrl = query["playerAvatar"]
                        };

                        requestQueue.Enqueue(newRequest);
                        responseText = $"成功! 已将 {newRequest.GiftName} 发送给 {newRequest.PlayerName}";
                    }
                    else
                    {
                        responseText = "未知的指令路径";
                    }

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (System.Exception e) { Debug.LogWarning(e.Message); }
            }
        }

        // === 这里处理具体的业务逻辑 ===
        private void HandleRequest(GiftRequest req)
        {
            Debug.Log("--------------------------------");
            Debug.Log($"收到赠送指令:");
            Debug.Log($"[玩家] ID: {req.PlayerId}, 名字: {req.PlayerName}, 头像: {req.PlayerAvatarUrl}");
            Debug.Log($"[礼物] 物品: {req.GiftName}, 数量: {req.Amount}");
            Debug.Log("--------------------------------");

            // 可以在这里调用你的游戏逻辑，例如：
            // GameManager.SendGift(req.PlayerId, req.GiftName, req.Amount);
        }

        void OnApplicationQuit()
        {
            if (listener != null) listener.Stop();
            if (listenerThread != null) listenerThread.Abort();
        }
    }
}