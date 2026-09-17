using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using AxuFrameKit.Common.Utility.Singleton;

namespace AxuFrameKit.Modules.LiveStream
{
    /// <summary>
    /// 接入：秋恋猫软件，不必挂载，直接调用StartReceive与EndReceive即可
    /// </summary>
    public class RequestLiveStream : SingletonMonoCrDs<RequestLiveStream>
    {
        // 通过注册来实现消息传递，而非内置到HandleMsg中
        
        public event Action<MsgEnter> OnUserEnterEvent;
        public event Action<MsgSpeak> OnUserSpeakEvent;
        public event Action<MsgGift> OnUserGiftEvent;
        public event Action<MsgLike> OnUserLikeEvent;
        public event Action<MsgSubscribe> OnUserSubscribeEvent;
        public event Action<MsgOnlineNumber> OnlineUserNumberEvent;
        
        private const string URL = "http://127.0.0.1:9999/game-da302d82";
        private const float ReqInterval = 0.5f;
        
        private static readonly Dictionary<string, Type> TypeMap = new()
        {
            { "MemberMessage", typeof(MsgEnter) },
            { "ChatMessage", typeof(MsgSpeak) },
            { "GiftMessage", typeof(MsgGift) },
            { "LikeMessage", typeof(MsgLike) },
            { "SocialMessage", typeof(MsgSubscribe) },
            { "RoomDetail", typeof(MsgOnlineNumber) },
        };
        
        private Coroutine _requestCoroutine;

        public void StartRequest()
        {
            _requestCoroutine = StartCoroutine(Get());
        }

        public void EndRequest()
        {
            if (_requestCoroutine != null)
                StopCoroutine(_requestCoroutine);
        }

        private IEnumerator Get()
        {
            var wait = new WaitForSeconds(ReqInterval);

            while (true)
            {
                using UnityWebRequest req = UnityWebRequest.Get(URL);
                // 设置超时（秒），避免本地服务假死时请求无限挂起、轮询停摆
                req.timeout = 15;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string responseText = req.downloadHandler.text;
                    try
                    {
                        JObject parsedJson = JObject.Parse(responseText);
                        JArray  messages   = (JArray)parsedJson["message"];
                        if (messages != null)
                            foreach (var jToken in messages)
                            {
                                var    message = (JObject)jToken;
                                string type    = message["type"]?.ToString();
                                if (type != null && TypeMap.TryGetValue(type, out var t))
                                {
                                    var obj = JsonConvert.DeserializeObject(message.ToString(), t);
                                    HandleMsg(obj);
                                }
                            }
                    }
                    catch (Exception e)
                    {
                        // 畸形响应不应杀死轮询协程：记录警告后等待下一轮重试
                        string snippet = responseText.Length > 120 ? responseText.Substring(0, 120) : responseText;
                        Debug.LogWarning($"解析直播消息响应失败，已跳过本轮（{e.GetType().Name}: {e.Message}），响应片段：{snippet}");
                    }
                }
                else
                {
                    Debug.LogError($"GET消息错误，state：{req.result}");
                }

                yield return wait;
            }
        }

        private void HandleMsg(object msg)
        {
            if (msg is MsgEnter msgEnter)
            {
                Debug.Log($"{msgEnter.name} 进入直播间");
                OnUserEnterEvent?.Invoke(msgEnter);
            }

            if (msg is MsgSpeak msgSpeak)
            {
                Debug.Log($"{msgSpeak.name} 说：{msgSpeak.content}");
                OnUserSpeakEvent?.Invoke(msgSpeak);
            }

            if (msg is MsgGift msgGift)
            {
                Debug.Log($"{msgGift.name} 送了 {msgGift.giftCount} 个 {msgGift.giftName}");
                OnUserGiftEvent?.Invoke(msgGift);
            }

            if (msg is MsgLike msgLike)
            {
                Debug.Log($"{msgLike.name} 点赞了 {msgLike.count} 次");
                OnUserLikeEvent?.Invoke(msgLike);
            }

            if (msg is MsgSubscribe msgSubscribe)
            {
                Debug.Log($"{msgSubscribe.name} 关注了主播");
                OnUserSubscribeEvent?.Invoke(msgSubscribe);
            }

            if (msg is MsgOnlineNumber msgOnlineNumber)
            {
                Debug.Log($"当前直播人数为 {msgOnlineNumber.memberCount}");
                OnlineUserNumberEvent?.Invoke(msgOnlineNumber);
            }
        }
    }

    public class Info
    {
        public string message;
        public string data;
        public bool   status;
        public string tips;

        public override string ToString()
        {
            return $"msg: {message}, data: {data}, status: {status}, tips: {tips}";
        }
    }

    public class MsgBase
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string type;
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string name;
        /// <summary>
        /// 用户标识
        /// </summary>
        public string player;
        /// <summary>
        /// 用户头像地址
        /// </summary>
        public string head_img;
    }

    public class MsgEnter : MsgBase
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string content;
    }

    public class MsgSpeak : MsgBase
    {
        /// <summary>
        /// 发言消息内容
        /// </summary>
        public string content;
    }

    public class MsgGift : MsgBase
    {
        /// <summary>
        /// 礼物消息内容（送出n个xxx礼物）
        /// </summary>
        public string content;
        /// <summary>
        /// 礼物iD
        /// </summary>
        public string giftId;
        /// <summary>
        /// 礼物昵称
        /// </summary>
        public string giftName;
        /// <summary>
        /// 礼物数量
        /// </summary>
        public string giftCount;
        /// <summary>
        /// 单个礼物的价值（单位：钻石/或对应平台的单位）
        /// </summary>
        public string diamondCount;
    }

    public class MsgLike : MsgBase
    {
        /// <summary>
        /// 点赞消息内容（点赞了n次）
        /// </summary>
        public string content;
        /// <summary>
        /// 点赞数量
        /// </summary>
        public string count;
    }

    public class MsgSubscribe : MsgBase
    {
        /// <summary>
        /// 消息内容（关注了主播）
        /// </summary>
        public string content;
    }

    public class MsgOnlineNumber
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string type;
        /// <summary>
        /// 在线人数
        /// </summary>
        public string memberCount;
        /// <summary>
        /// 消息内容（当前直播人数为n）
        /// </summary>
        public string content;
    }
}