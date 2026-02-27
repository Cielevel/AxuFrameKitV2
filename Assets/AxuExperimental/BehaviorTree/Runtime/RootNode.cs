using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    public class RootNode : Node
    {
        [HideInInspector] public Node child;

        protected override void OnStart()
        {

        }

        protected override void OnStop()
        {

        }

        protected override NodeState OnUpdate()
        {
            if (child)
            {
                return child.Update();
            }
            return NodeState.Failure;
        }

        public override Node Clone()
        {
            RootNode node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
            }
            return node;
        }
    }
}
