namespace AxuExperimental.BehaviorTree.Runtime
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
