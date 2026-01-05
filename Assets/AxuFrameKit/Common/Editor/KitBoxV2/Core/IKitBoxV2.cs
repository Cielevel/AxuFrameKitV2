namespace AxuFrameKit.Common.Editor.KitBoxV2.Core
{
    /// <summary>
    /// KitBoxV2 主窗口接口
    /// </summary>
    public interface IKitBoxV2
    {
        /// <summary>
        /// 当选择某个分组的页面时触发
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="pageIndex">页面索引</param>
        void OnSelectPage(string groupName, int pageIndex);
        
        /// <summary>
        /// 刷新窗口
        /// </summary>
        void Refresh();
    }
}
