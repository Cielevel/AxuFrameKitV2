namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 运行就绪问题对应的修复动作类别（看板/引导界面据此渲染修复入口；见 <see cref="RunReadinessIssue"/>）。
    /// </summary>
    public enum ReadinessFixKind
    {
        /// <summary>无需动作（仅文案指引，或非阻断提示）。</summary>
        None,

        /// <summary>开启 AXU_TEST_AGENT 脚本符号（菜单 AxuTestFramework/Enable Test Agent，写符号触发重编译）。</summary>
        EnableAgentDefine,

        /// <summary>退出 Play Mode 后重试。</summary>
        ExitPlayMode,

        /// <summary>等待在途运行结束（UTF 运行与场景套件运行共用单运行通道，禁止并发）。</summary>
        WaitUntilIdle,

        /// <summary>创建缺失的 suite 文件（首次运行引导场景用）。</summary>
        CreateSuiteFile,

        /// <summary>配置构建场景清单（Build Settings / Build Profiles 的场景列表）。</summary>
        ConfigureBuildScenes,
    }
}
