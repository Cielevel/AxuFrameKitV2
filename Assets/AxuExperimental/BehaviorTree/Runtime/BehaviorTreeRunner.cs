using System.Collections;
using UnityEngine;

namespace AxuExperimental.BehaviorTree.Runtime
{
    /// <summary>
    /// Component to run a BehaviorTree on a GameObject.
    /// Attach this to any GameObject and assign a BehaviorTree asset.
    /// </summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [Header("Behavior Tree Settings")]
        [Tooltip("The behavior tree asset to run")]
        public BehaviorTree tree;
        
        [Tooltip("Update mode for the behavior tree")]
        public UpdateMode updateMode = UpdateMode.Update;
        
        [Tooltip("If true, clone the tree at runtime to avoid modifying the original asset")]
        public bool cloneTree = true;
        
        public enum UpdateMode
        {
            Update,         // Run every frame in Update
            FixedUpdate,    // Run in FixedUpdate (physics timestep)
            Manual          // Manual control via RunOnce() or Tick()
        }
        
        /// <summary>
        /// The runtime tree instance (cloned or original)
        /// </summary>
        public BehaviorTree RuntimeTree { get; private set; }
        
        /// <summary>
        /// Current state of the behavior tree
        /// </summary>
        public NodeState TreeState => RuntimeTree != null ? RuntimeTree.treeState : NodeState.Failure;
        
        /// <summary>
        /// Is the tree currently running?
        /// </summary>
        public bool IsRunning => TreeState == NodeState.Running;
        
        private void Awake()
        {
            if (tree != null)
            {
                InitializeTree();
            }
        }
        
        private void OnEnable()
        {
            // Reset tree state when enabled
            if (RuntimeTree != null && RuntimeTree.rootNode != null)
            {
                RuntimeTree.rootNode.started = false;
                RuntimeTree.treeState = NodeState.Running;
            }
        }
        
        /// <summary>
        /// Initialize the runtime tree instance
        /// </summary>
        public void InitializeTree()
        {
            if (tree == null)
            {
                Debug.LogWarning($"[BehaviorTreeRunner] No tree assigned on {gameObject.name}");
                return;
            }
            
            if (cloneTree)
            {
                RuntimeTree = tree.Clone();
            }
            else
            {
                RuntimeTree = tree;
            }
        }
        
        /// <summary>
        /// Set a new behavior tree at runtime
        /// </summary>
        public void SetTree(BehaviorTree newTree, bool clone = true)
        {
            tree = newTree;
            cloneTree = clone;
            InitializeTree();
        }
        
        private void Update()
        {
            if (updateMode == UpdateMode.Update)
            {
                Tick();
            }
        }
        
        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
            {
                Tick();
            }
        }
        
        /// <summary>
        /// Run one tick of the behavior tree
        /// </summary>
        public NodeState Tick()
        {
            if (RuntimeTree == null)
            {
                return NodeState.Failure;
            }
            
            return RuntimeTree.Update();
        }
        
        /// <summary>
        /// Run the behavior tree once until it completes (Success or Failure)
        /// </summary>
        public IEnumerator RunOnce()
        {
            if (RuntimeTree == null)
            {
                yield break;
            }
            
            // Reset tree state
            RuntimeTree.rootNode.started = false;
            RuntimeTree.treeState = NodeState.Running;
            
            while (RuntimeTree.treeState == NodeState.Running)
            {
                RuntimeTree.Update();
                yield return null;
            }
        }
        
        /// <summary>
        /// Start running the tree once as a coroutine
        /// </summary>
        public Coroutine StartRunOnce()
        {
            return StartCoroutine(RunOnce());
        }
        
        /// <summary>
        /// Reset the tree to allow running again
        /// </summary>
        public void ResetTree()
        {
            if (RuntimeTree != null && RuntimeTree.rootNode != null)
            {
                // Reset all nodes
                RuntimeTree.Traverse(RuntimeTree.rootNode, node =>
                {
                    node.started = false;
                    node.state = NodeState.Running;
                });
                
                RuntimeTree.treeState = NodeState.Running;
            }
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Optional: Draw debug info in scene view
            if (RuntimeTree != null && Application.isPlaying)
            {
                // Could draw current state indicator here
            }
        }
#endif
    }
}
