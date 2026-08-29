using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Editor
{
    /// <summary>
    /// LiveStream 编辑器桥接：把 <see cref="LiveStreamSimulatorWindow"/> 的发送动作
    /// 转发给运行时的 <see cref="LiveStreamEditorModeManager"/>。
    /// 原先位于 LiveStreamEditorModeManager（Runtime）中 #if UNITY_EDITOR 块的接线逻辑，
    /// 在 Runtime/Editor 程序集拆分后迁移至此，使 Runtime 程序集不再引用任何 Editor 类型。
    /// </summary>
    internal static class LiveStreamEditorHooks
    {
        [InitializeOnLoadMethod]
        private static void HookSimulatorWindow()
        {
            // 先解绑再绑定，避免域重载（或程序集重新加载）导致重复订阅，
            // 等价于原先 Init 中 ClearActions() + 订阅 的去重效果；
            // 且不再像原先那样清空 Recorder 窗口等其他订阅者。
            LiveStreamSimulatorWindow.OnCommentsSent -= ForwardComments;
            LiveStreamSimulatorWindow.OnGiftsSent -= ForwardGifts;
            LiveStreamSimulatorWindow.OnLikesSent -= ForwardLikes;

            LiveStreamSimulatorWindow.OnCommentsSent += ForwardComments;
            LiveStreamSimulatorWindow.OnGiftsSent += ForwardGifts;
            LiveStreamSimulatorWindow.OnLikesSent += ForwardLikes;
        }

        private static void ForwardComments(IReadOnlyList<UserDataComment> comments)
        {
            // SingletonMono<T>.Instance 仅返回场景中已 Awake 的实例，不会自动创建；
            // 场景中没有管理器或未调用 Init 时保持静默，等价于原先未接线的状态。
            var manager = LiveStreamEditorModeManager.Instance;
            if (manager != null && manager.IsReady)
                manager.TriggerComment(comments);
        }

        private static void ForwardGifts(IReadOnlyList<UserDataGift> gifts)
        {
            var manager = LiveStreamEditorModeManager.Instance;
            if (manager != null && manager.IsReady)
                manager.TriggerGift(gifts);
        }

        private static void ForwardLikes(IReadOnlyList<UserDataLike> likes)
        {
            var manager = LiveStreamEditorModeManager.Instance;
            if (manager != null && manager.IsReady)
                manager.TriggerLike(likes);
        }
    }
}
