using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Circ缓动使用圆方程 sqrt{1 - t^2} 的一个象限，斜率变化平滑。
    
    public class InCirc : IEase
    {
        public float Evaluate(float t) => -(MathF.Sqrt(1f - t * t) - 1f);
    }

    public class OutCirc : IEase
    {
        public float Evaluate(float t)
        {
            t -= 1f;
            return MathF.Sqrt(1f - t * t);
        }
    }

    public class InOutCirc : IEase
    {
        public float Evaluate(float t)
        {
            t *= 2f;
            if (t < 1f) return -0.5f * (MathF.Sqrt(1f - t * t) - 1f);
            t -= 2f;
            return 0.5f * (MathF.Sqrt(1f - t * t) + 1f);
        }
    }
}