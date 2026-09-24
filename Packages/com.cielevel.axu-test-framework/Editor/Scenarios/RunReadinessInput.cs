namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行就绪判定的全量输入：由 <see cref="RunReadinessCheck.GatherInput"/> 从 Unity 真实状态采集，
    /// 单测则手工构造——判定本体 <see cref="RunReadinessCheck.Evaluate"/> 是不碰 Unity API 的纯函数。
    /// </summary>
    public sealed class RunReadinessInput
    {
        /// <summary>当前是否处于 Play Mode。</summary>
        public bool IsPlaying;

        /// <summary>Unity 是否正在编译脚本（非阻断提示：此时发起运行结果可能不完整）。</summary>
        public bool IsCompiling;

        /// <summary>当前 BuildTargetGroup 是否已开启 AXU_TEST_AGENT（场景类目标需要）。</summary>
        public bool HasAgentDefine;

        /// <summary>是否有单元测试（UTF）运行在途。</summary>
        public bool UtfRunInFlight;

        /// <summary>是否有场景套件运行在途。</summary>
        public bool ScenarioRunInFlight;

        /// <summary>构建场景清单是否含至少一个已启用场景（smoke 的 buildSettings 来源的可跑前提）。</summary>
        public bool BuildSettingsHasScenes;
    }
}
