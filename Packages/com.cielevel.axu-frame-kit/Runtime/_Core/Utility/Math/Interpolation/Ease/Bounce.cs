namespace AxuFrameKit._Core.Utility.Math.Interpolation.Ease
{
    // Bounce 缓动模拟物体落地弹跳的效果，通过分段函数实现。
    public class InBounce : IEase
    {
        public float Evaluate(float t) => 1f - new OutBounce().Evaluate(1f - t);
    }

    public class OutBounce : IEase
    {
        public float Evaluate(float t)
        {
            if (t < (1f / 2.75f))
            {
                return 7.5625f * t * t;
            }

            if (t < (2f / 2.75f))
            {
                t -= (1.5f / 2.75f);
                return 7.5625f * t * t + 0.75f;
            }

            if (t < (2.5f / 2.75f))
            {
                t -= (2.25f / 2.75f);
                return 7.5625f * t * t + 0.9375f;
            }

            t -= (2.625f / 2.75f);
            return 7.5625f * t * t + 0.984375f;
        }
    }

    public class InOutBounce : IEase
    {
        public float Evaluate(float t)
        {
            if (t < 0.5f)
                return new InBounce().Evaluate(t * 2f) * 0.5f;
            
            return new OutBounce().Evaluate(t * 2f - 1f) * 0.5f + 0.5f;
        }
    }
}