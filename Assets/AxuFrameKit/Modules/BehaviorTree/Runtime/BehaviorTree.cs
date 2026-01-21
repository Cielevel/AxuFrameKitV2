using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AxuFrameKit.BehaviorTree
{
    [CreateAssetMenu(fileName = "NewBehaviorTree", menuName = "AxuFrameKit/BehaviorTree/Tree")]
    public class BehaviorTree : ScriptableObject
    {
        public Node rootNode;
        public NodeState treeState = NodeState.Running;
        public List<Node> nodes = new List<Node>();

        public NodeState Update()
        {
            if (rootNode.state == NodeState.Running)
            {
                treeState = rootNode.Update();
            }
            return treeState;
        }

        public BehaviorTree Clone()
        {
            BehaviorTree tree = Instantiate(this);
            tree.rootNode = tree.rootNode.Clone();
            tree.nodes = new List<Node>();
            Traverse(tree.rootNode, (n) =>
            {
                tree.nodes.Add(n);
            });
            return tree;
        }

        public void Traverse(Node node, System.Action<Node> visitor)
        {
            if (node)
            {
                visitor.Invoke(node);
                var children = GetChildren(node);
                children.ForEach((n) => Traverse(n, visitor));
            }
        }

        public List<Node> GetChildren(Node parent)
        {
            List<Node> children = new List<Node>();

            if (parent is RootNode root && root.child != null)
            {
                children.Add(root.child);
            }
            else if (parent is DecoratorNode decorator && decorator.child != null)
            {
                children.Add(decorator.child);
            }
            else if (parent is CompositeNode composite)
            {
                return composite.children;
            }

            return children;
        }

#if UNITY_EDITOR
        public Node CreateNode(System.Type type)
        {
            Node node = ScriptableObject.CreateInstance(type) as Node;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            
            Undo.RecordObject(this, "Behavior Tree (CreateNode)");
            nodes.Add(node);

            if (!Application.isPlaying)
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }
            
            Undo.RegisterCreatedObjectUndo(node, "Behavior Tree (CreateNode)");
            AssetDatabase.SaveAssets();
            return node;
        }

        public void DeleteNode(Node node)
        {
            Undo.RecordObject(this, "Behavior Tree (DeleteNode)");
            nodes.Remove(node);
            
            // AssetDatabase.RemoveObjectFromAsset(node); // Not immediately necessary if using Undo.DestroyObjectImmediate
            Undo.DestroyObjectImmediate(node);
            
            AssetDatabase.SaveAssets();
        }

        public void AddChild(Node parent, Node child)
        {
            if (parent is RootNode root)
            {
                Undo.RecordObject(root, "Behavior Tree (AddChild)");
                root.child = child;
                EditorUtility.SetDirty(root);
            }
            else if (parent is DecoratorNode decorator)
            {
                Undo.RecordObject(decorator, "Behavior Tree (AddChild)");
                decorator.child = child;
                EditorUtility.SetDirty(decorator);
            }
            else if (parent is CompositeNode composite)
            {
                Undo.RecordObject(composite, "Behavior Tree (AddChild)");
                composite.children.Add(child);
                EditorUtility.SetDirty(composite);
            }
        }

        public void RemoveChild(Node parent, Node child)
        {
            if (parent is RootNode root)
            {
                Undo.RecordObject(root, "Behavior Tree (RemoveChild)");
                root.child = null;
                EditorUtility.SetDirty(root);
            }
            else if (parent is DecoratorNode decorator)
            {
                Undo.RecordObject(decorator, "Behavior Tree (RemoveChild)");
                decorator.child = null;
                EditorUtility.SetDirty(decorator);
            }
            else if (parent is CompositeNode composite)
            {
                Undo.RecordObject(composite, "Behavior Tree (RemoveChild)");
                composite.children.Remove(child);
                EditorUtility.SetDirty(composite);
            }
        }
#endif
    }
}
