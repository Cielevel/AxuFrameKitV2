using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Utility.Analysis.InGameConsole
{
    /// <summary>
    /// InGameConsole的UI管理类
    /// 使用UI Toolkit构建高性能游戏内Console界面
    /// </summary>
    public class InGameConsoleUI : IDisposable
    {
        // 核心引用
        private readonly InGameConsole _console;
        private readonly InGameConsoleManager _manager;

        // UI组件
        private GameObject _uiObject;
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _consolePanel;

        // 工具栏元素
        private Button _clearBtn;
        private Button _collapseBtn;
        private TextField _searchField;
        private Button _logFilterBtn;
        private Button _warnFilterBtn;
        private Button _errorFilterBtn;
        private Label _logCountLabel;
        private Label _warnCountLabel;
        private Label _errorCountLabel;

        // 日志列表
        private ListView _logListView;

        // 详情面板
        private VisualElement _detailPanel;
        private Label _detailMessage;
        private Label _detailStackTrace;

        // 状态
        public bool IsVisible => _consolePanel?.style.display == DisplayStyle.Flex;

        // 样式路径
        private const string USSPath = "InGameConsole";

        public InGameConsoleUI(InGameConsole console, InGameConsoleManager manager)
        {
            _console = console;
            _manager = manager;
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        public void Initialize()
        {
            CreateUIDocument();
            BuildUI();
            BindEvents();
            Hide();
        }

        /// <summary>
        /// 创建UIDocument游戏对象
        /// </summary>
        private void CreateUIDocument()
        {
            _uiObject = new GameObject("[InGameConsole_UI]");
            _uiObject.transform.SetParent(_manager.transform);

            _uiDocument = _uiObject.AddComponent<UIDocument>();
            _uiDocument.panelSettings = CreatePanelSettings();

            // 加载样式表
            var styleSheet = Resources.Load<StyleSheet>(USSPath);
            if (styleSheet != null)
            {
                _uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }

            _root = _uiDocument.rootVisualElement;
        }

        /// <summary>
        /// 创建PanelSettings
        /// </summary>
        private PanelSettings CreatePanelSettings()
        {
            // 1. 优先使用Manager中配置的PanelSettings
            if (_manager.PanelSettings != null)
            {
                return _manager.PanelSettings;
            }

            // 2. 其次尝试从Resources加载预制的PanelSettings
            var prebuiltSettings = Resources.Load<PanelSettings>("InGameConsolePanelSettings");
            if (prebuiltSettings != null)
            {
                return prebuiltSettings;
            }

            // 3. 最后运行时创建（可能会有字体问题）
            Debug.LogWarning("[InGameConsole] 未配置PanelSettings，运行时创建可能导致字体问题。" +
                           "请在InGameConsoleManager中指定PanelSettings，或在Resources文件夹中放置'InGameConsolePanelSettings'资源。");

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 10000;  // 确保在最上层

            return settings;
        }

        /// <summary>
        /// 构建UI层级结构
        /// </summary>
        private void BuildUI()
        {
            _root.AddToClassList("igc-root");

            // 主Console面板
            _consolePanel = new VisualElement();
            _consolePanel.name = "console-panel";
            _consolePanel.AddToClassList("igc-panel");
            _consolePanel.style.position = Position.Absolute;
            _consolePanel.style.left = 0;
            _consolePanel.style.right = 0;
            _consolePanel.style.bottom = 0;
            _consolePanel.style.height = Length.Percent(_manager.InitialHeightRatio * 100);

            // 顶部拖动条
            var dragBar = new VisualElement();
            dragBar.name = "drag-bar";
            dragBar.AddToClassList("igc-drag-bar");
            SetupDragBar(dragBar);
            _consolePanel.Add(dragBar);

            // 工具栏
            var toolbar = BuildToolbar();
            _consolePanel.Add(toolbar);

            // 分隔线
            var separator = new VisualElement();
            separator.AddToClassList("igc-separator");
            _consolePanel.Add(separator);

            // 主内容区域 (分割视图)
            var mainContent = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Vertical);
            mainContent.name = "main-content";
            mainContent.AddToClassList("igc-main-content");

            // 日志列表区域
            var logListContainer = new VisualElement();
            logListContainer.name = "log-list-container";
            logListContainer.AddToClassList("igc-log-list-container");

            _logListView = new ListView();
            _logListView.name = "log-list";
            _logListView.AddToClassList("igc-log-list");
            _logListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _logListView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            _logListView.selectionType = SelectionType.Single;
            _logListView.makeItem = MakeLogItem;
            _logListView.bindItem = BindLogItem;
            _logListView.itemsSource = (System.Collections.IList)_console.FilteredLogs;

            logListContainer.Add(_logListView);
            mainContent.Add(logListContainer);

            // 详情面板
            _detailPanel = BuildDetailPanel();
            mainContent.Add(_detailPanel);

            _consolePanel.Add(mainContent);
            _root.Add(_consolePanel);

            // 应用内联样式 (作为备用，如果USS未加载)
            ApplyInlineStyles();
        }

        /// <summary>
        /// 构建工具栏
        /// </summary>
        private VisualElement BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.name = "toolbar";
            toolbar.AddToClassList("igc-toolbar");

            // 左侧按钮组
            var leftGroup = new VisualElement();
            leftGroup.AddToClassList("igc-toolbar-group");

            // Clear按钮
            _clearBtn = new Button(() => _console.Clear());
            _clearBtn.text = "Clear";
            _clearBtn.AddToClassList("igc-btn");
            _clearBtn.AddToClassList("igc-btn-danger");
            leftGroup.Add(_clearBtn);

            // Collapse按钮
            _collapseBtn = new Button(OnCollapseClick);
            _collapseBtn.text = "Collapse";
            _collapseBtn.AddToClassList("igc-btn");
            leftGroup.Add(_collapseBtn);

            toolbar.Add(leftGroup);

            // 搜索框
            _searchField = new TextField();
            _searchField.name = "search-field";
            _searchField.AddToClassList("igc-search-field");
            _searchField.value = "";

            // 添加搜索图标占位符
            var searchPlaceholder = new Label("🔍 Search...");
            searchPlaceholder.AddToClassList("igc-search-placeholder");
            _searchField.Add(searchPlaceholder);

            _searchField.RegisterValueChangedCallback(evt =>
            {
                searchPlaceholder.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                _console.SearchFilter = evt.newValue;
            });

            toolbar.Add(_searchField);

            // 右侧过滤按钮组
            var rightGroup = new VisualElement();
            rightGroup.AddToClassList("igc-toolbar-group");
            rightGroup.AddToClassList("igc-filter-group");

            // Log过滤器
            _logFilterBtn = CreateFilterButton("ℹ", "igc-filter-log", () =>
            {
                _console.ShowLogs = !_console.ShowLogs;
                UpdateFilterButtonState(_logFilterBtn, _console.ShowLogs);
            });
            _logCountLabel = new Label("0");
            _logCountLabel.AddToClassList("igc-filter-count");
            var logContainer = new VisualElement();
            logContainer.AddToClassList("igc-filter-container");
            logContainer.Add(_logFilterBtn);
            logContainer.Add(_logCountLabel);
            rightGroup.Add(logContainer);

            // Warning过滤器
            _warnFilterBtn = CreateFilterButton("⚠", "igc-filter-warn", () =>
            {
                _console.ShowWarnings = !_console.ShowWarnings;
                UpdateFilterButtonState(_warnFilterBtn, _console.ShowWarnings);
            });
            _warnCountLabel = new Label("0");
            _warnCountLabel.AddToClassList("igc-filter-count");
            var warnContainer = new VisualElement();
            warnContainer.AddToClassList("igc-filter-container");
            warnContainer.Add(_warnFilterBtn);
            warnContainer.Add(_warnCountLabel);
            rightGroup.Add(warnContainer);

            // Error过滤器
            _errorFilterBtn = CreateFilterButton("✕", "igc-filter-error", () =>
            {
                _console.ShowErrors = !_console.ShowErrors;
                UpdateFilterButtonState(_errorFilterBtn, _console.ShowErrors);
            });
            _errorCountLabel = new Label("0");
            _errorCountLabel.AddToClassList("igc-filter-count");
            var errorContainer = new VisualElement();
            errorContainer.AddToClassList("igc-filter-container");
            errorContainer.Add(_errorFilterBtn);
            errorContainer.Add(_errorCountLabel);
            rightGroup.Add(errorContainer);

            // 关闭按钮
            var closeBtn = new Button(Hide);
            closeBtn.text = "✕";
            closeBtn.AddToClassList("igc-btn");
            closeBtn.AddToClassList("igc-btn-close");
            rightGroup.Add(closeBtn);

            toolbar.Add(rightGroup);

            return toolbar;
        }

        /// <summary>
        /// 创建过滤按钮
        /// </summary>
        private Button CreateFilterButton(string icon, string className, Action onClick)
        {
            var btn = new Button(onClick);
            btn.text = icon;
            btn.AddToClassList("igc-filter-btn");
            btn.AddToClassList(className);
            btn.AddToClassList("igc-filter-active");
            return btn;
        }

        /// <summary>
        /// 更新过滤按钮状态
        /// </summary>
        private void UpdateFilterButtonState(Button btn, bool active)
        {
            if (active)
            {
                btn.AddToClassList("igc-filter-active");
                btn.RemoveFromClassList("igc-filter-inactive");
            }
            else
            {
                btn.RemoveFromClassList("igc-filter-active");
                btn.AddToClassList("igc-filter-inactive");
            }
        }

        /// <summary>
        /// 构建详情面板
        /// </summary>
        private VisualElement BuildDetailPanel()
        {
            var panel = new VisualElement();
            panel.name = "detail-panel";
            panel.AddToClassList("igc-detail-panel");

            var header = new Label("Details");
            header.AddToClassList("igc-detail-header");
            panel.Add(header);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("igc-detail-scroll");

            _detailMessage = new Label();
            _detailMessage.name = "detail-message";
            _detailMessage.AddToClassList("igc-detail-message");
            _detailMessage.enableRichText = true;
            scrollView.Add(_detailMessage);

            var stackHeader = new Label("Stack Trace:");
            stackHeader.AddToClassList("igc-stack-header");
            scrollView.Add(stackHeader);

            _detailStackTrace = new Label();
            _detailStackTrace.name = "detail-stack";
            _detailStackTrace.AddToClassList("igc-detail-stack");
            scrollView.Add(_detailStackTrace);

            panel.Add(scrollView);

            return panel;
        }

        /// <summary>
        /// 设置拖动条功能
        /// </summary>
        private void SetupDragBar(VisualElement dragBar)
        {
            float startY = 0;
            float startHeight = 0;
            bool isDragging = false;

            dragBar.RegisterCallback<PointerDownEvent>(evt =>
            {
                isDragging = true;
                startY = evt.position.y;
                startHeight = _consolePanel.resolvedStyle.height;
                dragBar.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            dragBar.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging) return;

                float deltaY = startY - evt.position.y;
                float newHeight = Mathf.Clamp(startHeight + deltaY, 150, Screen.height * 0.9f);
                _consolePanel.style.height = newHeight;
                evt.StopPropagation();
            });

            dragBar.RegisterCallback<PointerUpEvent>(evt =>
            {
                isDragging = false;
                dragBar.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }

        /// <summary>
        /// 创建日志列表项
        /// </summary>
        private VisualElement MakeLogItem()
        {
            var item = new VisualElement();
            item.AddToClassList("igc-log-item");

            // 内联样式
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.paddingLeft = 10;
            item.style.paddingRight = 10;
            item.style.paddingTop = 6;
            item.style.paddingBottom = 6;
            item.style.borderBottomWidth = 1;
            item.style.borderBottomColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f, 1f));
            item.style.minHeight = 32;

            var icon = new Label();
            icon.name = "icon";
            icon.AddToClassList("igc-log-icon");
            icon.style.width = 20;
            icon.style.minWidth = 20;
            icon.style.fontSize = 14;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.marginRight = 8;
            item.Add(icon);

            var content = new VisualElement();
            content.AddToClassList("igc-log-content");
            content.style.flexGrow = 1;
            content.style.flexDirection = FlexDirection.Column;
            content.style.overflow = Overflow.Hidden;

            var message = new Label();
            message.name = "message";
            message.AddToClassList("igc-log-message");
            message.style.fontSize = 11;
            message.style.color = new StyleColor(new Color(0.91f, 0.91f, 0.91f, 1f));
            message.style.unityTextAlign = TextAnchor.MiddleLeft;
            message.style.whiteSpace = WhiteSpace.NoWrap;
            message.style.overflow = Overflow.Hidden;
            message.style.textOverflow = TextOverflow.Ellipsis;
            content.Add(message);

            var timestamp = new Label();
            timestamp.name = "timestamp";
            timestamp.AddToClassList("igc-log-timestamp");
            timestamp.style.fontSize = 9;
            timestamp.style.color = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1f));
            timestamp.style.marginTop = 2;
            content.Add(timestamp);

            item.Add(content);

            var collapseCount = new Label();
            collapseCount.name = "collapse-count";
            collapseCount.AddToClassList("igc-collapse-count");
            collapseCount.style.minWidth = 24;
            collapseCount.style.height = 18;
            collapseCount.style.backgroundColor = new StyleColor(new Color(1f, 0.58f, 0f, 1f));
            collapseCount.style.color = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
            collapseCount.style.fontSize = 10;
            collapseCount.style.unityFontStyleAndWeight = FontStyle.Bold;
            collapseCount.style.borderTopLeftRadius = 9;
            collapseCount.style.borderTopRightRadius = 9;
            collapseCount.style.borderBottomLeftRadius = 9;
            collapseCount.style.borderBottomRightRadius = 9;
            collapseCount.style.paddingLeft = 6;
            collapseCount.style.paddingRight = 6;
            collapseCount.style.marginLeft = 8;
            collapseCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            collapseCount.style.display = DisplayStyle.None;
            item.Add(collapseCount);

            return item;
        }

        /// <summary>
        /// 绑定日志列表项数据
        /// </summary>
        private void BindLogItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _console.FilteredLogs.Count) return;

            var log = _console.FilteredLogs[index];

            // 设置图标
            var icon = element.Q<Label>("icon");
            if (icon != null)
            {
                icon.text = log.GetIcon();
                icon.style.color = new StyleColor(ColorUtility.TryParseHtmlString(log.GetColorHex(), out var color) ? color : Color.white);
            }

            // 设置消息
            var message = element.Q<Label>("message");
            if (message != null)
            {
                message.text = log.GetDisplayText();
            }

            // 设置时间戳
            var timestamp = element.Q<Label>("timestamp");
            if (timestamp != null)
            {
                timestamp.text = log.Timestamp;
            }

            // 设置折叠计数
            var collapseCount = element.Q<Label>("collapse-count");
            if (collapseCount != null)
            {
                if (log.CollapseCount > 1)
                {
                    collapseCount.text = log.CollapseCount.ToString();
                    collapseCount.style.display = DisplayStyle.Flex;
                }
                else
                {
                    collapseCount.style.display = DisplayStyle.None;
                }
            }

            // 根据日志类型设置背景色（使用内联样式）
            switch (log.LogType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    element.style.backgroundColor = new StyleColor(new Color(1f, 0.31f, 0.31f, 0.08f));
                    break;
                case LogType.Warning:
                    element.style.backgroundColor = new StyleColor(new Color(1f, 0.78f, 0.2f, 0.05f));
                    break;
                default:
                    element.style.backgroundColor = new StyleColor(Color.clear);
                    break;
            }
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents()
        {
            _console.OnLogsChanged += OnLogsChanged;
            _console.OnCountsChanged += OnCountsChanged;

            _logListView.selectionChanged += OnLogSelected;
        }

        /// <summary>
        /// 日志变更处理
        /// </summary>
        private void OnLogsChanged()
        {
            _logListView.itemsSource = (System.Collections.IList)_console.FilteredLogs;
            _logListView.Rebuild();
        }

        /// <summary>
        /// 计数变更处理
        /// </summary>
        private void OnCountsChanged(int logCount, int warnCount, int errorCount)
        {
            _logCountLabel.text = FormatCount(logCount);
            _warnCountLabel.text = FormatCount(warnCount);
            _errorCountLabel.text = FormatCount(errorCount);
        }

        /// <summary>
        /// 格式化计数显示
        /// </summary>
        private string FormatCount(int count)
        {
            if (count >= 1000)
            {
                return $"{count / 1000f:F1}k";
            }
            return count.ToString();
        }

        /// <summary>
        /// 日志选择处理
        /// </summary>
        private void OnLogSelected(System.Collections.Generic.IEnumerable<object> selectedItems)
        {
            int index = _logListView.selectedIndex;
            var log = _console.GetLogAt(index);

            if (log.HasValue)
            {
                _detailMessage.text = log.Value.Message;
                _detailStackTrace.text = string.IsNullOrEmpty(log.Value.StackTrace)
                    ? "(No stack trace)"
                    : log.Value.StackTrace;
            }
            else
            {
                _detailMessage.text = "";
                _detailStackTrace.text = "";
            }
        }

        /// <summary>
        /// Collapse按钮点击
        /// </summary>
        private void OnCollapseClick()
        {
            _console.CollapseEnabled = !_console.CollapseEnabled;

            if (_console.CollapseEnabled)
            {
                _collapseBtn.AddToClassList("igc-btn-active");
            }
            else
            {
                _collapseBtn.RemoveFromClassList("igc-btn-active");
            }
        }

        /// <summary>
        /// 应用完整的内联样式
        /// </summary>
        private void ApplyInlineStyles()
        {
            // 颜色定义
            var bgDarkest = new Color(0.08f, 0.08f, 0.08f, 1f);
            var bgDark = new Color(0.1f, 0.1f, 0.1f, 1f);
            var bgPanel = new Color(0.12f, 0.12f, 0.12f, 1f);
            var bgHover = new Color(0.18f, 0.18f, 0.18f, 1f);
            var accentPrimary = new Color(1f, 0.58f, 0f, 1f);
            var accentBorder = new Color(1f, 0.58f, 0f, 0.5f);
            var borderDark = new Color(0.05f, 0.05f, 0.05f, 1f);
            var borderSubtle = new Color(0.2f, 0.2f, 0.2f, 1f);
            var textPrimary = new Color(0.91f, 0.91f, 0.91f, 1f);
            var textSecondary = new Color(0.63f, 0.63f, 0.63f, 1f);
            var textDisabled = new Color(0.4f, 0.4f, 0.4f, 1f);

            // Root样式
            _root.style.position = Position.Absolute;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.top = 0;
            _root.style.bottom = 0;

            // Panel样式
            _consolePanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f, 0.97f));
            _consolePanel.style.borderTopWidth = 3;
            _consolePanel.style.borderTopColor = new StyleColor(accentPrimary);
            _consolePanel.style.flexDirection = FlexDirection.Column;

            // 拖动条样式
            var dragBar = _consolePanel.Q("drag-bar");
            if (dragBar != null)
            {
                dragBar.style.height = 8;
                dragBar.style.minHeight = 8;
                dragBar.style.backgroundColor = new StyleColor(bgDark);
                dragBar.style.borderBottomWidth = 1;
                dragBar.style.borderBottomColor = new StyleColor(borderDark);
                dragBar.style.flexShrink = 0;
            }

            // 工具栏样式
            var toolbar = _consolePanel.Q("toolbar");
            if (toolbar != null)
            {
                toolbar.style.flexDirection = FlexDirection.Row;
                toolbar.style.justifyContent = Justify.SpaceBetween;
                toolbar.style.alignItems = Align.Center;
                toolbar.style.backgroundColor = new StyleColor(bgPanel);
                toolbar.style.paddingLeft = 10;
                toolbar.style.paddingRight = 10;
                toolbar.style.paddingTop = 6;
                toolbar.style.paddingBottom = 6;
                toolbar.style.borderBottomWidth = 1;
                toolbar.style.borderBottomColor = new StyleColor(borderDark);
                toolbar.style.height = 40;
                toolbar.style.minHeight = 40;
                toolbar.style.flexShrink = 0;

                // 设置所有工具栏组的样式
                toolbar.Query(className: "igc-toolbar-group").ForEach(group =>
                {
                    group.style.flexDirection = FlexDirection.Row;
                    group.style.alignItems = Align.Center;
                    group.style.flexShrink = 0;
                });

                // 设置所有过滤容器的样式
                toolbar.Query(className: "igc-filter-container").ForEach(container =>
                {
                    container.style.flexDirection = FlexDirection.Row;
                    container.style.alignItems = Align.Center;
                    container.style.marginLeft = 4;
                });

                // 设置过滤组靠右
                var filterGroup = toolbar.Q(className: "igc-filter-group");
                if (filterGroup != null)
                {
                    filterGroup.style.marginLeft = new StyleLength(StyleKeyword.Auto);
                }
            }

            // 按钮通用样式
            ApplyButtonStyles(_clearBtn, new Color(0.29f, 0.17f, 0.17f, 1f), new Color(1f, 0.6f, 0.6f, 1f));
            ApplyButtonStyles(_collapseBtn, new Color(0.23f, 0.23f, 0.23f, 1f), textPrimary);

            // 搜索框样式
            if (_searchField != null)
            {
                _searchField.style.flexGrow = 1;
                _searchField.style.maxWidth = 300;
                _searchField.style.marginLeft = 10;
                _searchField.style.marginRight = 10;

                var input = _searchField.Q(className: "unity-text-field__input");
                if (input != null)
                {
                    input.style.backgroundColor = new StyleColor(bgDarkest);
                    input.style.borderTopWidth = 1;
                    input.style.borderBottomWidth = 1;
                    input.style.borderLeftWidth = 1;
                    input.style.borderRightWidth = 1;
                    input.style.borderTopColor = new StyleColor(borderSubtle);
                    input.style.borderBottomColor = new StyleColor(borderSubtle);
                    input.style.borderLeftColor = new StyleColor(borderSubtle);
                    input.style.borderRightColor = new StyleColor(borderSubtle);
                    input.style.borderTopLeftRadius = 3;
                    input.style.borderTopRightRadius = 3;
                    input.style.borderBottomLeftRadius = 3;
                    input.style.borderBottomRightRadius = 3;
                    input.style.color = new StyleColor(textPrimary);
                    input.style.paddingLeft = 10;
                    input.style.paddingRight = 10;
                    input.style.paddingTop = 4;
                    input.style.paddingBottom = 4;
                    input.style.fontSize = 11;
                    input.style.minHeight = 24;
                }
            }

            // 过滤按钮样式
            ApplyFilterButtonStyles(_logFilterBtn, new Color(0.43f, 0.71f, 1f, 1f));
            ApplyFilterButtonStyles(_warnFilterBtn, new Color(1f, 0.85f, 0.24f, 1f));
            ApplyFilterButtonStyles(_errorFilterBtn, new Color(1f, 0.42f, 0.42f, 1f));

            // 计数标签样式
            ApplyCountLabelStyles(_logCountLabel);
            ApplyCountLabelStyles(_warnCountLabel);
            ApplyCountLabelStyles(_errorCountLabel);

            // 关闭按钮样式
            var closeBtn = _consolePanel.Q<Button>(className: "igc-btn-close");
            if (closeBtn != null)
            {
                closeBtn.style.width = 26;
                closeBtn.style.minWidth = 26;
                closeBtn.style.height = 26;
                closeBtn.style.paddingLeft = 0;
                closeBtn.style.paddingRight = 0;
                closeBtn.style.marginRight = 0;
                closeBtn.style.marginLeft = 10;
                closeBtn.style.fontSize = 14;
                closeBtn.style.backgroundColor = new StyleColor(Color.clear);
                closeBtn.style.borderTopWidth = 0;
                closeBtn.style.borderBottomWidth = 0;
                closeBtn.style.borderLeftWidth = 0;
                closeBtn.style.borderRightWidth = 0;
                closeBtn.style.color = new StyleColor(new Color(0.53f, 0.53f, 0.53f, 1f));
            }

            // 分隔线样式
            var separator = _consolePanel.Q(className: "igc-separator");
            if (separator != null)
            {
                separator.style.height = 2;
                separator.style.minHeight = 2;
                separator.style.backgroundColor = new StyleColor(accentPrimary);
                separator.style.flexShrink = 0;
            }

            // 主内容区域样式
            var mainContent = _consolePanel.Q("main-content");
            if (mainContent != null)
            {
                mainContent.style.flexGrow = 1;
            }

            // 日志列表容器样式
            var logListContainer = _consolePanel.Q("log-list-container");
            if (logListContainer != null)
            {
                logListContainer.style.flexGrow = 1;
                logListContainer.style.backgroundColor = new StyleColor(bgDarkest);
            }

            // 日志列表样式
            if (_logListView != null)
            {
                _logListView.style.flexGrow = 1;
                _logListView.style.backgroundColor = new StyleColor(Color.clear);
            }

            // 详情面板样式
            if (_detailPanel != null)
            {
                _detailPanel.style.backgroundColor = new StyleColor(bgPanel);
                _detailPanel.style.borderTopWidth = 1;
                _detailPanel.style.borderTopColor = new StyleColor(borderDark);
                _detailPanel.style.minHeight = 100;

                var detailHeader = _detailPanel.Q(className: "igc-detail-header");
                if (detailHeader != null)
                {
                    detailHeader.style.height = 26;
                    detailHeader.style.backgroundColor = new StyleColor(bgDark);
                    detailHeader.style.color = new StyleColor(textSecondary);
                    detailHeader.style.fontSize = 11;
                    detailHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    detailHeader.style.paddingLeft = 10;
                    detailHeader.style.unityTextAlign = TextAnchor.MiddleLeft;
                    detailHeader.style.borderBottomWidth = 1;
                    detailHeader.style.borderBottomColor = new StyleColor(borderDark);
                }

                var detailScroll = _detailPanel.Q(className: "igc-detail-scroll");
                if (detailScroll != null)
                {
                    detailScroll.style.flexGrow = 1;
                    detailScroll.style.paddingLeft = 10;
                    detailScroll.style.paddingRight = 10;
                    detailScroll.style.paddingTop = 10;
                    detailScroll.style.paddingBottom = 10;
                }

                if (_detailMessage != null)
                {
                    _detailMessage.style.fontSize = 12;
                    _detailMessage.style.color = new StyleColor(textPrimary);
                    _detailMessage.style.whiteSpace = WhiteSpace.Normal;
                    _detailMessage.style.marginBottom = 10;
                }

                var stackHeader = _detailPanel.Q(className: "igc-stack-header");
                if (stackHeader != null)
                {
                    stackHeader.style.fontSize = 10;
                    stackHeader.style.color = new StyleColor(accentPrimary);
                    stackHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    stackHeader.style.marginBottom = 4;
                }

                if (_detailStackTrace != null)
                {
                    _detailStackTrace.style.fontSize = 10;
                    _detailStackTrace.style.color = new StyleColor(textSecondary);
                    _detailStackTrace.style.whiteSpace = WhiteSpace.Normal;
                }
            }
        }

        /// <summary>
        /// 应用按钮样式
        /// </summary>
        private void ApplyButtonStyles(Button btn, Color bgColor, Color textColor)
        {
            if (btn == null) return;

            btn.style.height = 26;
            btn.style.minHeight = 26;
            btn.style.minWidth = 60;
            btn.style.fontSize = 11;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.backgroundColor = new StyleColor(bgColor);
            btn.style.color = new StyleColor(textColor);
            btn.style.borderTopLeftRadius = 3;
            btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3;
            btn.style.borderBottomRightRadius = 3;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderRightColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.marginRight = 6;
        }

        /// <summary>
        /// 应用过滤按钮样式
        /// </summary>
        private void ApplyFilterButtonStyles(Button btn, Color iconColor)
        {
            if (btn == null) return;

            btn.style.width = 28;
            btn.style.height = 26;
            btn.style.minWidth = 28;
            btn.style.minHeight = 26;
            btn.style.fontSize = 12;
            btn.style.backgroundColor = new StyleColor(new Color(0.23f, 0.23f, 0.23f, 1f));
            btn.style.color = new StyleColor(iconColor);
            btn.style.borderTopLeftRadius = 3;
            btn.style.borderTopRightRadius = 3;
            btn.style.borderBottomLeftRadius = 3;
            btn.style.borderBottomRightRadius = 3;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.borderRightColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
            btn.style.paddingLeft = 0;
            btn.style.paddingRight = 0;
            btn.style.marginLeft = 4;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 应用计数标签样式
        /// </summary>
        private void ApplyCountLabelStyles(Label label)
        {
            if (label == null) return;

            label.style.fontSize = 10;
            label.style.color = new StyleColor(new Color(0.63f, 0.63f, 0.63f, 1f));
            label.style.marginLeft = 2;
            label.style.minWidth = 20;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        /// <summary>
        /// 显示Console
        /// </summary>
        public void Show()
        {
            if (_consolePanel != null)
            {
                _consolePanel.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 隐藏Console
        /// </summary>
        public void Hide()
        {
            if (_consolePanel != null)
            {
                _consolePanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 切换显示/隐藏状态
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// 销毁资源
        /// </summary>
        public void Dispose()
        {
            _console.OnLogsChanged -= OnLogsChanged;
            _console.OnCountsChanged -= OnCountsChanged;

            if (_uiObject != null)
            {
                UnityEngine.Object.Destroy(_uiObject);
            }
        }
    }
}
