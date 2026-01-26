using AxuFrameKit.Modules.LiveStream;
using UnityEngine;

namespace TestAndSample.Sample_Modules.LiveStream
{
    [CreateAssetMenu(fileName = "LiveTriggerGift", menuName = "AxuFrameKit/LiveStream/Sample/LiveTriggerGift")]
    public class LiveTriggerGift : LiveTriggerGiftBase
    {
        public override void Trigger(UserDataGift unit, int count)
        {
            Debug.Log($"Nickname:{unit.Nickname},GiftName:{unit.GiftName},Count:{count}");
        }
    }
}