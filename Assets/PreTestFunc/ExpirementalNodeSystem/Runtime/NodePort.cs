using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.ExpirementalNodeSystem
{
    [System.Serializable]
    public struct NodeConnection
    {
        public Node node;
        public string portName;
    }

    public class NodePort
    {
        public string fieldName;
        public Node node;
        public bool IsInput { get; private set; }

        public NodePort(string fieldName, Node node, bool isInput)
        {
            this.fieldName = fieldName;
            this.node = node;
            this.IsInput = isInput;
        }

        public int ConnectionCount
        {
            get { return node.GetConnectionCount(fieldName); }
        }

        public NodeConnection GetConnection(int index)
        {
            return node.GetConnection(fieldName, index);
        }

        public List<NodeConnection> GetConnections()
        {
            return node.GetConnections(fieldName);
        }

        public void Connect(NodePort other)
        {
            if (other == null) return;
            node.Connect(this, other);
        }

        public void Disconnect(NodePort other)
        {
            if (other == null) return;
            node.Disconnect(this, other);
        }

        public object GetValue()
        {
            return node.GetValue(this);
        }
    }
}
