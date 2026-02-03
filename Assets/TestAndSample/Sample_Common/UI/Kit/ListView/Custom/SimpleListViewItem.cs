using AxuFrameKit.Common.UI.Kit.ListView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TestAndSample.Sample_Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView Item示例实现
    /// 演示如何继承UIListViewItem并实现自定义数据绑定
    /// </summary>
    public class SimpleListViewItem : UIListViewItem
    {
        [Header("UI组件引用")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI indexText;

        protected override void OnUpdateData(object data, int index)
        {
            // 方式1: 直接使用基类提供的泛型方法
            if (TryGetData<SimpleItemData>(out var itemData))
            {
                if (titleText != null)
                    titleText.text = itemData.Title;

                if (descriptionText != null)
                    descriptionText.text = itemData.Description;

                if (iconImage != null)
                    iconImage.sprite = itemData.Icon;

                if (indexText != null)
                    indexText.text = $"#{index}";
            }
            // 方式2: 如果数据是字符串，也可以简单显示
            else if (data is string str)
            {
                if (titleText != null)
                    titleText.text = str;

                if (indexText != null)
                    indexText.text = $"#{index}";
            }
        }

        protected override void OnSelectionChanged(bool isSelected)
        {
            // 自定义选中时的视觉反馈
            // 例如改变文字颜色等
            if (titleText != null)
                titleText.color = isSelected ? Color.yellow : Color.white;
        }

        protected override void OnItemClicked()
        {
            Debug.Log($"Item {DataIndex} clicked! Title: {GetData<SimpleItemData>()?.Title}");
        }

        protected override void OnShow()
        {
            // Item显示时的逻辑（例如播放动画）
        }

        protected override void OnHide()
        {
            // Item隐藏时的逻辑（例如停止动画）
        }
    }

    /// <summary>
    /// 简单的数据类示例
    /// </summary>
    [System.Serializable]
    public class SimpleItemData
    {
        public string Title;
        public string Description;
        public Sprite Icon;
        public int Value;

        public SimpleItemData(string title, string description = "", Sprite icon = null, int value = 0)
        {
            Title = title;
            Description = description;
            Icon = icon;
            Value = value;
        }
    }
}
