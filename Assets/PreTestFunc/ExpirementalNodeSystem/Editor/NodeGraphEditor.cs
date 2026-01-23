using UnityEngine;
using UnityEditor;
using AxuFrameKit.ExpirementalNodeSystem;
using System.Linq;

namespace AxuFrameKit.ExpirementalNodeSystem.Editor
{
    [CustomEditor(typeof(NodeGraph), true)]
    public class NodeGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            NodeGraph graph = (NodeGraph)target;

            GUILayout.Label($"Nodes: {graph.nodes.Count}", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Node"))
            {
                GenericMenu menu = new GenericMenu();

                var nodeTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(Node)) && !t.IsAbstract);

                foreach(var type in nodeTypes)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () => {
                        CreateNode(graph, type);
                    });
                }

                menu.ShowAsContext();
            }

            // List nodes
            for(int i = 0; i < graph.nodes.Count; i++)
            {
                var node = graph.nodes[i];
                if (node == null)
                {
                    // Clean up null nodes
                    graph.nodes.RemoveAt(i);
                    i--;
                    continue;
                }

                GUILayout.BeginHorizontal("box");
                GUILayout.Label(node.name);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    RemoveNode(graph, node);
                    i--;
                }
                GUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CreateNode(NodeGraph graph, System.Type type)
        {
            Node node = graph.AddNode(type);
            node.name = type.Name;

            if (EditorUtility.IsPersistent(graph))
            {
                AssetDatabase.AddObjectToAsset(node, graph);
                AssetDatabase.SaveAssets();
            }
        }

        void RemoveNode(NodeGraph graph, Node node)
        {
            graph.RemoveNode(node);
            if (EditorUtility.IsPersistent(graph))
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                Object.DestroyImmediate(node, true);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Object.DestroyImmediate(node);
            }
        }
    }
}
