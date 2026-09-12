using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Modules.LiveStream
{
    [CreatorEntry("直播互动/触发器/TirggerCollectionSo")]
    public class TirggerCollectionSo : ScriptableObject
    {
        [Header("PreTrigger")] // 预先触发（关于弹窗或动画）
        [SerializeField] private TriggerCommentConfigSo _preTriggerCommentConfigSo;
        [SerializeField] private TriggerGiftConfigSo _preTriggerGiftConfigSo;
        [SerializeField] private TriggerLikeConfigSo _preTriggerLikeConfigSo;

        [Header("MainTrigger")] // 主要触发（关于逻辑）
        [SerializeField] private TriggerCommentConfigSo _mainTriggerCommentConfigSo;
        [SerializeField] private TriggerGiftConfigSo _mainTriggerGiftConfigSo;
        [SerializeField] private TriggerLikeConfigSo _mainTriggerLikeConfigSo;
        [SerializeField] private GiftConfigSo _giftConfigSo;

        [Header("TEST")]

        [SerializeField] private UserConfigSo _userConfigSo;

        public void Trigger(UserDataUnit userDataUnit)
        {
            if (userDataUnit is UserDataComment userDataComment)
            {
                _preTriggerCommentConfigSo?.PushComment(userDataComment);
                _mainTriggerCommentConfigSo?.PushComment(userDataComment);
            }
            else if (userDataUnit is UserDataGift userDataGift)
            {
                _preTriggerGiftConfigSo?.PushGift(userDataGift);
                _mainTriggerGiftConfigSo?.PushGift(userDataGift);
            }
            else if (userDataUnit is UserDataLike userDataLike)
            {
                // Like 无形态分支，入口直通（设计原因见 TriggerLikeConfigSo 类注释）
                _preTriggerLikeConfigSo?.PushLike(userDataLike);
                _mainTriggerLikeConfigSo?.PushLike(userDataLike);
            }
        }
    }
}