using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Modules.LiveStream
{
    // 控制台礼物列表条目：展示图标/名称/价格 + 数量输入
    // GiftIndex 为礼物在 GiftConfigSo.Gifts 中的下标，下游触发按它匹配
    public class UIConsoleGiftItem : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_InputField _countInput;

        public GiftDataUnit Data { get; private set; }

        public int GiftIndex { get; private set; }

        public bool IsSelected => _toggle != null && _toggle.isOn;

        // 解析失败或小于 1 时一律按 1 处理
        public int Count
        {
            get
            {
                if (_countInput == null || !int.TryParse(_countInput.text, out int count) || count < 1)
                {
                    return 1;
                }

                return count;
            }
        }

        public void Bind(GiftDataUnit data, int giftIndex)
        {
            Data = data;
            GiftIndex = giftIndex;
            _nameText.text = string.IsNullOrEmpty(data.Name) ? "(无名称)" : data.Name;
            _priceText.text = $"¥ {data.Price}";

            _icon.sprite = data.Img;
            _icon.enabled = data.Img != null;

            // Bind 可能被多次调用，先清空旧订阅避免 onValueChanged 叠加
            _countInput.onValueChanged.RemoveAllListeners();
            _countInput.SetTextWithoutNotify("1");
            _countInput.onValueChanged.AddListener(FilterCountInput);
        }

        // 数量输入过滤：只保留数字、空串视为 1，对齐 Editor 模拟器窗口的过滤逻辑
        private void FilterCountInput(string value)
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

            if (int.TryParse(filtered, out int quantity))
            {
                if (quantity < 1)
                {
                    quantity = 1;
                }

                filtered = quantity.ToString();
            }

            if (value != filtered)
            {
                _countInput.SetTextWithoutNotify(filtered);
            }
        }
    }
}
