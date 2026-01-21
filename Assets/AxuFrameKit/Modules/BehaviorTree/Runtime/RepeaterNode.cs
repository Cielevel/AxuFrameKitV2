using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.BehaviorTree
{
    public class RepeaterNode : DecoratorNode
    {
        protected override void OnStart()
        {
            
        }

        protected override void OnStop()
        {
            
        }

        protected override NodeState OnUpdate()
        {
            child.Update();
            return NodeState.Running;
        }
    }
}
