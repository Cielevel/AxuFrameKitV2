using System;

namespace AxuFrameKit.Common.Utility.Timer
{
    public static class CdTimer
    {
        private static ICdManager Manager => CdManager.Instance;

        public static void StartCd(float time, Action onComplete) => Manager.StartCd(time, onComplete);
        public static void StartCd(float time, Action<float> onTik, Action onComplete) => Manager.StartCd(time, onTik, onComplete);
    }
}