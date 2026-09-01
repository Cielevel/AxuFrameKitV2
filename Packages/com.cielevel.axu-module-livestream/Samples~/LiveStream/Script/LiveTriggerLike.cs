using AxuFrameKit.Modules.LiveStream;
using UnityEngine;

namespace TestAndSample.Sample_Modules.LiveStream
{
    [CreateAssetMenu(fileName = "LiveTriggerLike", menuName = "AxuFrameKit/LiveStream/Sample/LiveTriggerLike")]
    public class LiveTriggerLike : LiveTriggerLikeBase
    {
        public override void Trigger(UserDataLike unit, int count)
        {
            Debug.Log($"LiveTriggerLike");
        }
    }
}