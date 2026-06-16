using System;

namespace AxuFrameKit._Core.Advanced.BetterFSM
{
    /// <summary>
    /// 条件谓词接口，用于状态机转换条件的评估
    /// </summary>
    public interface IPredicate
    {
        /// <summary>
        /// 评估条件是否满足
        /// </summary>
        /// <returns>如果条件满足返回 true，否则返回 false</returns>
        bool Evaluate();
    }

    /// <summary>
    /// 基于委托的函数谓词实现
    /// </summary>
    public class FuncPredicate : IPredicate
    {
        private readonly Func<bool> _func;

        /// <summary>
        /// 初始化函数谓词
        /// </summary>
        /// <param name="func">返回布尔值的条件函数</param>
        public FuncPredicate(Func<bool> func) => _func = func;

        /// <summary>
        /// 执行并返回嵌套函数的结果
        /// </summary>
        /// <returns>函数的评估结果</returns>
        public bool Evaluate() => _func.Invoke();
    }
}