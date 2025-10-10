namespace AxuFrameKit.Common.Utility.UseMono
{
    // 每个继承IUseMonoBehaviour的类“实例”，都共用同一个MonoBehaviour脚本
    // 通过这个脚本，可以使用协程，Update等Unity特性
    
    /// <summary>
    /// 每个实例共用一个生命周期
    /// </summary>
    public interface IUseMonoBehaviourCommon { }
}