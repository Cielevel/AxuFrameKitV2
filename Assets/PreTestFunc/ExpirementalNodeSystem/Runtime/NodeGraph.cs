using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.ExpirementalNodeSystem
{
    public abstract class NodeGraph : ScriptableObject
    {
        public List<Node> nodes = new List<Node>();

        public T AddNode<T>() where T : Node
        {
            return AddNode(typeof(T)) as T;
        }

        public virtual Node AddNode(System.Type type)
        {
            Node node = ScriptableObject.CreateInstance(type) as Node;
            node.graph = this;
            nodes.Add(node);
            node.name = type.Name;
            return node;
        }

        public virtual void RemoveNode(Node node)
        {
            nodes.Remove(node);
        }
    }
}
