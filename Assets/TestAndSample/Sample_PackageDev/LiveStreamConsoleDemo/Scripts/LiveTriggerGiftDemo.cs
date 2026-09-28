using UnityEngine;
using AxuFrameKit.Modules.LiveStream;

namespace TestAndSample.Sample_PackageDev.LiveStreamConsoleDemo
{
    // 演示用礼物触发器：仅打印日志（含礼物名/数量/来源用户）
    public class LiveTriggerGiftDemo : LiveTriggerGiftBase
    {
        public override void Trigger(UserDataGift userDataUnit, int count)
        {
            Debug.Log($"[DemoTrigger] {userDataUnit.Nickname}({userDataUnit.UserId}) 赠送 {userDataUnit.GiftName} x{count} (GiftIndex={userDataUnit.GiftIndex})");
        }
    }
}
