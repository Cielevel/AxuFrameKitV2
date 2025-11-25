namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Back 缓动在开始或结束时会稍微超出范围，创建一种拉伸后回弹的效果。标准的超射量 Overshoot 为 1.70158.
    
    public class InBack : IEase
    {
        private const float s = 1.70158f;
        public float Evaluate(float t) => t * t * ((s + 1f) * t - s);
    }

    public class OutBack : IEase
    {
        private const float s = 1.70158f;
        public float Evaluate(float t)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }

    public class InOutBack : IEase
    {
        private const float s = 1.70158f * 1.525f;
        public float Evaluate(float t)
        {
            t *= 2f;
            if (t < 1f) return 0.5f * (t * t * ((s + 1f) * t - s));
            t -= 2f;
            return 0.5f * (t * t * ((s + 1f) * t + s) + 2f);
        }
    }
}