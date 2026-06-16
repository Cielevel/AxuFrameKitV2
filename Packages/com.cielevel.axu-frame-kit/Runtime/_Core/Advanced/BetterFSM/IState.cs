namespace AxuFrameKit._Core.Advanced.BetterFSM
{
    /// <summary>
    /// 状态接口，定义状态机的基本行为周期
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 进入该状态时调用
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 状态更新逻辑，通常在 Update 中调用
        /// </summary>
        void Update();

        /// <summary>
        /// 物理更新逻辑，通常在 FixedUpdate 中调用
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// 退出该状态时调用
        /// </summary>
        void OnExit();
    }
}
