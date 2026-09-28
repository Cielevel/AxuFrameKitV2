using UnityEngine;
using AxuFrameKit.Modules.LiveStream;

namespace TestAndSample.Sample_PackageDev.LiveStreamConsoleDemo
{
    // 演示用点赞触发器：仅打印日志
    public class LiveTriggerLikeDemo : LiveTriggerLikeBase
    {
        public override void Trigger(UserDataLike userDataUnit, int count)
        {
            Debug.Log($"[DemoTrigger] {userDataUnit.Nickname}({userDataUnit.UserId}) 点赞 x{count}");
        }
    }
}
