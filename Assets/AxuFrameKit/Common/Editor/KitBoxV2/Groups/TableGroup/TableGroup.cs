using AxuFrameKit.Common.Editor.KitBoxV2.Core;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.TableGroup
{
    /// <summary>
    /// 表格工具分组
    /// </summary>
    public class TableGroup : KitGroupBase
    {
        public override string Title => "表格工具";
        public override string GroupName => nameof(TableGroup);
        
        private readonly IKitPageV2[] _pages =
        {
            new CsvEditViewLitePage(),
            new SelectionToCsvPage(),
        };
        
        public override IKitPageV2[] Pages => _pages;
    }
}
