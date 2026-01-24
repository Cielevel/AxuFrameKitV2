using System;

namespace AxuFrameKit.ExpirementalNodeSystem
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NodeInputAttribute : Attribute
    {
        public enum ConnectionType { Multiple, Override }
        public ConnectionType connectionType;

        public NodeInputAttribute(ConnectionType connectionType = ConnectionType.Multiple)
        {
            this.connectionType = connectionType;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class NodeOutputAttribute : Attribute
    {
        // Output typically allows multiple connections
        public NodeOutputAttribute()
        {
        }
    }
}
