using AxuFrameKit.Common.Editor.KitBoxV2.Core;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.CopierGroup
{
    /// <summary>
    /// 拷贝器分组
    /// </summary>
    public class CopierGroup : KitGroupBase
    {
        public override string Title => "拷贝器";
        public override string GroupName => nameof(CopierGroup);
        
        private readonly IKitPageV2[] _pages =
        {
            new MaterialCopierPage(),
        };
        
        public override IKitPageV2[] Pages => _pages;
    }
}
