using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    public class DebugLogNode : ActionNode
    {
        public string message = "Hello World";

        protected override void OnStart()
        {
            Debug.Log($"OnStart: {message}");
        }

        protected override void OnStop()
        {
            Debug.Log($"OnStop: {message}");
        }

        protected override NodeState OnUpdate()
        {
            Debug.Log($"OnUpdate: {message}");
            return NodeState.Success;
        }
    }
}
