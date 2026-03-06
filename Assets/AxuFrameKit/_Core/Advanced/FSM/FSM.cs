using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.FSM
{
    /// <summary>
    /// 泛型状态接口，适用于特定持有者类型
    /// </summary>
    /// <typeparam name="T">持有者类型</typeparam>
    public interface IState<in T>
    {
        /// <summary>
        /// 进入该状态时执行
        /// </summary>
        /// <param name="owner">状态机持有者</param>
        void OnEnter(T owner);

        /// <summary>
        /// 每帧更新状态逻辑
        /// </summary>
        /// <param name="owner">状态机持有者</param>
        void OnUpdate(T owner);

        /// <summary>
        /// 退出该状态时执行
        /// </summary>
        /// <param name="owner">状态机持有者</param>
        void OnExit(T owner);
    }

    /// <summary>
    /// 泛型状态基类，提供默认的虚方法实现
    /// </summary>
    /// <typeparam name="T">持有者类型</typeparam>
    public abstract class StateBase<T> : IState<T>
    {
        /// <summary>
        /// 进入逻辑
        /// </summary>
        public virtual void OnEnter(T owner) { }

        /// <summary>
        /// 更新逻辑
        /// </summary>
        public virtual void OnUpdate(T owner) { }

        /// <summary>
        /// 退出逻辑
        /// </summary>
        public virtual void OnExit(T owner) { }
    }

    /// <summary>
    /// 基于持有者模式的泛型状态机
    /// </summary>
    /// <typeparam name="T">持有者类型</typeparam>
    public class StateMachine<T>
    {
        /// <summary>
        /// 该状态机管理的持有者实例
        /// </summary>
        public T Owner { get; private set; }

        /// <summary>
        /// 当前活跃的状态
        /// </summary>
        public IState<T> CurrentState { get; private set; }

        /// <summary>
        /// 前一个状态的历史记录
        /// </summary>
        public IState<T> PreviousState { get; private set; }

        /// <summary>
        /// 全局状态，不受普通状态切换影响，始终运行
        /// </summary>
        public IState<T> GlobalState { get; private set; }

        /// <summary>
        /// 初始化状态机
        /// </summary>
        /// <param name="owner">关联的持有者</param>
        public StateMachine(T owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 设置全局运行的状态
        /// </summary>
        /// <param name="state">全局状态实例</param>
        public void SetGlobalState(IState<T> state)
        {
            GlobalState = state;
            GlobalState?.OnEnter(Owner);
        }

        /// <summary>
        /// 切换到新状态
        /// </summary>
        /// <param name="newState">要进入的目标状态</param>
        public void ChangeState(IState<T> newState)
        {
            if (newState == null) return;

            CurrentState?.OnExit(Owner);

            PreviousState = CurrentState;
            CurrentState = newState;

            CurrentState.OnEnter(Owner);
        }

        /// <summary>
        /// 回退到之前的历史状态
        /// </summary>
        public void RevertToPreviousState()
        {
            ChangeState(PreviousState);
        }

        /// <summary>
        /// 每帧更新逻辑
        /// </summary>
        public void Update()
        {
            GlobalState?.OnUpdate(Owner);
            CurrentState?.OnUpdate(Owner);
        }

        /// <summary>
        /// 清理资源并重置状态机
        /// </summary>
        public void Clear()
        {
            CurrentState?.OnExit(Owner);
            GlobalState?.OnExit(Owner);
            CurrentState = null;
            GlobalState = null;
            PreviousState = null;
        }
    }
}
