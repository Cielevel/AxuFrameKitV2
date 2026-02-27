using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    public class WaitNode : ActionNode
    {
        public float duration = 1;
        float startTime;

        protected override void OnStart()
        {
            startTime = Time.time;
        }

        protected override void OnStop()
        {
            
        }

        protected override NodeState OnUpdate()
        {
            if (Time.time - startTime > duration)
            {
                return NodeState.Success;
            }
            return NodeState.Running;
        }
    }
}
