
namespace AxuFrameKit.Common.Utility.Misc
{
    /// <summary>
    /// 默认的unity输入系统，其中的常量为默认的值（且只记录了键鼠的操作）；如果在PlayerSetting中修改了一些值，不要使用该类
    /// </summary>
    public static class DefaultInputPreset
    {
        public const string Horizontal = "Horizontal";
        
        public const string Vertical = "Vertical";

        public const string Jump = "Jump";

        public const string MouseX = "Mouse X";
        
        public const string MouseY = "Mouse Y";
        
        public const string MouseScrollWheel = "Mouse ScrollWheel";
    }
}