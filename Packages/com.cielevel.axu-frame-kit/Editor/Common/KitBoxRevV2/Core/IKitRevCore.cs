using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Core
{
    /// <summary>
    /// KitBoxRevV2 主窗口接口
    /// </summary>
    public interface IKitBoxRevV2
    {
        void OnSelectPage(string groupName, int pageIndex);
        void Refresh();
    }

    /// <summary>
    /// KitBoxRevV2 页面接口，支持 UIToolKit
    /// </summary>
    public interface IKitPageRevV2
    {
        string Title { get; }
        void Init(IKitBoxRevV2 kitBox);
        VisualElement CreateContent(); // 返回 UIToolKit 根元素
        void Refresh();
        
        // 兼容旧版 IMGUI 绘制 (可选)
        void DrawIMGUI();
    }

    /// <summary>
    /// KitBoxRevV2 分组接口
    /// </summary>
    public interface IKitGroupRevV2
    {
        string Title { get; }
        string GroupName { get; }
        IKitPageRevV2[] Pages { get; }
        bool IsExpanded { get; set; }
        void Init(IKitBoxRevV2 kitBox);
    }
}
