using AxuFrameKit.Common.Utility.Singleton;
using UnityEngine;

namespace AxuExperimental.InGameConsole
{
    /// <summary>
    /// InGameConsole的Manager组件，挂载在场景中以启用Console功能。
    /// 只有当此组件存在于场景中时，InGameConsole才会被启用。
    /// </summary>
    public class InGameConsoleManager : SingletonMono<InGameConsoleManager>
    {
        [Header("Console Settings")]
        [Tooltip("是否在游戏启动时显示Console")]
        [SerializeField] private bool showOnStart;

        [Tooltip("切换Console显示/隐藏的按键")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

        [Tooltip("最大日志条目数量（用于性能优化）")]
        [SerializeField] private int maxLogCount = 1000;

        [Tooltip("是否捕获Unity日志")]
        [SerializeField] private bool captureUnityLogs = true;

        [Tooltip("是否捕获异常堆栈")]
        [SerializeField] private bool captureStackTrace = true;

        [Header("Appearance")]
        [Tooltip("Console面板的初始高度比例 (0-1)")]
        [Range(0.2f, 0.8f)]
        [SerializeField] private float initialHeightRatio = 0.4f;

        [Tooltip("UI面板设置（包含字体配置）。如果不设置，将尝试从Resources加载或创建默认配置")]
        [SerializeField] private UnityEngine.UIElements.PanelSettings panelSettings;

        // Properties
        public bool ShowOnStart => showOnStart;
        public KeyCode ToggleKey => toggleKey;
        public int MaxLogCount => maxLogCount;
        public bool CaptureUnityLogs => captureUnityLogs;
        public bool CaptureStackTrace => captureStackTrace;
        public float InitialHeightRatio => initialHeightRatio;
        public UnityEngine.UIElements.PanelSettings PanelSettings => panelSettings;

        // 内部引用
        private InGameConsole _console;

        protected override void Awake()
        {
            base.Awake();

            // 创建Console实例
            _console = new InGameConsole(this);
        }

        private void Start()
        {
            // 初始化UI
            _console.Initialize();

            if (showOnStart)
            {
                _console.Show();
            }
        }

        private void Update()
        {
            // 检测切换按键
            if (Input.GetKeyDown(toggleKey))
            {
                _console.Toggle();
            }
        }

        private void OnEnable()
        {
            if (captureUnityLogs)
            {
                Application.logMessageReceived += OnLogReceived;
            }
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        protected override void OnDestroy()
        {
            _console?.Dispose();
            base.OnDestroy();
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            _console?.AddLog(condition, stackTrace, type);
        }

        /// <summary>
        /// 显示Console
        /// </summary>
        public void ShowConsole() => _console?.Show();

        /// <summary>
        /// 隐藏Console
        /// </summary>
        public void HideConsole() => _console?.Hide();

        /// <summary>
        /// 切换Console显示/隐藏状态
        /// </summary>
        public void ToggleConsole() => _console?.Toggle();

        /// <summary>
        /// 清除所有日志
        /// </summary>
        public void ClearLogs() => _console?.Clear();

        /// <summary>
        /// 获取Console是否可见
        /// </summary>
        public bool IsVisible => _console?.IsVisible ?? false;
    }
}
