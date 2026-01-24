using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace AxuFrameKit.ExpirementalNodeSystem
{
    public abstract class Node : ScriptableObject
    {
        [SerializeField, HideInInspector]
        public NodeGraph graph;

        [System.Serializable]
        public class NodePortData
        {
            public string fieldName;
            public List<NodeConnection> connections = new List<NodeConnection>();
        }

        [SerializeField, HideInInspector]
        private List<NodePortData> serializedPorts = new List<NodePortData>();

        private Dictionary<string, NodePort> _ports = new Dictionary<string, NodePort>();

        protected virtual void OnEnable()
        {
            UpdatePorts();
        }

        public void UpdatePorts()
        {
            var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                bool isInput = field.GetCustomAttributes(typeof(NodeInputAttribute), true).Length > 0;
                bool isOutput = field.GetCustomAttributes(typeof(NodeOutputAttribute), true).Length > 0;

                if (isInput || isOutput)
                {
                    if (!_ports.ContainsKey(field.Name))
                    {
                        _ports.Add(field.Name, new NodePort(field.Name, this, isInput));
                    }
                }
            }
        }

        public NodePort GetPort(string name)
        {
            if (!_ports.ContainsKey(name)) UpdatePorts(); // Ensure ports are loaded
            if (_ports.ContainsKey(name)) return _ports[name];
            return null;
        }

        public virtual object GetValue(NodePort port)
        {
            return null;
        }

        public T GetInputValue<T>(string portName, T fallback = default(T))
        {
            NodePort port = GetPort(portName);
            if (port != null && port.IsInput)
            {
                if (port.ConnectionCount > 0)
                {
                    var conn = port.GetConnection(0);
                    if (conn.node != null)
                    {
                         NodePort otherPort = conn.node.GetPort(conn.portName);
                         if (otherPort != null)
                         {
                             object val = conn.node.GetValue(otherPort);
                             if (val is T tVal) return tVal;
                             // Basic type conversion support could go here
                         }
                    }
                }
            }
            return fallback;
        }

        public int GetConnectionCount(string fieldName)
        {
            var data = GetPortData(fieldName);
            return data != null ? data.connections.Count : 0;
        }

        public NodeConnection GetConnection(string fieldName, int index)
        {
             var data = GetPortData(fieldName);
             if (data != null && index < data.connections.Count) return data.connections[index];
             return default(NodeConnection);
        }

        public List<NodeConnection> GetConnections(string fieldName)
        {
            var data = GetPortData(fieldName);
            return data != null ? data.connections : new List<NodeConnection>();
        }

        public void Connect(NodePort myPort, NodePort otherPort)
        {
            AddConnection(myPort.fieldName, otherPort.node, otherPort.fieldName);

            if (otherPort.node != this)
            {
                 otherPort.node.AddConnection(otherPort.fieldName, this, myPort.fieldName);
            }
        }

        public void AddConnection(string fieldName, Node otherNode, string otherPortName)
        {
             var data = GetOrCreatePortData(fieldName);
             if (!data.connections.Exists(c => c.node == otherNode && c.portName == otherPortName))
             {
                 data.connections.Add(new NodeConnection { node = otherNode, portName = otherPortName });
             }
        }

        public void Disconnect(NodePort myPort, NodePort otherPort)
        {
             RemoveConnection(myPort.fieldName, otherPort.node, otherPort.fieldName);

             if (otherPort.node != this)
             {
                 otherPort.node.RemoveConnection(otherPort.fieldName, this, myPort.fieldName);
             }
        }

        public void RemoveConnection(string fieldName, Node otherNode, string otherPortName)
        {
             var data = GetPortData(fieldName);
             if (data != null)
             {
                 data.connections.RemoveAll(c => c.node == otherNode && c.portName == otherPortName);
             }
        }

        private NodePortData GetPortData(string name)
        {
            return serializedPorts.Find(p => p.fieldName == name);
        }

        private NodePortData GetOrCreatePortData(string name)
        {
            var data = GetPortData(name);
            if (data == null)
            {
                data = new NodePortData { fieldName = name };
                serializedPorts.Add(data);
            }
            return data;
        }
    }
}
