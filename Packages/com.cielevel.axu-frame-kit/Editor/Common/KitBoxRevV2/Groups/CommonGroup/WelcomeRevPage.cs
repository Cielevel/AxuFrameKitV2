using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CommonGroup
{
    public class WelcomeRevPage : IKitPageRevV2
    {
        public string Title => "欢迎";
        private IKitBoxRevV2 _kitBox;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("welcome-container");

            var title = new Label("AxuKitBox");
            title.AddToClassList("welcome-title");
            root.Add(title);

            var subtitle = new Label("Unity 编辑器工具箱");
            subtitle.AddToClassList("welcome-subtitle");
            root.Add(subtitle);

            var cardContainer = new VisualElement();
            cardContainer.AddToClassList("row");
            cardContainer.style.flexWrap = Wrap.Wrap;
            cardContainer.style.justifyContent = Justify.Center;
            cardContainer.style.maxWidth = 600;
            root.Add(cardContainer);

            // 卡片即入口：点击直接跳到对应分组的目标页面
            AddFeatureCard(cardContainer, "Common", 1, "通用工具", "符号宏管理 & 环境配置");
            AddFeatureCard(cardContainer, "Copier", 0, "材质工具", "材质球批量关联复制");
            AddFeatureCard(cardContainer, "Table", 0, "表格工具", "CSV 编辑与 ScriptableObject 导出");
            AddFeatureCard(cardContainer, "Utility", 0, "实用工具", "场景快跳 & 资产批量重命名");

            return root;
        }

        private void AddFeatureCard(VisualElement parent, string groupName, int pageIndex, string title, string desc)
        {
            var card = new VisualElement();
            card.AddToClassList("feature-card");
            card.AddManipulator(new Clickable(() => _kitBox?.OnSelectPage(groupName, pageIndex)));

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
