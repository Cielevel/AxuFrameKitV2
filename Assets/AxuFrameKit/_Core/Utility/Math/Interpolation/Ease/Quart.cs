namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Quart 缓动函数使用 t^4 曲线，比 Cubic 更急剧。
    
    public class InQuart : IEase
    {
        public float Evaluate(float t) => t * t * t * t;
    }

    public class OutQuart : IEase
    {
        public float Evaluate(float t)
        {
            t -= 1f;
            return -(t * t * t * t - 1f);
        }
    }

    public class InOutQuart : IEase
    {
        public float Evaluate(float t)
        {
            t *= 2f;
            if (t < 1f) return 0.5f * t * t * t * t;
            t -= 2f;
            return -0.5f * (t * t * t * t - 2f);
        }
    }
}