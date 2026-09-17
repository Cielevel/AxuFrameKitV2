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
    /// LiveStream 操作回放窗口
    /// 加载 LiveStreamOpRecorder 保存的 JSON 录制文件，按录制时间戳回放礼物/评论/点赞操作。
    /// 编辑器窗口没有 MonoBehaviour，无法用 SnapshotPlayer.Play 的协程驱动，
    /// 这里用 EditorApplication.update 自驱时间轴（相对时间偏移），不依赖 Play 模式。
    /// </summary>
    public class LiveStreamPlayerWindow : EditorWindow
    {
        [MenuItem("AxuFrameKit/Modules/LiveStream/Operation Player")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LiveStreamPlayerWindow>();
            wnd.titleContent = new GUIContent("LiveStream Player");
            wnd.minSize = new Vector2(600, 450);
        }

        // === Playback State ===
        private SnapshotPlayer<LiveStreamOpData> _player;
        private LiveStreamOpPlaybackHandler _handler;
        private IReadOnlyList<TimestampedSnapshot<LiveStreamOpData>> _snapshots;
        private int _cursor;
        private bool _playing;
        // 用 double 记时间基准：编辑器长开会话时 realtimeSinceStartup 量级大，float 大数相减误差会累积
        private double _playStartRealtime;
        private double _firstTimestamp;
        private const int MaxLogCount = 200;

        // === UI Elements ===
        private TextField _pathField;
        private Button _loadBtn;
        private Button _playBtn;
        private Button _stopBtn;
        private Button _flushBtn;
        private Button _clearLogBtn;
        private Button _useSelectedBtn;
        private Label _statusLabel;
        private Label _countLabel;
        private ScrollView _logScrollView;

        private struct PlayerLogEntry
        {
            public DateTime Time;
            public string Message;
            public string Details;
        }

        private readonly List<PlayerLogEntry> _logs = new List<PlayerLogEntry>();

        private void OnEnable()
        {
            if (_pathField != null && string.IsNullOrEmpty(_pathField.value))
            {
                _pathField.value = Path.Combine(Application.dataPath, "LiveStreamRecordings");
            }
            _handler = new LiveStreamOpPlaybackHandler();
            BindHandler();
        }

        private void OnDisable()
        {
            StopPlayback();
            EditorApplication.update -= OnEditorUpdate;
        }

        public void CreateGUI()
        {
            // 复用 Simulator 同款样式表
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/LiveStreamSimulatorWindow.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            rootVisualElement.AddToClassList("ue5-root");

            var splitView = new TwoPaneSplitView(1, 220, TwoPaneSplitViewOrientation.Vertical);
            splitView.AddToClassList("main-vertical-split");
            splitView.style.flexGrow = 1;
            rootVisualElement.Add(splitView);

            var controlArea = CreateControlArea();
            controlArea.style.flexGrow = 1;
            controlArea.style.minHeight = 160;
            splitView.Add(controlArea);

            var logArea = CreateLogArea();
            logArea.style.flexGrow = 1;
            logArea.style.minHeight = 150;
            splitView.Add(logArea);

            UpdateUIState();
        }

        #region Control Area

        private VisualElement CreateControlArea()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.flexGrow = 1;

            // 路径行
            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathRow.style.marginBottom = 8;
            container.Add(pathRow);

            _pathField = new TextField { value = Path.Combine(Application.dataPath, "LiveStreamRecordings") };
            _pathField.style.flexGrow = 1;
            _pathField.style.marginRight = 6;
            pathRow.Add(_pathField);

            var browseBtn = new Button(OnBrowseClicked) { text = "..." };
            browseBtn.AddToClassList("ue5-button");
            browseBtn.style.width = 30;
            browseBtn.style.marginRight = 6;
            pathRow.Add(browseBtn);

            _useSelectedBtn = new Button(OnUseSelectedClicked) { text = "📌 使用选中文件" };
            _useSelectedBtn.AddToClassList("ue5-button");
            _useSelectedBtn.style.width = 110;
            pathRow.Add(_useSelectedBtn);

            // 操作按钮行
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginBottom = 12;
            container.Add(btnRow);

            _loadBtn = new Button(OnLoadClicked) { text = "📂 加载录制" };
            _loadBtn.AddToClassList("ue5-button");
            _loadBtn.style.flexGrow = 1;
            _loadBtn.style.flexBasis = 0;
            _loadBtn.style.marginRight = 4;
            btnRow.Add(_loadBtn);

            _playBtn = new Button(OnPlayClicked) { text = "▶ 开始回放" };
            _playBtn.AddToClassList("ue5-button");
            _playBtn.AddToClassList("ue5-button-accent");
            _playBtn.style.flexGrow = 1;
            _playBtn.style.flexBasis = 0;
            _playBtn.style.marginRight = 4;
            btnRow.Add(_playBtn);

            _stopBtn = new Button(OnStopClicked) { text = "■ 停止" };
            _stopBtn.AddToClassList("ue5-button");
            _stopBtn.style.flexGrow = 1;
            _stopBtn.style.flexBasis = 0;
            _stopBtn.style.marginRight = 4;
            btnRow.Add(_stopBtn);

            _flushBtn = new Button(OnFlushClicked) { text = "⚡ 立即回放" };
            _flushBtn.AddToClassList("ue5-button");
            _flushBtn.style.flexGrow = 1;
            _flushBtn.style.flexBasis = 0;
            _flushBtn.style.marginRight = 4;
            btnRow.Add(_flushBtn);

            _clearLogBtn = new Button(OnClearLogClicked) { text = "🗑 清空日志" };
            _clearLogBtn.AddToClassList("ue5-button");
            _clearLogBtn.style.flexGrow = 1;
            _clearLogBtn.style.flexBasis = 0;
            btnRow.Add(_clearLogBtn);

            // 状态行
            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.alignItems = Align.Center;
            container.Add(statusRow);

            _statusLabel = new Label("未加载录制文件");
            _statusLabel.style.flexGrow = 1;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusLabel.style.fontSize = 14;
            statusRow.Add(_statusLabel);

            _countLabel = new Label("");
            _countLabel.style.color = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
            _countLabel.style.fontSize = 14;
            _countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusRow.Add(_countLabel);

            return container;
        }

        #endregion

        #region Log Area

        private VisualElement CreateLogArea()
        {
            var container = new VisualElement();
            container.AddToClassList("rounded-debugger");
            container.style.flexGrow = 1;

            _logScrollView = new ScrollView(ScrollViewMode.Vertical);
            _logScrollView.AddToClassList("debugger-log-scroll");
            container.Add(_logScrollView);

            return container;
        }

        private void AddLog(string message, string details = null)
        {
            _logs.Add(new PlayerLogEntry { Time = DateTime.Now, Message = message, Details = details });
            while (_logs.Count > MaxLogCount)
            {
                _logs.RemoveAt(0);
            }
            RefreshLogView();
        }

        private void RefreshLogView()
        {
            if (_logScrollView == null) return;

            _logScrollView.Clear();
            foreach (var log in _logs)
            {
                var row = new VisualElement();
                row.AddToClassList("log-entry");
                row.style.paddingTop = 2;
                row.style.paddingBottom = 2;

                var timeLabel = new Label(log.Time.ToString("HH:mm:ss"));
                timeLabel.AddToClassList("log-timestamp");
                row.Add(timeLabel);

                var messageLabel = new Label(log.Message);
                messageLabel.AddToClassList("log-message");
                row.Add(messageLabel);

                if (!string.IsNullOrEmpty(log.Details))
                {
                    var detailsLabel = new Label(log.Details);
                    detailsLabel.AddToClassList("log-details");
                    row.Add(detailsLabel);
                }

                _logScrollView.Add(row);
            }

            _logScrollView.schedule.Execute(() =>
            {
                _logScrollView.scrollOffset = new Vector2(0, _logScrollView.contentContainer.layout.height);
            });
        }

        private void OnClearLogClicked()
        {
            _logs.Clear();
            RefreshLogView();
        }

        #endregion

        #region Playback Logic

        private void OnBrowseClicked()
        {
            string selected = EditorUtility.OpenFilePanel("选择录制文件", _pathField.value, "json");
            if (!string.IsNullOrEmpty(selected))
            {
                _pathField.value = selected;
            }
        }

        private void OnUseSelectedClicked()
        {
            string path = GetSelectedJsonPath();
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中一个 .json 录制文件。", "确定");
                return;
            }

            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("提示", $"所选文件不存在：\n{path}", "确定");
                return;
            }

            // 尝试解析：能通过 LoadFromFile 解析即视为有效录制文件
            var player = LiveStreamOpRecorder.LoadFromFile(path);
            if (player == null)
            {
                EditorUtility.DisplayDialog("解析失败", "所选文件不是有效的 LiveStream 录制文件。", "确定");
                return;
            }

            _pathField.value = path;
            _statusLabel.text = $"已选定文件（{player.TotalCount} 条操作），可点击加载";
            AddLog($"已选定录制文件：{path}");
        }

        private static string GetSelectedJsonPath()
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }
            return null;
        }

        private void OnLoadClicked()
        {
            string path = _pathField.value;
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("提示", $"文件不存在：\n{path}", "确定");
                return;
            }

            StopPlayback();

            var player = LiveStreamOpRecorder.LoadFromFile(path);
            if (player == null)
            {
                EditorUtility.DisplayDialog("加载失败", "文件解析失败，请查看 Console 日志。", "确定");
                return;
            }

            _player = player;
            _snapshots = player.GetAllSnapshots();
            _cursor = 0;
            _handler.ClearCallbacks();
            BindHandler();
            _logs.Clear();
            RefreshLogView();

            _countLabel.text = $"已加载 {player.TotalCount} 条操作";
            _statusLabel.text = "已加载，可开始回放";
            AddLog($"已加载录制文件：{Path.GetFileName(path)}（{player.TotalCount} 条操作）");
            UpdateUIState();
        }

        private void BindHandler()
        {
            if (_handler == null) return;
            _handler.OnGiftPlayed += OnGiftPlayed;
            _handler.OnCommentPlayed += OnCommentPlayed;
            _handler.OnLikePlayed += OnLikePlayed;
        }

        private void OnGiftPlayed(UserDataGift gift, float timestamp)
        {
            Debug.Log($"[LiveStream] 回放礼物: {gift.Nickname} 赠送 {gift.GiftCount} 个 {gift.GiftName}");
            AddLog($"🎁 {gift.Nickname} 赠送 {gift.GiftCount} 个 {gift.GiftName}",
                $"礼物索引: {gift.GiftIndex}  录制时间: {timestamp:F2}s");
        }

        private void OnCommentPlayed(UserDataComment comment, float timestamp)
        {
            Debug.Log($"[LiveStream] 回放评论: {comment.Nickname}: {comment.Comment}");
            AddLog($"💬 {comment.Nickname}: {comment.Comment}", $"录制时间: {timestamp:F2}s");
        }

        private void OnLikePlayed(UserDataLike like, float timestamp)
        {
            Debug.Log($"[LiveStream] 回放点赞: {like.Nickname} x{like.LikeCount}");
            AddLog($"❤ {like.Nickname} 点赞 x{like.LikeCount}", $"录制时间: {timestamp:F2}s");
        }

        private void OnPlayClicked()
        {
            if (_player == null || _snapshots == null || _snapshots.Count == 0) return;
            if (_cursor >= _snapshots.Count) _cursor = 0;

            _playing = true;
            _firstTimestamp = _snapshots[0].Timestamp;
            // 恢复播放时把已回放部分的时间偏移计入基准，让剩余快照从暂停位置立即继续，而非从头重新等待
            _playStartRealtime = Time.realtimeSinceStartupAsDouble
                                 - Math.Max(0d, _snapshots[_cursor].Timestamp - _firstTimestamp);
            _statusLabel.text = "● 回放中";
            EditorApplication.update += OnEditorUpdate;
            UpdateUIState();
        }

        private void OnEditorUpdate()
        {
            if (!_playing) return;

            double elapsed = Time.realtimeSinceStartupAsDouble - _playStartRealtime;
            while (_cursor < _snapshots.Count)
            {
                // 以第一条录制时间为基准取相对偏移，兼容 RecordFromInit / RecordFromGameStart 两种录制模式
                double delay = Math.Max(0d, _snapshots[_cursor].Timestamp - _firstTimestamp);
                if (delay > elapsed) break;

                var snapshot = _snapshots[_cursor];
                _handler.OnSnapshotOutput(snapshot.Data, snapshot.Timestamp);
                _cursor++;
            }

            _countLabel.text = $"已回放 {_cursor} / {_snapshots.Count}";
            Repaint();

            if (_cursor >= _snapshots.Count)
            {
                StopPlayback();
                _statusLabel.text = "✔ 回放完成";
                _countLabel.text = $"已回放 {_snapshots.Count} / {_snapshots.Count}";
                AddLog("回放完成");
            }
        }

        private void OnStopClicked()
        {
            StopPlayback();
            _statusLabel.text = "已停止（可继续回放剩余操作）";
        }

        private void OnFlushClicked()
        {
            if (_player == null || _snapshots == null || _snapshots.Count == 0) return;

            StopPlayback();
            while (_cursor < _snapshots.Count)
            {
                var snapshot = _snapshots[_cursor];
                _handler.OnSnapshotOutput(snapshot.Data, snapshot.Timestamp);
                _cursor++;
            }
            _countLabel.text = $"已回放 {_cursor} / {_snapshots.Count}";
            _statusLabel.text = "✔ 已立即回放全部操作";
            AddLog("已立即回放全部操作");
            UpdateUIState();
        }

        private void StopPlayback()
        {
            if (_playing)
            {
                EditorApplication.update -= OnEditorUpdate;
                _playing = false;
            }
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool hasData = _player != null && _snapshots != null && _snapshots.Count > 0;
            bool hasRemaining = hasData && _cursor < _snapshots.Count;

            _playBtn?.SetEnabled(hasRemaining && !_playing);
            _stopBtn?.SetEnabled(_playing);
            _flushBtn?.SetEnabled(hasRemaining);
            _clearLogBtn?.SetEnabled(_logs.Count > 0);
        }

        #endregion
    }
}
#endif