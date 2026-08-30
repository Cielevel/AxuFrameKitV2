using System.Collections.Generic;
using AxuFrameKit._Core.Advanced.BetterFSM;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Core/Util] BetterFSM 一条状态流转：谓词不满足保持原状态、满足时按 OnExit → OnEnter → Update 流转。
    /// 注意：StateMachine 以 state.GetType() 为节点键（每类型一个节点），测试状态必须是不同类型。
    /// </summary>
    [Category("Axu/Core/Util")]
    public class CoreBetterFsmTests
    {
        /// <summary>记录生命周期回调顺序的状态基类；子类化以满足"按类型注册"约束。</summary>
        private abstract class RecordingState : IState
        {
            protected abstract string Name { get; }
            public readonly List<string> Log = new List<string>();

            public void OnEnter() => Log.Add("Enter:" + Name);
            public void Update() => Log.Add("Update:" + Name);
            public void FixedUpdate() { }
            public void OnExit() => Log.Add("Exit:" + Name);
        }

        private sealed class StateA : RecordingState { protected override string Name => "A"; }
        private sealed class StateB : RecordingState { protected override string Name => "B"; }

        [Test]
        public void StateMachine_谓词驱动流转_不满足保持满足则切换()
        {
            // Arrange：A --(flag 谓词)--> B（不同类型，才能各自成节点）
            var a = new StateA();
            var b = new StateB();
            var sm = new StateMachine();
            bool flag = false;
            sm.AddTransition(a, b, new FuncPredicate(() => flag));
            sm.SetState(a); // 进入 A

            // Act 1：谓词不满足
            sm.Update();

            // Assert：保持 A，仅 A.Update
            CollectionAssert.AreEqual(new[] { "Enter:A", "Update:A" }, a.Log);
            CollectionAssert.IsEmpty(b.Log);

            // Act 2：谓词满足
            flag = true;
            sm.Update();

            // Assert：A.OnExit → B.OnEnter → 同帧 B.Update
            CollectionAssert.AreEqual(new[] { "Enter:A", "Update:A", "Exit:A" }, a.Log);
            CollectionAssert.AreEqual(new[] { "Enter:B", "Update:B" }, b.Log);
        }
    }
}
