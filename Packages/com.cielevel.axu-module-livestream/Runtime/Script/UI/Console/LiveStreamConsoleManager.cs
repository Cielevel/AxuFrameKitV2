using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream
{
    // 游戏内直播控制台总控：面板收集选中项 → 构造 UserData* 单元 → TirggerCollectionSo.Trigger 分发
    // 即挂即用的普通 MonoBehaviour（不走框架单例），引用全部在 Inspector 手动挂载
    public class LiveStreamConsoleManager : MonoBehaviour
    {
        [SerializeField] private UserConfigSo _userConfigSo;
        [SerializeField] private GiftConfigSo _giftConfigSo;
        [SerializeField] private TirggerCollectionSo _tirggerCollectionSo;
        [SerializeField] private UIPanelLiveStreamConsole _panel;

        private void Start()
        {
            // 逐项校验，缺失即指名报错并放弃订阅，避免半初始化状态
            if (_userConfigSo == null)
            {
                Debug.LogError($"[LiveStreamConsoleManager] 未在 Inspector 上挂载 {nameof(_userConfigSo)}，控制台无法启动。");
                return;
            }

            if (_giftConfigSo == null)
            {
                Debug.LogError($"[LiveStreamConsoleManager] 未在 Inspector 上挂载 {nameof(_giftConfigSo)}，控制台无法启动。");
                return;
            }

            if (_tirggerCollectionSo == null)
            {
                Debug.LogError($"[LiveStreamConsoleManager] 未在 Inspector 上挂载 {nameof(_tirggerCollectionSo)}，控制台无法启动。");
                return;
            }

            if (_panel == null)
            {
                Debug.LogError($"[LiveStreamConsoleManager] 未在 Inspector 上挂载 {nameof(_panel)}，控制台无法启动。");
                return;
            }

            _panel.Setup(_userConfigSo.Users, _giftConfigSo.Gifts);
            _panel.CommentRequested += HandleCommentRequested;
            _panel.LikeRequested += HandleLikeRequested;
            _panel.GiftRequested += HandleGiftRequested;
        }

        private void OnDestroy()
        {
            // 面板随预制体销毁、事件随实例走，无静态泄漏；仍做对称退订保持干净
            if (_panel != null)
            {
                _panel.CommentRequested -= HandleCommentRequested;
                _panel.LikeRequested -= HandleLikeRequested;
                _panel.GiftRequested -= HandleGiftRequested;
            }
        }

        private void HandleCommentRequested()
        {
            List<UserDataUnit> users = _panel.GetSelectedUsers();
            if (users.Count == 0)
            {
                Debug.LogWarning("请先选择至少一个用户！");
                return;
            }

            string comment = _panel.GetCommentText();
            if (string.IsNullOrEmpty(comment))
            {
                Debug.LogWarning("评论不能为空！");
                return;
            }

            foreach (UserDataUnit user in users)
            {
                UserDataComment commentData = new UserDataComment
                {
                    UserId = user.UserId,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Avatar = user.Avatar,
                    Comment = comment
                };
                _tirggerCollectionSo.Trigger(commentData);
            }

            _panel.ClearCommentInput();
        }

        private void HandleLikeRequested()
        {
            List<UserDataUnit> users = _panel.GetSelectedUsers();
            if (users.Count == 0)
            {
                Debug.LogWarning("请先选择至少一个用户！");
                return;
            }

            int count = _panel.GetLikeCount();
            foreach (UserDataUnit user in users)
            {
                UserDataLike likeData = new UserDataLike
                {
                    UserId = user.UserId,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Avatar = user.Avatar,
                    LikeCount = count
                };
                _tirggerCollectionSo.Trigger(likeData);
            }
        }

        private void HandleGiftRequested()
        {
            List<UserDataUnit> users = _panel.GetSelectedUsers();
            if (users.Count == 0)
            {
                Debug.LogWarning("请先选择至少一个用户！");
                return;
            }

            List<UIPanelLiveStreamConsole.GiftSelection> gifts = _panel.GetSelectedGifts();
            if (gifts.Count == 0)
            {
                Debug.LogWarning("请先选择至少一个礼物！");
                return;
            }

            // M 个选中用户 × N 种选中礼物，逐条触发
            foreach (UserDataUnit user in users)
            {
                foreach (UIPanelLiveStreamConsole.GiftSelection selection in gifts)
                {
                    UserDataGift giftData = new UserDataGift
                    {
                        UserId = user.UserId,
                        Nickname = user.Nickname,
                        AvatarUrl = user.AvatarUrl,
                        Avatar = user.Avatar,
                        GiftName = selection.Gift.Name,
                        GiftIndex = selection.GiftIndex,
                        GiftCount = selection.Count
                    };
                    _tirggerCollectionSo.Trigger(giftData);
                }
            }
        }
    }
}
