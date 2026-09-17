using System;
using System.Collections.Generic;

namespace AxuFrameKit._Core.Advanced.BetterFSM
{
    /// <summary>
    /// 高级状态机系统，支持基于谓词的条件转换 (BetterFSM)
    /// </summary>
    public class StateMachine
    {
        private StateNode _current;
        private Dictionary<Type, StateNode> _nodes = new();
        private HashSet<ITransition> _anyTransitions = new();

        /// <summary>
        /// 每帧更新状态机逻辑
        /// </summary>
        public void Update()
        {
            var transition = GetTransition();
            if (transition != null)
                ChangeState(transition.To);

            _current.State?.Update();
        }

        /// <summary>
        /// 每物理帧更新状态机逻辑
        /// </summary>
        public void FixedUpdate()
        {
            _current.State?.FixedUpdate();
        }

        /// <summary>
        /// 设置当前的初始状态
        /// </summary>
        /// <param name="state">目标状态实例</param>
        public void SetState(IState state)
        {
            _current = GetOrAddNode(state);
            _current.State?.OnEnter();
        }

        /// <summary>
        /// 执行状态切换逻辑
        /// </summary>
        /// <param name="state">目标状态</param>
        private void ChangeState(IState state)
        {
            if (state == _current.State) return;

            var previousState = _current.State;
            var nextNode = GetOrAddNode(state);

            previousState?.OnExit();
            nextNode.State?.OnEnter();
            _current = nextNode;
        }

        /// <summary>
        /// 检查转换条件并获取第一个满足条件的转换
        /// </summary>
        /// <returns>待执行的转换规则，若无则返回 null</returns>
        private ITransition GetTransition() // ! 注意foreach在update中的性能问题
        {
            foreach (var transition in _anyTransitions)
                if (transition.Condition.Evaluate())
                    return transition;

            foreach (var transition in _current.Transitions)
                if (transition.Condition.Evaluate())
                    return transition;

            return null;
        }

        /// <summary>
        /// 添加两个状态之间的转换关系
        /// </summary>
        /// <param name="from">起始状态</param>
        /// <param name="to">目标状态</param>
        /// <param name="condition">满足转换的条件谓词</param>
        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }

        /// <summary>
        /// 添加从任意状态跳转至指定状态的转换规则
        /// </summary>
        /// <param name="to">目标状态</param>
        /// <param name="condition">满足转换的条件谓词</param>
        public void AddAnyTransition(IState to, IPredicate condition)
        {
            _anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
        }

        /// <summary>
        /// 获取状态对应的节点，若不存在则创建
        /// </summary>
        /// <param name="state">关联的状态</param>
        /// <returns>封装了转换列表的状态节点</returns>
        private StateNode GetOrAddNode(IState state)
        {
            var node = _nodes.GetValueOrDefault(state.GetType());

            if (node == null)
            {
                node = new StateNode(state);
                _nodes.Add(state.GetType(), node);
            }

            return node;
        }

        private class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }

            public StateNode(IState state)
            {
                State = state;
                Transitions = new HashSet<ITransition>();
            }

            public void AddTransition(IState to, IPredicate condition)
            {
                Transitions.Add(new Transition(to, condition));
            }
        }
    }
}