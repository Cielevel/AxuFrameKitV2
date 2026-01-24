using UnityEngine;
using AxuFrameKit.ExpirementalNodeSystem;

namespace AxuFrameKit.ExpirementalNodeSystem.Samples
{
    public class MathNode : Node
    {
        public enum MathType { Add, Subtract, Multiply, Divide }
        public MathType mathType = MathType.Add;

        [NodeInput] public float a;
        [NodeInput] public float b;
        [NodeOutput] public float result;

        public override object GetValue(NodePort port)
        {
            float valA = GetInputValue<float>("a", this.a);
            float valB = GetInputValue<float>("b", this.b);

            if (port.fieldName == "result")
            {
                switch (mathType)
                {
                    case MathType.Add: return valA + valB;
                    case MathType.Subtract: return valA - valB;
                    case MathType.Multiply: return valA * valB;
                    case MathType.Divide: return valB != 0 ? valA / valB : 0;
                    default: return 0f;
                }
            }
            return 0f;
        }
    }
}
