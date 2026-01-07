using TMPro;
using UnityEngine;
using AxuFrameKit.Common.UI.Framework;

namespace AxuFrameKit.Modules.LiveStream.Script.UI
{
    public class UIPanelComment : UIBasePanel
    {
        [SerializeField] private TMP_InputField _input;

        protected override void Start()
        {
            base.Start();

            _input.onValueChanged.AddListener(OnInput);
        }

        private void OnInput(string text)
        {

        }

        public void OnSelectUser(string userNickname)
        {
            if (_input.text == "@")
                _input.text = $"@{userNickname}";
        }
    }
}
