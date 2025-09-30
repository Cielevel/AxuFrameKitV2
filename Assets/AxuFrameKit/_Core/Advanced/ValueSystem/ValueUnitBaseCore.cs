
namespace AxuFrameKit._Core.Advanced.ValueSystem
{
    public enum ValueUnitRefreshType
    {
        Auto = 1, // 自动刷新
        Manual = 2, // 手动刷新
        Dirty = 3, // 脏值刷新
    }
    
    public abstract class ValueUnitBaseCore
    {
        public ValueUnitRefreshType RefreshType { get; private set; }
        
        public double Param { get; private set; }
    }
}
