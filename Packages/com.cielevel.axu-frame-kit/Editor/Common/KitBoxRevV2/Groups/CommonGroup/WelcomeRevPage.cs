using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CommonGroup
{
    public class WelcomeRevPage : IKitPageRevV2
    {
        public string Title => "工具集介绍-Rev";
        private IKitBoxRevV2 _kitBox;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("welcome-container");

            // Header Title with Gradient-like style (using multiple levels)
            var title = new Label("AxuFrameKit V2 REV");
            title.AddToClassList("welcome-title");
            root.Add(title);

            var subtitle = new Label("Next-Generation Unity Toolset");
            subtitle.AddToClassList("welcome-subtitle");
            root.Add(subtitle);

            // Feature Cards Container
            var cardContainer = new VisualElement();
            cardContainer.AddToClassList("row");
            cardContainer.style.flexWrap = Wrap.Wrap;
            cardContainer.style.justifyContent = Justify.Center;
            cardContainer.style.maxWidth = 600;
            root.Add(cardContainer);

            AddFeatureCard(cardContainer, "📦 通用工具", "定义符号宏管理 & 环境配置", "#3498db");
            AddFeatureCard(cardContainer, "📋 拷贝器", "材质球批量关联复制 & 资产同步", "#2ecc71");
            AddFeatureCard(cardContainer, "📊 表格工具", "高性能 CSV 编辑与 ScriptableObject 导出", "#f1c40f");
            AddFeatureCard(cardContainer, "🔧 实用工具", "一键场景跳转 & 资产快速定位", "#e67e22");

            // Footer
            var footer = new Label("Designed with ❤️ specifically for High Efficiency");
            footer.style.marginTop = 50;
            footer.style.color = new Color(0.4f, 0.4f, 0.4f);
            footer.style.fontSize = 12;
            root.Add(footer);

            return root;
        }

        private void AddFeatureCard(VisualElement parent, string title, string desc, string colorHex)
        {
            var card = new VisualElement();
            card.AddToClassList("feature-card");

            if (ColorUtility.TryParseHtmlString(colorHex, out var color))
            {
                card.style.borderLeftColor = color;
            }

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("feature-card-title");
            card.Add(titleLabel);

            var descLabel = new Label(desc);
            descLabel.AddToClassList("feature-card-desc");
            card.Add(descLabel);

            parent.Add(card);
        }

        public void Refresh() { }
        public void DrawIMGUI() { }
    }
}
