using AxuFrameKit.Common.Editor.KitBoxV2.Core;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.UtilityGroup
{
    /// <summary>
    /// 实用工具分组
    /// </summary>
    public class UtilityGroup : KitGroupBase
    {
        public override string Title => "实用工具";
        public override string GroupName => nameof(UtilityGroup);
        
        private readonly IKitPageV2[] _pages =
        {
            new SceneJumpPage(),
        };
        
        public override IKitPageV2[] Pages => _pages;
    }
}
