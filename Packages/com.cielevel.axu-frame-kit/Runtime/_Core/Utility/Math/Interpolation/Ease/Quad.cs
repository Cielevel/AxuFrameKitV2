using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    public class InQuad : IEase
    {
        public float Evaluate(float t) => t * t;
    }
    
    public class OutQuad : IEase
    {
        public float Evaluate(float t) => t * (2 - t);
    }
    
    public class InOutQuad : IEase
    {
        public float Evaluate(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) * 0.5f;
    }
}