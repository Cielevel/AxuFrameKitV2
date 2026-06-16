using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    public class InCubic : IEase
    {
        public float Evaluate(float t) => t * t * t;
    }
    
    public class OutCubic : IEase
    {
        public float Evaluate(float t)
        {
            t -= 1f;
            return t * t * t + 1f;
        }
    }
    
    public class InOutCubic : IEase
    {
        public float Evaluate(float t) => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
}