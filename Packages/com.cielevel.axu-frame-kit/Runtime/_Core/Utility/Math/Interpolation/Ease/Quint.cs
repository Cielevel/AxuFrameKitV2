namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Quint 缓动函数使用 t^5 曲线，比 Quart 更急剧。
    
    public class InQuint : IEase
    {
        public float Evaluate(float t) => t * t * t * t * t;
    }

    public class OutQuint : IEase
    {
        public float Evaluate(float t)
        {
            t -= 1f;
            return t * t * t * t * t + 1f;
        }
    }

    public class InOutQuint : IEase
    {
        public float Evaluate(float t)
        {
            t *= 2f;
            if (t < 1f) return 0.5f * t * t * t * t * t;
            t -= 2f;
            return 0.5f * (t * t * t * t * t + 2f);
        }
    }
}