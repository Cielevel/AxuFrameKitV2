using UnityEngine;

namespace TestAndSample._Sample_Inner.Sample_AnimationState
{
    public class SampleAnimationStateBigger : StateMachineBehaviour
    {
        public Color colorEnter;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            
            animator.gameObject.GetComponent<Renderer>().material.color = colorEnter;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnStateExit(animator, stateInfo, layerIndex);
        }
    }
}