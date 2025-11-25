using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Expo缓动使用 2^10(t-1) 曲线，加速非常快。
    
    public class InExpo : IEase
    {
        public float Evaluate(float t) => MathF.Abs(t) < float.Epsilon ? 0f : MathF.Pow(2f, 10f * (t - 1f));
    }

    public class OutExpo : IEase
    {
        public float Evaluate(float t) => MathF.Abs(t - 1f) < float.Epsilon ? 1f : -MathF.Pow(2f, -10f * t) + 1f;
    }

    public class InOutExpo : IEase
    {
        public float Evaluate(float t)
        {
            if (MathF.Abs(t) < float.Epsilon) return 0f;
            if (MathF.Abs(t - 1f) < float.Epsilon) return 1f;
            t *= 2f;
            if (t < 1f) return 0.5f * MathF.Pow(2f, 10f * (t - 1f));
            return 0.5f * (-MathF.Pow(2f, -10f * (t - 1f)) + 2f);
        }
    }
}