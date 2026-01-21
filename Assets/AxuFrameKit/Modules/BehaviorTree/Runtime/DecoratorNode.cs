using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.BehaviorTree
{
    public abstract class DecoratorNode : Node
    {
        [HideInInspector] public Node child;

        public override Node Clone()
        {
            DecoratorNode node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
            }
            return node;
        }
    }
}
