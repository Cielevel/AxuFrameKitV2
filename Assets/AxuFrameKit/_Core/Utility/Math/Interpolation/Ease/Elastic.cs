using System;

namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Elastic 缓动模拟橡皮筋拉伸和振动的效果，在动画结束前会多次震荡。
    
    public class InElastic : IEase
    {
        // 标准参数: amplitude = 1f, period = 0.3f
        public float Evaluate(float t)
        {
            if (MathF.Abs(t) < float.Epsilon) return 0f;
            if (MathF.Abs(t - 1f) < float.Epsilon) return 1f;
            float p = 0.3f;
            float s = p / 4f; // 1f * MathF.Asin(1f) / (2f * MathF.PI) * p;

            t -= 1f;
            return -(MathF.Pow(2f, 10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p));
        }
    }

    public class OutElastic : IEase
    {
        // 标准参数: amplitude = 1f, period = 0.3f
        public float Evaluate(float t)
        {
            if (MathF.Abs(t) < float.Epsilon) return 0f;
            if (MathF.Abs(t - 1f) < float.Epsilon) return 1f;
            float p = 0.3f;
            float s = p / 4f; // 1f * MathF.Asin(1f) / (2f * MathF.PI) * p;
        
            return MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p) + 1f;
        }
    }

    public class InOutElastic : IEase
    {
        // 标准参数: amplitude = 1f, period = 0.45f
        public float Evaluate(float t)
        {
            if (MathF.Abs(t) < float.Epsilon) return 0f;
            t *= 2f;
            if (MathF.Abs(t - 2f) < float.Epsilon) return 1f;
            float p = 0.45f;
            float s = p / 4f; // 1f * MathF.Asin(1f) / (2f * MathF.PI) * p;

            if (t < 1f)
            {
                t -= 1f;
                return -0.5f * (MathF.Pow(2f, 10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p));
            }
            else
            {
                t -= 1f;
                return MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * MathF.PI) / p) * 0.5f + 1f;
            }
        }
    }
}