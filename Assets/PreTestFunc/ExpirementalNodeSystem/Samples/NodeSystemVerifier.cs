using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using AxuFrameKit.ExpirementalNodeSystem;

namespace AxuFrameKit.ExpirementalNodeSystem.Samples
{
    public class NodeSystemVerifier : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("AxuFrameKit/NodeSystem/Verify")]
        public static void Verify()
        {
            Debug.Log("Starting Node System Verification...");

            // Create Graph
            SimpleGraph graph = ScriptableObject.CreateInstance<SimpleGraph>();

            // Create Nodes
            MathNode nodeA = graph.AddNode<MathNode>();
            nodeA.mathType = MathNode.MathType.Add;
            nodeA.a = 5f;
            nodeA.b = 3f;

            MathNode nodeB = graph.AddNode<MathNode>();
            nodeB.mathType = MathNode.MathType.Multiply;
            nodeB.b = 2f;

            // Manually update ports
            nodeA.UpdatePorts();
            nodeB.UpdatePorts();

            // Connect nodeA.result -> nodeB.a
            NodePort outputPort = nodeA.GetPort("result");
            NodePort inputPort = nodeB.GetPort("a");

            if (outputPort == null || inputPort == null)
            {
                Debug.LogError("Ports not found!");
                return;
            }

            inputPort.Connect(outputPort);

            // Calculate
            object finalResObj = nodeB.GetValue(nodeB.GetPort("result"));

            if (finalResObj is float val)
            {
                Debug.Log($"Result: {val}");
                // (5 + 3) * 2 = 16
                if (Mathf.Approximately(val, 16f))
                {
                    Debug.Log("Verification SUCCESS: Result is 16.");
                }
                else
                {
                    Debug.LogError($"Verification FAILED: Expected 16, got {val}");
                }
            }
            else
            {
                Debug.LogError("Verification FAILED: Result is not float or null");
            }

            Object.DestroyImmediate(graph);
            Object.DestroyImmediate(nodeA);
            Object.DestroyImmediate(nodeB);
        }
#endif
    }
}
