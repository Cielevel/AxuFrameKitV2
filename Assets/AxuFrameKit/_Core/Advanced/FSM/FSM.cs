using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.FSM
{
    public interface IState<T>
    {
        void OnEnter(T owner);
        void OnUpdate(T owner);
        void OnExit(T owner);
    }

    public abstract class StateBase<T> : IState<T>
    {
        public virtual void OnEnter(T owner) { }
        public virtual void OnUpdate(T owner) { }
        public virtual void OnExit(T owner) { }
    }

    public class StateMachine<T>
    {
        public T Owner { get; private set; }
        public IState<T> CurrentState { get; private set; }
        public IState<T> PreviousState { get; private set; }

        // 全局状态（始终运行）
        public IState<T> GlobalState { get; private set; }

        public StateMachine(T owner)
        {
            Owner = owner;
        }

        public void SetGlobalState(IState<T> state)
        {
            GlobalState = state;
            GlobalState?.OnEnter(Owner);
        }

        public void ChangeState(IState<T> newState)
        {
            if (newState == null) return;

            // 可以在这里加一个 CheckTransition 逻辑

            CurrentState?.OnExit(Owner);

            PreviousState = CurrentState;
            CurrentState = newState;

            CurrentState.OnEnter(Owner);
        }

        public void RevertToPreviousState()
        {
            ChangeState(PreviousState);
        }

        public void Update()
        {
            GlobalState?.OnUpdate(Owner);
            CurrentState?.OnUpdate(Owner);
        }

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
