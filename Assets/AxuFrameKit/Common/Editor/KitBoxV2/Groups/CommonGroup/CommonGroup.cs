using AxuFrameKit.Common.Editor.KitBoxV2.Core;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.CommonGroup
{
    /// <summary>
    /// 通用分组
    /// </summary>
    public class CommonGroup : KitGroupBase
    {
        public override string Title => "通用";
        public override string GroupName => nameof(CommonGroup);
        
        private readonly IKitPageV2[] _pages =
        {
            new WelcomePage(),
            new SymbolsEditPage(),
        };
        
        public override IKitPageV2[] Pages => _pages;
    }
}
