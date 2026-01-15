using System;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    [Serializable]
    public class UserDataUnit
    {
        [HideInInspector]
        public string elementName;

        [field: SerializeField] public Sprite Avatar { get; set; }
        [field: SerializeField] public string AvatarUrl { get; set; }
        [field: SerializeField] public string Nickname { get; set; }
        [field: SerializeField] public string UserId { get; set; }
    }

    // 以下是Trigger通信单元

    public class UserDataGift : UserDataUnit
    {
        public string GiftName { get; set; }

        public int GiftIndex { get; set; } = -1;

        public int GiftCount { get; set; }
    }

    public class UserDataComment : UserDataUnit
    {
        public string Comment { get; set; }
    }

    public class UserDataLike : UserDataUnit
    {
        public int LikeCount { get; set; }
    }
}