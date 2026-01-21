using AxuFrameKit._Core.Advanced.BetterFSM;
using UnityEngine;

namespace TestAndSample.Sample_Core.Sample_Core_BetterFSM
{
    public class BetterFSMUsage : MonoBehaviour
    {
        private StateMachine _stateMachine;

        /*
        betterFSM的逻辑结构类似于Unity Animator，状态与状态直接的联系是确定的，有转换条件。
        */

        private void Init()
        {
            _stateMachine = new StateMachine();

            IState idle = new IdleState(),
                walk = new WalkingState(),
                dash = new DashState(),
                mine = new MineState();

            #region Transition
            // 数据变化-->状态变化（数据驱动状态变化，有一定局限性：数据一般是内部变动，外部对一个内部数据的影响是一定有限的）
            // ! 只是示例，不要复制，这里会产生闭包。
            float moveSpeed = 0;
            bool isDash = false;
            bool isMine = false;

            // ? 条件自定义，主要是数据驱动状态

            // moveSpeed大于0时，开始walk
            _stateMachine.AddTransition(idle, walk, new FuncPredicate(() => moveSpeed > 0));

            // idle和walk状态均可dash
            _stateMachine.AddTransition(idle, dash, new FuncPredicate(() => ((IdleState)idle).IsFinished));
            _stateMachine.AddTransition(walk, dash, new FuncPredicate(() => isDash));
            _stateMachine.AddTransition(dash, idle, new FuncPredicate(() => !isDash && moveSpeed <= 0));
            _stateMachine.AddTransition(dash, walk, new FuncPredicate(() => !isDash && moveSpeed > 0));

            // idle和walk状态可以进入挖矿状态，dash时不行
            _stateMachine.AddTransition(idle, mine, new FuncPredicate(() => isMine));
            _stateMachine.AddTransition(walk, mine, new FuncPredicate(() => isMine));
            _stateMachine.AddTransition(mine, idle, new FuncPredicate(() => !isMine && moveSpeed <= 0));
            _stateMachine.AddTransition(mine, walk, new FuncPredicate(() => !isMine && moveSpeed > 0));
            #endregion

            _stateMachine.SetState(idle);
        }
    }

    public class IdleState : IState
    {
        public void FixedUpdate() { }
        public void OnEnter() { }
        public void OnExit() { }
        public void Update() { }

        public bool IsFinished { get; private set; }
    }

    public class WalkingState : IState
    {
        public void FixedUpdate() { }
        public void OnEnter() { }
        public void OnExit() { }
        public void Update() { }
    }

    public class DashState : IState
    {
        public void FixedUpdate() { }
        public void OnEnter() { }
        public void OnExit() { }
        public void Update() { }
    }

    public class MineState : IState // 挖矿
    {
        public void FixedUpdate() { }
        public void OnEnter() { }
        public void OnExit() { }
        public void Update() { }
    }
}