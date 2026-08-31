using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 编辑器侧宿主清道夫：AgentRunnerHost 的宿主 GO（AxuTestAgentHost）是
    /// DontDestroyOnLoad + HideFlags.DontSave，Play 退出后可能泄漏为编辑态对象
    /// （实例字段随对象存活，会让下一次 Play 的启动逻辑被陈旧状态吞掉）。
    /// 本类在 Play 边界把残留宿主清掉，避免泄漏累积。只清非持久化的运行时对象，
    /// 不碰任何资产与用户场景内容。名字与运行时 <c>AgentRunnerHost.HostObjectName</c>
    /// 为字符串契约（Agent 程序集不被 Editor 强依赖，故此处独立声明）。
    /// </summary>
    [InitializeOnLoad]
    public static class AgentHostCleanup
    {
        /// <summary>宿主 GO 名（与 AgentRunnerHost.HostObjectName 契约一致，勿改）。</summary>
        public const string HostObjectName = "AxuTestAgentHost";

        static AgentHostCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (Application.isPlaying) return;
            if (change != PlayModeStateChange.EnteredEditMode && change != PlayModeStateChange.ExitingPlayMode)
                return;

            // FindObjectsOfTypeAll 才能覆盖泄漏的 DDOL/DontSave 残留（不属已加载场景，常规 Find 扫不到）
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null || go.name != HostObjectName) continue;
                if (EditorUtility.IsPersistent(go)) continue; // 资产不动
                Object.DestroyImmediate(go);
            }
        }
    }
}
