using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Modules.LiveStream
{
    // 游戏内运行时直播控制台三栏面板：用户列表 / 礼物列表 / 操作区
    // 只负责展示与收集意图，发送语义由 LiveStreamConsoleManager 实现；引用全部由预制体挂接
    public class UIPanelLiveStreamConsole : MonoBehaviour
    {
        // 一次礼物发送的选中单元：Gift 为配置数据，GiftIndex 为其在 GiftConfigSo.Gifts 中的下标，Count 为数量
        public class GiftSelection
        {
            public GiftDataUnit Gift;
            public int GiftIndex;
            public int Count;
        }

        [Header("用户列表")]
        [SerializeField] private RectTransform _userListContent;
        [SerializeField] private UIConsoleUserItem _userItemTemplate;

        [Header("礼物列表")]
        [SerializeField] private RectTransform _giftListContent;
        [SerializeField] private UIConsoleGiftItem _giftItemTemplate;

        [Header("操作区")]
        [SerializeField] private TMP_InputField _commentInput;
        [SerializeField] private TMP_InputField _likeCountInput;
        [SerializeField] private Button _sendCommentButton;
        [SerializeField] private Button _likeButton;
        [SerializeField] private Button _sendGiftButton;
        [SerializeField] private Button _collapseButton;

        [Header("收起目标（三栏总根）")]
        [SerializeField] private GameObject _bodyRoot;

        public event Action CommentRequested;
        public event Action LikeRequested;
        public event Action GiftRequested;

        private readonly List<UIConsoleUserItem> _userItems = new List<UIConsoleUserItem>();
        private readonly List<UIConsoleGiftItem> _giftItems = new List<UIConsoleGiftItem>();

        private void Awake()
        {
            // 关键引用缺失时指名报错，方便预制体漏挂排查
            EnsureReference(_userListContent, nameof(_userListContent));
            EnsureReference(_userItemTemplate, nameof(_userItemTemplate));
            EnsureReference(_giftListContent, nameof(_giftListContent));
            EnsureReference(_giftItemTemplate, nameof(_giftItemTemplate));
            EnsureReference(_commentInput, nameof(_commentInput));
            EnsureReference(_likeCountInput, nameof(_likeCountInput));
            EnsureReference(_sendCommentButton, nameof(_sendCommentButton));
            EnsureReference(_likeButton, nameof(_likeButton));
            EnsureReference(_sendGiftButton, nameof(_sendGiftButton));
            EnsureReference(_collapseButton, nameof(_collapseButton));
            EnsureReference(_bodyRoot, nameof(_bodyRoot));

            if (_sendCommentButton != null)
            {
                _sendCommentButton.onClick.AddListener(() => CommentRequested?.Invoke());
            }

            if (_likeButton != null)
            {
                _likeButton.onClick.AddListener(() => LikeRequested?.Invoke());
            }

            if (_sendGiftButton != null)
            {
                _sendGiftButton.onClick.AddListener(() => GiftRequested?.Invoke());
            }

            if (_collapseButton != null)
            {
                _collapseButton.onClick.AddListener(ToggleBody);
            }

            if (_likeCountInput != null)
            {
                _likeCountInput.onValueChanged.AddListener(FilterLikeCountInput);
            }
        }

        // 模板是 Content 下的常驻子物体（默认 SetActive(false)），运行时克隆填充；可重入
        public void Setup(IReadOnlyList<UserDataUnit> users, IReadOnlyList<GiftDataUnit> gifts)
        {
            if (_userItemTemplate != null)
            {
                _userItemTemplate.gameObject.SetActive(false);
            }

            if (_giftItemTemplate != null)
            {
                _giftItemTemplate.gameObject.SetActive(false);
            }

            ClearItems();

            if (_userListContent != null && _userItemTemplate != null && users != null)
            {
                foreach (UserDataUnit user in users)
                {
                    UIConsoleUserItem item = Instantiate(_userItemTemplate, _userListContent);
                    item.Bind(user);
                    item.gameObject.SetActive(true);
                    _userItems.Add(item);
                }
            }

            if (_giftListContent != null && _giftItemTemplate != null && gifts != null)
            {
                for (int i = 0; i < gifts.Count; i++)
                {
                    UIConsoleGiftItem item = Instantiate(_giftItemTemplate, _giftListContent);
                    item.Bind(gifts[i], i);
                    item.gameObject.SetActive(true);
                    _giftItems.Add(item);
                }
            }
        }

        public List<UserDataUnit> GetSelectedUsers()
        {
            List<UserDataUnit> result = new List<UserDataUnit>();
            foreach (UIConsoleUserItem item in _userItems)
            {
                if (item.IsSelected)
                {
                    result.Add(item.Data);
                }
            }

            return result;
        }

        public List<GiftSelection> GetSelectedGifts()
        {
            List<GiftSelection> result = new List<GiftSelection>();
            foreach (UIConsoleGiftItem item in _giftItems)
            {
                if (item.IsSelected)
                {
                    result.Add(new GiftSelection
                    {
                        Gift = item.Data,
                        GiftIndex = item.GiftIndex,
                        Count = item.Count
                    });
                }
            }

            return result;
        }

        public string GetCommentText()
        {
            return _commentInput != null ? _commentInput.text.Trim() : string.Empty;
        }

        // 解析失败或小于 1 时一律按 1 处理
        public int GetLikeCount()
        {
            if (_likeCountInput == null || !int.TryParse(_likeCountInput.text, out int count) || count < 1)
            {
                return 1;
            }

            return count;
        }

        public void ClearCommentInput()
        {
            if (_commentInput != null)
            {
                _commentInput.text = string.Empty;
            }
        }

        private void ClearItems()
        {
            foreach (UIConsoleUserItem item in _userItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _userItems.Clear();

            foreach (UIConsoleGiftItem item in _giftItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _giftItems.Clear();
        }

        private void ToggleBody()
        {
            if (_bodyRoot != null)
            {
                _bodyRoot.SetActive(!_bodyRoot.activeSelf);
            }
        }

        private void EnsureReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference == null)
            {
                Debug.LogError($"[{nameof(UIPanelLiveStreamConsole)}] 缺失序列化引用 {fieldName}，请在预制体上挂接。");
            }
        }

        // 点赞数量输入过滤：只保留数字、空串视为 1，对齐 Editor 模拟器窗口的过滤逻辑
        private void FilterLikeCountInput(string value)
        {
            string filtered = string.Empty;
            foreach (char c in value)
            {
                if (char.IsDigit(c))
                {
                    filtered += c;
                }
            }

            if (string.IsNullOrEmpty(filtered))
            {
                filtered = "1";
            }

            if (int.TryParse(filtered, out int count))
            {
                if (count < 1)
                {
                    count = 1;
                }

                filtered = count.ToString();
            }

            if (value != filtered)
            {
                _likeCountInput.SetTextWithoutNotify(filtered);
            }
        }
    }
}
