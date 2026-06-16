namespace AxuFrameKit._Core.Advanced.BetterFSM
{
    /// <summary>
    /// 状态转换接口，定义从当前状态转换到目标状态的规则
    /// </summary>
    public interface ITransition
    {
        /// <summary>
        /// 目标状态
        /// </summary>
        IState To { get; }

        /// <summary>
        /// 触发转换的谓词条件
        /// </summary>
        IPredicate Condition { get; }
    }

    /// <summary>
    /// 状态转换的默认实现
    /// </summary>
    public class Transition : ITransition
    {
        /// <summary>
        /// 目标状态
        /// </summary>
        public IState To { get; }

        /// <summary>
        /// 触发转换的谓词条件
        /// </summary>
        public IPredicate Condition { get; }

        /// <summary>
        /// 初始化转换规则
        /// </summary>
        /// <param name="to">目标状态</param>
        /// <param name="condition">触发条件</param>
        public Transition(IState to, IPredicate condition)
        {
            To = to;
            Condition = condition;
        }
    }
}