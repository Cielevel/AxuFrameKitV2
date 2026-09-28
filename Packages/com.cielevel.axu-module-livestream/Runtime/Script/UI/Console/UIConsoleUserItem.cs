using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Modules.LiveStream
{
    // 控制台用户列表条目：展示头像/昵称/UID，勾选状态由面板收集
    public class UIConsoleUserItem : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _avatar;
        [SerializeField] private TMP_Text _nicknameText;
        [SerializeField] private TMP_Text _uidText;

        public UserDataUnit Data { get; private set; }

        public bool IsSelected => _toggle != null && _toggle.isOn;

        public void Bind(UserDataUnit data)
        {
            Data = data;
            _nicknameText.text = string.IsNullOrEmpty(data.Nickname) ? "(无昵称)" : data.Nickname;
            _uidText.text = data.UserId ?? string.Empty;

            // 空头像只关 Image，保留背景色块供美术层表现
            _avatar.sprite = data.Avatar;
            _avatar.enabled = data.Avatar != null;
        }
    }
}
