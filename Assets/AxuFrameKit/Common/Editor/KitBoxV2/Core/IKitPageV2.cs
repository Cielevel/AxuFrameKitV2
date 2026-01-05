namespace AxuFrameKit.Common.Editor.KitBoxV2.Core
{
    /// <summary>
    /// KitBoxV2 页面接口
    /// </summary>
    public interface IKitPageV2
    {
        /// <summary>
        /// 页面标题
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// 初始化页面
        /// </summary>
        /// <param name="kitBox">主窗口引用</param>
        void Init(IKitBoxV2 kitBox);
        
        /// <summary>
        /// 绘制页面内容
        /// </summary>
        void Draw();
        
        /// <summary>
        /// 刷新页面数据
        /// </summary>
        void Refresh();
    }
}
