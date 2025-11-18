using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    public class InSine : IEase
    {
        public float Evaluate(float t) => 1f - MathF.Cos(t * MathF.PI * 0.5f);
    }

    public class OutSine : IEase
    {
        public float Evaluate(float t) => MathF.Sin(t * MathF.PI * 0.5f);
    }

    public class InOutSine : IEase
    {
        public float Evaluate(float t) => -0.5f * (MathF.Cos(MathF.PI * t) - 1f);
    }
}