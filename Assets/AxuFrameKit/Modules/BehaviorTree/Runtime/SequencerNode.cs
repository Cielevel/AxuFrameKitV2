using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.BehaviorTree
{
    public class SequencerNode : CompositeNode
    {
        int current;

        protected override void OnStart()
        {
            current = 0;
        }

        protected override void OnStop()
        {
            
        }

        protected override NodeState OnUpdate()
        {
            var child = children[current];
            switch (child.Update())
            {
                case NodeState.Running:
                    return NodeState.Running;
                case NodeState.Failure:
                    return NodeState.Failure;
                case NodeState.Success:
                    current++;
                    break;
            }

            return current == children.Count ? NodeState.Success : NodeState.Running;
        }
    }
}
