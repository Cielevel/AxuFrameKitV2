using UnityEngine;
using AxuFrameKit.Modules.LiveStream;

namespace TestAndSample.Sample_PackageDev.LiveStreamConsoleDemo
{
    // 演示用评论触发器：仅打印日志，展示"控制台发送 → 触发器分发"的最小闭环
    public class LiveTriggerCommentDemo : LiveTriggerCommentBase
    {
        public override void Trigger(UserDataComment userDataUnit)
        {
            Debug.Log($"[DemoTrigger] 评论来自 {userDataUnit.Nickname}({userDataUnit.UserId}): {userDataUnit.Comment}");
        }
    }
}
