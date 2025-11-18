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
            _ => InSine,
        };
    }
}