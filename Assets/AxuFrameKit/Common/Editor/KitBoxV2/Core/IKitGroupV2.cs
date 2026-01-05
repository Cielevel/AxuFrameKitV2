namespace AxuFrameKit.Common.Editor.KitBoxV2.Core
{
    /// <summary>
    /// KitBoxV2 分组接口
    /// </summary>
    public interface IKitGroupV2
    {
        /// <summary>
        /// 分组显示标题
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// 分组内的所有页面
        /// </summary>
        IKitPageV2[] Pages { get; }
        
        /// <summary>
        /// 是否展开
        /// </summary>
        bool IsExpanded { get; set; }
        
        /// <summary>
        /// 分组名称标识符
        /// </summary>
        string GroupName { get; }
        
        /// <summary>
        /// 初始化分组
        /// </summary>
        /// <param name="kitBox">主窗口引用</param>
        void Init(IKitBoxV2 kitBox);
        
        /// <summary>
        /// 绘制分组 UI
        /// </summary>
        /// <param name="currentGroupName">当前选中的分组名称</param>
        /// <param name="currentPageIndex">当前选中的页面索引</param>
        void Draw(string currentGroupName, int currentPageIndex);
    }
}
