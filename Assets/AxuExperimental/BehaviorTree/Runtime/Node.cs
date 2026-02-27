using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    public abstract class Node : ScriptableObject
    {
        [HideInInspector] public NodeState state = NodeState.Running;
        [HideInInspector] public bool started = false;
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;

        [TextArea]
        public string remark;

        public NodeState Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
            }

            state = OnUpdate();

            if (state == NodeState.Failure || state == NodeState.Success)
            {
                OnStop();
                started = false;
            }

            return state;
        }

        public virtual Node Clone()
        {
            return Instantiate(this);
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract NodeState OnUpdate();
    }
}
