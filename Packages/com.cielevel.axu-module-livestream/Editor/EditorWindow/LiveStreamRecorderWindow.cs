#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using AxuFrameKit.Common.Utility.Auto.AutoSnapshot;

namespace AxuFrameKit.Modules.LiveStream
{
    /// <summary>
    /// LiveStream 操作录制窗口
    /// 用于录制 LiveStreamSimulatorWindow 中发生的操作
    /// </summary>
    public class LiveStreamRecorderWindow : EditorWindow
    {
        [MenuItem("AxuFrameKit/Modules/LiveStream/Operation Recorder")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LiveStreamRecorderWindow>();
            wnd.titleContent = new GUIContent("LiveStream Recorder");
            wnd.minSize = new Vector2(500, 600);
        }

        // === Recorder Instance ===
        private LiveStreamOpRecorder _recorder;
        private LiveStreamRecorderConfig _config;

        // === UI Elements ===
        private Button _startRecordBtn;
        private Button _stopRecordBtn;
        private Button _saveBtn;
        private Button _clearBtn;
        private Label _statusLabel;
        private Label _countLabel;
        private TextField _savePathField;

        // === Config Buttons (Toggle style) ===
        private Button _scaledTimeBtn;
        private Button _unscaledTimeBtn;
        private Button _recordFromInitBtn;
        private Button _recordFromGameStartBtn;
        private Button _recordGiftsBtn;
        private Button _recordCommentsBtn;
        private Button _recordLikesBtn;

        // === Log View ===
        private ScrollView _logScrollView;
        private Label _logStatusLabel;
        private List<RecordLogEntry> _logs = new List<RecordLogEntry>();
        private HashSet<int> _expandedLogs = new HashSet<int>();
        private const int MaxLogCount = 500;
        private bool _autoScroll = true;

        // === Filters ===
        private bool _showGift = true;
        private bool _showComment = true;
        private bool _showLike = true;

        // === State ===
        private bool _isSubscribed = false;
        private string _defaultSavePath;

        // === Log Entry Structure ===
        public struct RecordLogEntry
        {
            public DateTime Timestamp;
            public LiveStreamOpType OpType;
            public string Nickname;
            public string UserId;
            public string Message;
            public string Details;
        }

        private void OnEnable()
        {
            // 设置默认保存路径
            _defaultSavePath = Path.Combine(Application.dataPath, "LiveStreamRecordings");

            // 初始化配置
            _config = new LiveStreamRecorderConfig();

            // 订阅 Simulator 事件
            SubscribeToSimulatorEvents();
        }

        private void OnDisable()
        {
            // 取消订阅
            UnsubscribeFromSimulatorEvents();
        }

        public void CreateGUI()
        {
            // 加载样式表
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/LiveStreamSimulatorWindow.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // 应用根样式
            rootVisualElement.AddToClassList("ue5-root");

            // 主垂直分割视图 (控制区域在上, 日志区域在下) - 可拖动分隔
            var mainSplitView = new TwoPaneSplitView(1, 250, TwoPaneSplitViewOrientation.Vertical);
            mainSplitView.AddToClassList("main-vertical-split");
            mainSplitView.style.flexGrow = 1;
            rootVisualElement.Add(mainSplitView);

            // 首次布局时设置分割比例
            mainSplitView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.oldRect.height == 0 && evt.newRect.height > 0)
                {
                    float logHeight = evt.newRect.height * 0.65f;
                    mainSplitView.fixedPaneInitialDimension = logHeight;
                }
            });

            // === 上半部分: 控制区域 ===
            var controlArea = CreateControlArea();
            controlArea.style.flexGrow = 1;
            controlArea.style.minHeight = 200;
            mainSplitView.Add(controlArea);

            // === 下半部分: 记录日志区域 ===
            var logArea = CreateLogArea();
            logArea.style.flexGrow = 1;
            logArea.style.minHeight = 150;
            mainSplitView.Add(logArea);

            // 初始化 UI 状态
            UpdateUIState();
        }

        #region Control Area

        private VisualElement CreateControlArea()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.minHeight = 200;
            container.AddToClassList("simulator-container");

            // 水平分割 (左侧配置 | 右侧控制)
            var splitView = new TwoPaneSplitView(0, 240, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            container.Add(splitView);

            // 左侧: 配置面板
            var leftPane = CreateConfigPane();
            splitView.Add(leftPane);

            // 右侧: 状态和控制面板
            var rightPane = CreateStatusAndControlPane();
            splitView.Add(rightPane);

            return container;
        }

        private VisualElement CreateConfigPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.minWidth = 180;
            pane.AddToClassList("rounded-left-pane");

            // === 时间配置区域 ===
            var timeSection = new VisualElement();
            timeSection.style.flexGrow = 0;
            pane.Add(timeSection);

            var timeHeader = new Label("TIME CONFIG");
            timeHeader.AddToClassList("header-label");
            timeSection.Add(timeHeader);

            // 时间缩放模式 Toggle 按钮
            var scaleModeLabel = new Label("时间缩放模式");
            scaleModeLabel.style.fontSize = 11;
            scaleModeLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            scaleModeLabel.style.marginBottom = 4;
            timeSection.Add(scaleModeLabel);

            var scaleModeRow = new VisualElement();
            scaleModeRow.style.flexDirection = FlexDirection.Row;
            scaleModeRow.style.marginBottom = 10;
            scaleModeRow.style.paddingLeft = 4;
            scaleModeRow.style.paddingRight = 4;
            timeSection.Add(scaleModeRow);

            _scaledTimeBtn = CreateToggleButton("Scaled", _config.ScaleMode == TimeScaleMode.Scaled, () =>
            {
                _config.ScaleMode = TimeScaleMode.Scaled;
                UpdateToggleButtonState(_scaledTimeBtn, true);
                UpdateToggleButtonState(_unscaledTimeBtn, false);
            });
            _scaledTimeBtn.style.flexGrow = 1;
            _scaledTimeBtn.style.marginRight = 4;
            scaleModeRow.Add(_scaledTimeBtn);

            _unscaledTimeBtn = CreateToggleButton("Unscaled", _config.ScaleMode == TimeScaleMode.Unscaled, () =>
            {
                _config.ScaleMode = TimeScaleMode.Unscaled;
                UpdateToggleButtonState(_scaledTimeBtn, false);
                UpdateToggleButtonState(_unscaledTimeBtn, true);
            });
            _unscaledTimeBtn.style.flexGrow = 1;
            scaleModeRow.Add(_unscaledTimeBtn);

            // 时间记录模式 Toggle 按钮
            var recordModeLabel = new Label("时间记录模式");
            recordModeLabel.style.fontSize = 11;
            recordModeLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            recordModeLabel.style.marginBottom = 4;
            timeSection.Add(recordModeLabel);

            var recordModeRow = new VisualElement();
            recordModeRow.style.flexDirection = FlexDirection.Row;
            recordModeRow.style.marginBottom = 10;
            recordModeRow.style.paddingLeft = 4;
            recordModeRow.style.paddingRight = 4;
            timeSection.Add(recordModeRow);

            _recordFromInitBtn = CreateToggleButton("From Init", _config.RecordMode == TimeRecordMode.RecordFromInit, () =>
            {
                _config.RecordMode = TimeRecordMode.RecordFromInit;
                UpdateToggleButtonState(_recordFromInitBtn, true);
                UpdateToggleButtonState(_recordFromGameStartBtn, false);
            });
            _recordFromInitBtn.style.flexGrow = 1;
            _recordFromInitBtn.style.marginRight = 4;
            recordModeRow.Add(_recordFromInitBtn);

            _recordFromGameStartBtn = CreateToggleButton("From Game Start", _config.RecordMode == TimeRecordMode.RecordFromGameStart, () =>
            {
                _config.RecordMode = TimeRecordMode.RecordFromGameStart;
                UpdateToggleButtonState(_recordFromInitBtn, false);
                UpdateToggleButtonState(_recordFromGameStartBtn, true);
            });
            _recordFromGameStartBtn.style.flexGrow = 1;
            recordModeRow.Add(_recordFromGameStartBtn);

            // === 分隔线 ===
            pane.Add(CreateSeparator());

            // === 录制内容区域 ===
            var contentSection = new VisualElement();
            contentSection.style.flexGrow = 0;
            pane.Add(contentSection);

            var contentHeader = new Label("RECORD CONTENT");
            contentHeader.AddToClassList("header-label");
            contentSection.Add(contentHeader);

            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("debugger-filter-container");
            contentSection.Add(contentContainer);

            _recordGiftsBtn = CreateFilterButton("🎁 礼物", _config.RecordGifts, v =>
            {
                _config.RecordGifts = v;
            });
            contentContainer.Add(_recordGiftsBtn);

            _recordCommentsBtn = CreateFilterButton("💬 评论", _config.RecordComments, v =>
            {
                _config.RecordComments = v;
            });
            contentContainer.Add(_recordCommentsBtn);

            _recordLikesBtn = CreateFilterButton("❤ 点赞", _config.RecordLikes, v =>
            {
                _config.RecordLikes = v;
            });
            contentContainer.Add(_recordLikesBtn);

            return pane;
        }

        private VisualElement CreateStatusAndControlPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1;
            pane.style.minWidth = 200;
            pane.AddToClassList("rounded-right-pane");

            // === 状态显示 ===
            var statusSection = new VisualElement();
            statusSection.style.flexGrow = 1;
            statusSection.style.paddingLeft = 8;
            statusSection.style.paddingRight = 8;
            statusSection.style.paddingTop = 4;
            pane.Add(statusSection);

            var statusHeader = new Label("STATUS");
            statusHeader.AddToClassList("header-label");
            statusSection.Add(statusHeader);

            // 录制状态
            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.alignItems = Align.Center;
            statusRow.style.marginBottom = 8;
            statusSection.Add(statusRow);

            var statusTitle = new Label("录制状态:");
            statusTitle.style.width = 80;
            statusRow.Add(statusTitle);

            _statusLabel = new Label("未录制");
            _statusLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            _statusLabel.style.fontSize = 14;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusRow.Add(_statusLabel);

            // 录制数量
            var countRow = new VisualElement();
            countRow.style.flexDirection = FlexDirection.Row;
            countRow.style.alignItems = Align.Center;
            countRow.style.marginBottom = 16;
            statusSection.Add(countRow);

            var countTitle = new Label("已录制:");
            countTitle.style.width = 80;
            countRow.Add(countTitle);

            _countLabel = new Label("0 条操作");
            _countLabel.style.fontSize = 18;
            _countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _countLabel.style.color = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
            countRow.Add(_countLabel);

            // === 分隔线 ===
            statusSection.Add(CreateSeparator());

            // === 保存路径区域 ===
            var saveHeader = new Label("SAVE PATH");
            saveHeader.AddToClassList("header-label");
            statusSection.Add(saveHeader);

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathRow.style.alignItems = Align.Center;
            statusSection.Add(pathRow);

            _savePathField = new TextField();
            _savePathField.value = _defaultSavePath;
            _savePathField.style.flexGrow = 1;
            _savePathField.style.marginRight = 4;
            pathRow.Add(_savePathField);

            var browseBtn = new Button(OnBrowseButtonClicked) { text = "..." };
            browseBtn.AddToClassList("ue5-button");
            browseBtn.style.width = 30;
            pathRow.Add(browseBtn);

            // 文件名提示
            var filenameHint = new Label("文件名: LiveStreamOp_{时间戳}.json");
            filenameHint.style.fontSize = 10;
            filenameHint.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
            filenameHint.style.marginTop = 4;
            filenameHint.style.marginBottom = 8;
            statusSection.Add(filenameHint);

            // === 控制按钮 ===
            var controlSection = new VisualElement();
            controlSection.style.marginTop = 8;
            controlSection.style.paddingLeft = 8;
            controlSection.style.paddingRight = 8;
            controlSection.style.paddingBottom = 8;
            pane.Add(controlSection);

            // 录制控制按钮行
            var recordRow = new VisualElement();
            recordRow.style.flexDirection = FlexDirection.Row;
            recordRow.style.marginBottom = 8;
            controlSection.Add(recordRow);

            _startRecordBtn = new Button(OnStartRecordClicked) { text = "▶ 开始录制" };
            _startRecordBtn.AddToClassList("ue5-button");
            _startRecordBtn.AddToClassList("ue5-button-accent");
            _startRecordBtn.style.flexGrow = 1;
            _startRecordBtn.style.flexBasis = 0;
            _startRecordBtn.style.marginRight = 4;
            _startRecordBtn.style.height = 36;
            recordRow.Add(_startRecordBtn);

            _stopRecordBtn = new Button(OnStopRecordClicked) { text = "■ 停止录制" };
            _stopRecordBtn.AddToClassList("ue5-button");
            _stopRecordBtn.style.flexGrow = 1;
            _stopRecordBtn.style.flexBasis = 0;
            _stopRecordBtn.style.marginLeft = 4;
            _stopRecordBtn.style.height = 36;
            recordRow.Add(_stopRecordBtn);

            // 保存和清空按钮行
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            controlSection.Add(actionRow);

            _saveBtn = new Button(OnSaveButtonClicked) { text = "💾 保存到文件" };
            _saveBtn.AddToClassList("ue5-button");
            _saveBtn.style.flexGrow = 1;
            _saveBtn.style.flexBasis = 0;
            _saveBtn.style.marginRight = 4;
            _saveBtn.style.height = 36;
            actionRow.Add(_saveBtn);

            _clearBtn = new Button(OnClearButtonClicked) { text = "🗑 清空" };
            _clearBtn.AddToClassList("ue5-button");
            _clearBtn.style.flexGrow = 1;
            _clearBtn.style.flexBasis = 0;
            _clearBtn.style.marginLeft = 4;
            _clearBtn.style.height = 36;
            actionRow.Add(_clearBtn);

            return pane;
        }

        #endregion

        #region Log Area

        private VisualElement CreateLogArea()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.minHeight = 150;
            container.style.flexDirection = FlexDirection.Row;
            container.AddToClassList("rounded-debugger");

            // === 左侧: 日志列表 ===
            var leftPane = new VisualElement();
            leftPane.AddToClassList("debugger-left-pane");
            container.Add(leftPane);

            // 工具栏
            var toolbar = CreateLogToolbar();
            leftPane.Add(toolbar);

            // 日志滚动视图
            _logScrollView = new ScrollView(ScrollViewMode.Vertical);
            _logScrollView.AddToClassList("debugger-log-scroll");
            leftPane.Add(_logScrollView);

            // 状态栏
            var statusBar = new VisualElement();
            statusBar.AddToClassList("ue5-status-bar");
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.justifyContent = Justify.SpaceBetween;
            leftPane.Add(statusBar);

            _logStatusLabel = new Label("记录数量: 0");
            statusBar.Add(_logStatusLabel);

            // === 右侧: 筛选面板 ===
            var rightPane = CreateLogFilterPanel();
            rightPane.AddToClassList("rounded-filter-panel");
            container.Add(rightPane);

            return container;
        }

        private VisualElement CreateLogToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("debugger-toolbar");

            // Header
            var header = new Label("RECORD LOG");
            header.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            header.style.fontSize = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginRight = 15;
            toolbar.Add(header);

            // 清空按钮
            var clearLogBtn = new Button(ClearLogView) { text = "清空" };
            clearLogBtn.AddToClassList("ue5-button");
            clearLogBtn.AddToClassList("ue5-button-small");
            clearLogBtn.style.marginRight = 10;
            toolbar.Add(clearLogBtn);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // 自动滚动按钮
            Button autoScrollBtn = null;
            autoScrollBtn = new Button(() =>
            {
                _autoScroll = !_autoScroll;
                UpdateButtonToggleState(autoScrollBtn, _autoScroll);
            })
            { text = "自动滚动" };
            autoScrollBtn.AddToClassList("debugger-btn-toggle");
            UpdateButtonToggleState(autoScrollBtn, _autoScroll);
            toolbar.Add(autoScrollBtn);

            return toolbar;
        }

        private VisualElement CreateLogFilterPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("debugger-filter-panel");

            // Header
            var header = new Label("筛选");
            header.AddToClassList("header-label");
            panel.Add(header);

            // 筛选按钮
            var toggleContainer = new VisualElement();
            toggleContainer.AddToClassList("debugger-filter-container");
            panel.Add(toggleContainer);

            toggleContainer.Add(CreateFilterButton("🎁 礼物", _showGift, v => { _showGift = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("💬 评论", _showComment, v => { _showComment = v; RefreshLogView(); }));
            toggleContainer.Add(CreateFilterButton("❤ 点赞", _showLike, v => { _showLike = v; RefreshLogView(); }));

            return panel;
        }

        #endregion

        #region UI Helpers

        private Button CreateToggleButton(string label, bool initialActive, Action onClick)
        {
            var btn = new Button(() =>
            {
                onClick?.Invoke();
            })
            { text = label };

            btn.AddToClassList("debugger-filter-btn");
            UpdateToggleButtonState(btn, initialActive);

            return btn;
        }

        private void UpdateToggleButtonState(Button button, bool isActive)
        {
            if (isActive)
            {
                button.RemoveFromClassList("debugger-btn-toggle--inactive");
                button.AddToClassList("debugger-btn-toggle--active");
            }
            else
            {
                button.RemoveFromClassList("debugger-btn-toggle--active");
                button.AddToClassList("debugger-btn-toggle--inactive");
            }
        }

        private Button CreateFilterButton(string label, bool initialValue, Action<bool> onChanged)
        {
            bool isActive = initialValue;
            Button btn = null;
            btn = new Button(() =>
            {
                isActive = !isActive;
                onChanged(isActive);
                UpdateButtonToggleState(btn, isActive);
            })
            { text = label };

            btn.AddToClassList("debugger-filter-btn");
            UpdateButtonToggleState(btn, isActive);

            return btn;
        }

        private void UpdateButtonToggleState(Button button, bool isActive)
        {
            if (isActive)
            {
                button.RemoveFromClassList("debugger-btn-toggle--inactive");
                button.AddToClassList("debugger-btn-toggle--active");
            }
            else
            {
                button.RemoveFromClassList("debugger-btn-toggle--active");
                button.AddToClassList("debugger-btn-toggle--inactive");
            }
        }

        private VisualElement CreateSeparator()
        {
            var separator = new VisualElement();
            separator.AddToClassList("ue5-separator");
            separator.style.marginTop = 8;
            separator.style.marginBottom = 8;
            return separator;
        }

        #endregion

        #region Event Handling

        private void SubscribeToSimulatorEvents()
        {
            if (_isSubscribed) return;

            LiveStreamSimulatorWindow.OnGiftsSent += OnGiftsSent;
            LiveStreamSimulatorWindow.OnCommentsSent += OnCommentsSent;
            LiveStreamSimulatorWindow.OnLikesSent += OnLikesSent;
            _isSubscribed = true;
        }

        private void UnsubscribeFromSimulatorEvents()
        {
            if (!_isSubscribed) return;

            LiveStreamSimulatorWindow.OnGiftsSent -= OnGiftsSent;
            LiveStreamSimulatorWindow.OnCommentsSent -= OnCommentsSent;
            LiveStreamSimulatorWindow.OnLikesSent -= OnLikesSent;
            _isSubscribed = false;
        }

        private void OnGiftsSent(IReadOnlyList<UserDataGift> gifts)
        {
            if (_recorder == null || !_recorder.IsRecording) return;

            _recorder.RecordGifts(gifts);

            // 添加日志条目
            foreach (var gift in gifts)
            {
                AddLogEntry(new RecordLogEntry
                {
                    Timestamp = DateTime.Now,
                    OpType = LiveStreamOpType.Gift,
                    Nickname = gift.Nickname,
                    UserId = gift.UserId,
                    Message = $"赠送 {gift.GiftCount} 个 {gift.GiftName}",
                    Details = $"礼物索引: {gift.GiftIndex}"
                });
            }

            UpdateCountLabel();
        }

        private void OnCommentsSent(IReadOnlyList<UserDataComment> comments)
        {
            if (_recorder == null || !_recorder.IsRecording) return;

            _recorder.RecordComments(comments);

            // 添加日志条目
            foreach (var comment in comments)
            {
                AddLogEntry(new RecordLogEntry
                {
                    Timestamp = DateTime.Now,
                    OpType = LiveStreamOpType.Comment,
                    Nickname = comment.Nickname,
                    UserId = comment.UserId,
                    Message = comment.Comment,
                    Details = null
                });
            }

            UpdateCountLabel();
        }

        private void OnLikesSent(IReadOnlyList<UserDataLike> likes)
        {
            if (_recorder == null || !_recorder.IsRecording) return;

            _recorder.RecordLikes(likes);

            // 添加日志条目
            foreach (var like in likes)
            {
                string likeMsg = like.LikeCount > 1 ? $"点赞 x{like.LikeCount}" : "点赞";
                AddLogEntry(new RecordLogEntry
                {
                    Timestamp = DateTime.Now,
                    OpType = LiveStreamOpType.Like,
                    Nickname = like.Nickname,
                    UserId = like.UserId,
                    Message = likeMsg,
                    Details = like.LikeCount > 1 ? $"点赞数量: {like.LikeCount}" : null
                });
            }

            UpdateCountLabel();
        }

        #endregion

        #region Button Handlers

        private void OnStartRecordClicked()
        {
            // 创建新的录制器
            _recorder = new LiveStreamOpRecorder(_config);
            _recorder.StartRecording();

            // 清除之前的日志
            _logs.Clear();
            _expandedLogs.Clear();
            RefreshLogView();

            UpdateUIState();
            Debug.Log("[LiveStreamRecorderWindow] 开始录制操作");
        }

        private void OnStopRecordClicked()
        {
            if (_recorder == null) return;

            _recorder.StopRecording();
            UpdateUIState();
            Debug.Log($"[LiveStreamRecorderWindow] 停止录制，共录制 {_recorder.RecordedCount} 条操作");
        }

        private void OnSaveButtonClicked()
        {
            if (_recorder == null || _recorder.RecordedCount == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有可保存的数据！", "确定");
                return;
            }

            // 确保目录存在
            string directory = _savePathField.value;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 生成文件名
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"LiveStreamOp_{timestamp}.json";
            string filePath = Path.Combine(directory, filename);

            // 保存
            if (_recorder.SaveToFile(filePath))
            {
                EditorUtility.DisplayDialog("保存成功",
                    $"已保存 {_recorder.RecordedCount} 条操作到:\n{filePath}", "确定");

                // 刷新 AssetDatabase（如果保存在 Assets 目录下）
                if (filePath.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("保存失败", "保存文件时发生错误，请查看控制台日志。", "确定");
            }
        }

        private void OnClearButtonClicked()
        {
            if (_recorder == null) return;

            if (_recorder.RecordedCount > 0)
            {
                if (!EditorUtility.DisplayDialog("确认清空",
                    $"确定要清空 {_recorder.RecordedCount} 条已录制的操作吗？", "确定", "取消"))
                {
                    return;
                }
            }

            _recorder.Clear();
            _logs.Clear();
            _expandedLogs.Clear();
            RefreshLogView();
            UpdateCountLabel();
            Debug.Log("[LiveStreamRecorderWindow] 已清空录制数据");
        }

        private void OnBrowseButtonClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择保存目录", _savePathField.value, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _savePathField.value = selectedPath;
            }
        }

        #endregion

        #region Log View

        private void AddLogEntry(RecordLogEntry entry)
        {
            _logs.Add(entry);

            // 限制日志数量
            while (_logs.Count > MaxLogCount)
            {
                _logs.RemoveAt(0);
                var newExpanded = new HashSet<int>();
                foreach (var idx in _expandedLogs)
                {
                    if (idx > 0) newExpanded.Add(idx - 1);
                }
                _expandedLogs = newExpanded;
            }

            RefreshLogView();
        }

        private void ClearLogView()
        {
            _logs.Clear();
            _expandedLogs.Clear();
            RefreshLogView();
        }

        private void RefreshLogView()
        {
            if (_logScrollView == null) return;

            _logScrollView.Clear();

            int visibleCount = 0;
            for (int i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];
                if (!ShouldShowLog(log.OpType)) continue;

                visibleCount++;
                bool isExpanded = _expandedLogs.Contains(i);
                var logElement = CreateLogElement(log, i, isExpanded);
                _logScrollView.Add(logElement);
            }

            if (_logStatusLabel != null)
                _logStatusLabel.text = $"记录: {visibleCount} / {_logs.Count}";

            if (_autoScroll && _logScrollView.contentContainer.childCount > 0)
            {
                _logScrollView.schedule.Execute(() =>
                {
                    _logScrollView.scrollOffset = new Vector2(0, _logScrollView.contentContainer.layout.height);
                });
            }
        }

        private bool ShouldShowLog(LiveStreamOpType type)
        {
            return type switch
            {
                LiveStreamOpType.Gift => _showGift,
                LiveStreamOpType.Comment => _showComment,
                LiveStreamOpType.Like => _showLike,
                _ => true
            };
        }

        private VisualElement CreateLogElement(RecordLogEntry log, int index, bool isExpanded)
        {
            var wrapper = new VisualElement();

            var container = new VisualElement();
            container.AddToClassList("log-entry");
            wrapper.Add(container);

            string typeIcon = GetLogTypeIcon(log.OpType);
            string colorClass = GetLogTypeColorClass(log.OpType);

            // 展开按钮
            var expandBtn = new Button(() => ToggleExpand(index));
            expandBtn.text = isExpanded ? "▼" : "▶";
            expandBtn.AddToClassList("log-expand-btn");
            container.Add(expandBtn);

            // 类型图标
            var typeLabel = new Label(typeIcon);
            typeLabel.AddToClassList("log-type-icon");
            typeLabel.AddToClassList(colorClass);
            container.Add(typeLabel);

            // 时间戳
            var timeLabel = new Label(log.Timestamp.ToString("HH:mm:ss"));
            timeLabel.AddToClassList("log-timestamp");
            container.Add(timeLabel);

            // 玩家名称
            if (!string.IsNullOrEmpty(log.Nickname))
            {
                var nameLabel = new Label(log.Nickname);
                nameLabel.AddToClassList("log-player-name");
                container.Add(nameLabel);
            }

            // 消息
            var messageLabel = new Label(log.Message);
            messageLabel.AddToClassList("log-message");
            messageLabel.AddToClassList(colorClass);
            container.Add(messageLabel);

            // 双击展开
            container.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2) ToggleExpand(index);
            });

            // 展开详情
            if (isExpanded)
            {
                var detailsSection = new VisualElement();
                detailsSection.AddToClassList("log-details");

                if (!string.IsNullOrEmpty(log.UserId))
                    detailsSection.Add(CreateDetailRow("用户ID", log.UserId));

                detailsSection.Add(CreateDetailRow("时间", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")));

                if (!string.IsNullOrEmpty(log.Details))
                {
                    foreach (var line in log.Details.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                            detailsSection.Add(CreateDetailRow(parts[0].Trim(), parts[1].Trim()));
                        else
                            detailsSection.Add(CreateDetailRow("详情", line));
                    }
                }

                var copyBtn = new Button(() =>
                {
                    string fullLog = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.OpType}]";
                    if (!string.IsNullOrEmpty(log.Nickname)) fullLog += $" [{log.Nickname}]";
                    if (!string.IsNullOrEmpty(log.UserId)) fullLog += $" (ID: {log.UserId})";
                    fullLog += $" {log.Message}";
                    if (!string.IsNullOrEmpty(log.Details)) fullLog += $"\n{log.Details}";
                    EditorGUIUtility.systemCopyBuffer = fullLog;
                })
                { text = "复制" };
                copyBtn.AddToClassList("ue5-button");
                copyBtn.AddToClassList("ue5-button-small");
                copyBtn.style.marginTop = 6;
                copyBtn.style.alignSelf = Align.FlexStart;
                detailsSection.Add(copyBtn);

                wrapper.Add(detailsSection);
            }

            return wrapper;
        }

        private VisualElement CreateDetailRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("log-detail-row");

            var labelElement = new Label(label + ":");
            labelElement.AddToClassList("log-detail-label");
            row.Add(labelElement);

            var valueElement = new Label(value);
            valueElement.AddToClassList("log-detail-value");
            row.Add(valueElement);

            return row;
        }

        private void ToggleExpand(int index)
        {
            if (_expandedLogs.Contains(index))
                _expandedLogs.Remove(index);
            else
                _expandedLogs.Add(index);
            RefreshLogView();
        }

        private string GetLogTypeColorClass(LiveStreamOpType type) => type switch
        {
            LiveStreamOpType.Gift => "log-message-gift",
            LiveStreamOpType.Comment => "log-message-comment",
            LiveStreamOpType.Like => "log-message-like",
            _ => "log-message-info"
        };

        private string GetLogTypeIcon(LiveStreamOpType type) => type switch
        {
            LiveStreamOpType.Gift => "🎁",
            LiveStreamOpType.Comment => "💬",
            LiveStreamOpType.Like => "❤",
            _ => "●"
        };

        #endregion

        #region UI State

        private void UpdateUIState()
        {
            bool isRecording = _recorder != null && _recorder.IsRecording;
            bool hasData = _recorder != null && _recorder.RecordedCount > 0;

            // 更新按钮状态
            _startRecordBtn?.SetEnabled(!isRecording);
            _stopRecordBtn?.SetEnabled(isRecording);
            _saveBtn?.SetEnabled(hasData && !isRecording);
            _clearBtn?.SetEnabled(hasData);

            // 更新配置区域状态（录制时禁用）
            _scaledTimeBtn?.SetEnabled(!isRecording);
            _unscaledTimeBtn?.SetEnabled(!isRecording);
            _recordFromInitBtn?.SetEnabled(!isRecording);
            _recordFromGameStartBtn?.SetEnabled(!isRecording);
            _recordGiftsBtn?.SetEnabled(!isRecording);
            _recordCommentsBtn?.SetEnabled(!isRecording);
            _recordLikesBtn?.SetEnabled(!isRecording);

            // 更新状态标签
            if (_statusLabel != null)
            {
                if (isRecording)
                {
                    _statusLabel.text = "● 录制中";
                    _statusLabel.style.color = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                }
                else if (hasData)
                {
                    _statusLabel.text = "已停止 (有数据)";
                    _statusLabel.style.color = new StyleColor(new Color(0.8f, 0.6f, 0.2f));
                }
                else
                {
                    _statusLabel.text = "未录制";
                    _statusLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
                }
            }

            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            if (_countLabel == null) return;

            int count = _recorder?.RecordedCount ?? 0;
            _countLabel.text = $"{count} 条操作";
        }

        #endregion
    }
}
#endif
