using AxuFrameKit._Core.Utility.Math.Interpolation.Ease;

namespace AxuFrameKit._Core.Utility.Math.Interpolation
{
    public static class EaseFactory
    {
        private static readonly IEase Linear = new Linear();
        
        private static readonly IEase InSine = new InSine();
        private static readonly IEase OutSine = new OutSine();
        private static readonly IEase InOutSine = new InOutSine();

        private static readonly IEase InQuad = new InQuad();
        private static readonly IEase OutQuad = new OutQuad();
        private static readonly IEase InOutQuad = new InOutQuad();

        private static readonly IEase InCubic = new InCubic();
        private static readonly IEase OutCubic = new OutCubic();
        private static readonly IEase InOutCubic = new InOutCubic();

        private static readonly IEase InQuart = new InQuart();
        private static readonly IEase OutQuart = new OutQuart();
        private static readonly IEase InOutQuart = new InOutQuart();
        
        private static readonly IEase InQuint = new InQuint();
        private static readonly IEase OutQuint = new OutQuint();
        private static readonly IEase InOutQuint = new InOutQuint();
        
        private static readonly IEase InExpo = new InExpo();
        private static readonly IEase OutExpo = new OutExpo();
        private static readonly IEase InOutExpo = new InOutExpo();
        
        private static readonly IEase InCirc = new InCirc();
        private static readonly IEase OutCirc = new OutCirc();
        private static readonly IEase InOutCirc = new InOutCirc();
        
        private static readonly IEase InBack = new InBack();
        private static readonly IEase OutBack = new OutBack();
        private static readonly IEase InOutBack = new InOutBack();
        
        private static readonly IEase InElastic = new InElastic();
        private static readonly IEase OutElastic = new OutElastic();
        private static readonly IEase InOutElastic = new InOutElastic();
        
        private static readonly IEase InBounce = new InBounce();
        private static readonly IEase OutBounce = new OutBounce();
        private static readonly IEase InOutBounce = new InOutBounce();
        
        public static IEase Get(EaseType easeType) => easeType switch
        {
            EaseType.Linear => Linear,
            EaseType.InSine => InSine,
            EaseType.OutSine => OutSine,
            EaseType.InOutSine => InOutSine,
            EaseType.InQuad => InQuad,
            EaseType.OutQuad => OutQuad,
            EaseType.InOutQuad => InOutQuad,
            EaseType.InCubic => InCubic,
            EaseType.OutCubic => OutCubic,
            EaseType.InOutCubic => InOutCubic,
            EaseType.InQuart => InQuart,
            EaseType.OutQuart => OutQuart,
            EaseType.InOutQuart => InOutQuart,
            EaseType.InQuint => InQuint,
            EaseType.OutQuint => OutQuint,
            EaseType.InOutQuint => InOutQuint,
            EaseType.InExpo => InExpo,
            EaseType.OutExpo => OutExpo,
            EaseType.InOutExpo => InOutExpo,
            EaseType.InCirc => InCirc,
            EaseType.OutCirc => OutCirc,
            EaseType.InOutCirc => InOutCirc,
            EaseType.InBack => InBack,
            EaseType.OutBack => OutBack,
            EaseType.InOutBack => InOutBack,
            EaseType.InElastic => InElastic,
            EaseType.OutElastic => OutElastic,
            EaseType.InOutElastic => InOutElastic,
            EaseType.InBounce => InBounce,
            EaseType.OutBounce => OutBounce,
            EaseType.InOutBounce => InOutBounce,
            _ => Linear,
        };
    }
}