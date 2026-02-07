using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using AxuFrameKit._Core.Utility.RandomX;

namespace AxuFrameKit.Common.Editor.Window
{
    /// <summary>
    /// 头像批量下载窗口
    /// </summary>
    public class AvatarDownloaderWindow : EditorWindow
    {
        #region Constants

        private const string PrefsKeyRobot = "AvatarDownloader_Robot";
        private const string PrefsKeyPixel = "AvatarDownloader_Pixel";
        private const string PrefsKeyHuman = "AvatarDownloader_Human";
        private const string PrefsKeyCount = "AvatarDownloader_Count";
        private const string PrefsKeySavePath = "AvatarDownloader_SavePath";

        #endregion

        #region Fields

        // Settings
        private bool _enableRobot = true;
        private bool _enablePixel = true;
        private bool _enableHuman = true;
        private int _downloadCount = 10;
        private string _savePath = "Assets/DownloadedAvatars";

        // UI Elements
        private Button _robotBtn;
        private Button _pixelBtn;
        private Button _humanBtn;
        private IntegerField _countField;
        private TextField _savePathField;
        private Button _downloadBtn;
        private Button _browseBtn;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private ScrollView _previewScrollView;

        // Download state
        private bool _isDownloading = false;
        private int _completedCount = 0;
        private int _totalCount = 0;
        private List<string> _downloadedFiles = new List<string>();

        #endregion

        [MenuItem("AxuFrameKit/Avatar Downloader")]
        public static void ShowWindow()
        {
            var window = GetWindow<AvatarDownloaderWindow>("Avatar Downloader");
            window.minSize = new Vector2(450, 550);
        }

        private void OnEnable()
        {
            LoadPreferences();
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        private void CreateGUI()
        {
            // Load and add USS
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/AxuFrameKit/Common/Editor/Window/CommonWindowStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("ue5-root");

            // Window Title
            var titleLabel = new Label("🖼️ Avatar Batch Downloader");
            titleLabel.AddToClassList("window-title");
            rootVisualElement.Add(titleLabel);

            // Style Selection Panel
            CreateStyleSelectionPanel();

            // Settings Panel
            CreateSettingsPanel();

            // Progress Panel
            CreateProgressPanel();

            // Preview Panel
            CreatePreviewPanel();

            // Initial state
            UpdateDownloadButtonState();
        }

        private void CreateStyleSelectionPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("🎨 头像风格选择");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            // Toggle button container with flex row (matching SelectionToCsvRevPage style)
            var toggleRow = new VisualElement();
            toggleRow.style.flexDirection = FlexDirection.Row;
            toggleRow.style.flexWrap = Wrap.Wrap;
            toggleRow.style.paddingTop = 8;
            toggleRow.style.paddingBottom = 8;
            content.Add(toggleRow);

            // Robot Toggle Button
            _robotBtn = CreateStyleToggleButton("🤖 Robot", "机器人风格头像 (robohash.org)", _enableRobot);
            _robotBtn.clicked += () => ToggleStyle(ref _enableRobot, _robotBtn);
            toggleRow.Add(_robotBtn);

            // Pixel Toggle Button
            _pixelBtn = CreateStyleToggleButton("👾 Pixel", "像素艺术风格头像 (dicebear.com)", _enablePixel);
            _pixelBtn.clicked += () => ToggleStyle(ref _enablePixel, _pixelBtn);
            toggleRow.Add(_pixelBtn);

            // Human Toggle Button
            _humanBtn = CreateStyleToggleButton("👤 Human", "真人风格头像 (xsgames.co)", _enableHuman);
            _humanBtn.clicked += () => ToggleStyle(ref _enableHuman, _humanBtn);
            toggleRow.Add(_humanBtn);

            // Helper buttons row
            var helperRow = new VisualElement();
            helperRow.AddToClassList("helper-btn-row");
            content.Add(helperRow);

            var selectAllBtn = new Button(() => SetAllStyles(true)) { text = "全选" };
            selectAllBtn.AddToClassList("helper-text-btn");
            helperRow.Add(selectAllBtn);

            var selectNoneBtn = new Button(() => SetAllStyles(false)) { text = "全不选" };
            selectNoneBtn.AddToClassList("helper-text-btn");
            helperRow.Add(selectNoneBtn);

            // Help box
            var helpBox = new VisualElement();
            helpBox.AddToClassList("help-box");
            helpBox.style.marginTop = 8;
            content.Add(helpBox);

            var helpText = new Label("💡 选择一种或多种头像风格，下载数量将均匀分配到各风格中");
            helpText.AddToClassList("help-box-text");
            helpBox.Add(helpText);
        }

        private Button CreateStyleToggleButton(string label, string tooltip, bool isActive)
        {
            var btn = new Button() { text = label };
            btn.tooltip = tooltip;
            btn.AddToClassList("column-toggle-btn");
            if (isActive)
            {
                btn.AddToClassList("active");
            }
            return btn;
        }

        private void ToggleStyle(ref bool styleState, Button btn)
        {
            styleState = !styleState;
            if (styleState)
            {
                btn.AddToClassList("active");
            }
            else
            {
                btn.RemoveFromClassList("active");
            }
            UpdateDownloadButtonState();
        }

        private void SetAllStyles(bool state)
        {
            _enableRobot = state;
            _enablePixel = state;
            _enableHuman = state;

            UpdateButtonActiveState(_robotBtn, state);
            UpdateButtonActiveState(_pixelBtn, state);
            UpdateButtonActiveState(_humanBtn, state);

            UpdateDownloadButtonState();
        }

        private void UpdateButtonActiveState(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.AddToClassList("active");
            }
            else
            {
                btn.RemoveFromClassList("active");
            }
        }

        private void CreateSettingsPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("⚙️ 下载设置");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            // Download Count Row
            var countRow = new VisualElement();
            countRow.AddToClassList("item-row");
            content.Add(countRow);

            var countLabel = new Label("下载数量:");
            countLabel.AddToClassList("toolbar-label");
            countLabel.style.minWidth = 80;
            countRow.Add(countLabel);

            _countField = new IntegerField();
            _countField.value = _downloadCount;
            _countField.style.flexGrow = 1;
            _countField.style.minWidth = 100;
            _countField.RegisterValueChangedCallback(evt =>
            {
                _downloadCount = Mathf.Max(1, evt.newValue);
                _countField.SetValueWithoutNotify(_downloadCount);
            });
            countRow.Add(_countField);

            var countHint = new Label("张头像");
            countHint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            countHint.style.marginLeft = 8;
            countRow.Add(countHint);

            // Save Path Row (with drag-and-drop support)
            var pathRow = new VisualElement();
            pathRow.AddToClassList("item-row");
            pathRow.style.position = Position.Relative;
            content.Add(pathRow);

            var pathLabel = new Label("保存路径:");
            pathLabel.AddToClassList("toolbar-label");
            pathLabel.style.minWidth = 80;
            pathRow.Add(pathLabel);

            // Create a drop zone container for the text field
            var dropZone = new VisualElement();
            dropZone.style.flexGrow = 1;
            dropZone.style.flexDirection = FlexDirection.Row;
            dropZone.style.alignItems = Align.Center;
            pathRow.Add(dropZone);

            _savePathField = new TextField();
            _savePathField.AddToClassList("ue5-text-field");
            _savePathField.value = _savePath;
            _savePathField.style.flexGrow = 1;
            _savePathField.RegisterValueChangedCallback(evt =>
            {
                _savePath = evt.newValue;
            });
            dropZone.Add(_savePathField);

            // Drag hint label (hidden by default)
            var dragHint = new Label("📂 拖放文件夹到此处");
            dragHint.style.position = Position.Absolute;
            dragHint.style.left = 0;
            dragHint.style.right = 0;
            dragHint.style.top = 0;
            dragHint.style.bottom = 0;
            dragHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            dragHint.style.backgroundColor = new Color(1f, 0.58f, 0f, 0.2f);
            dragHint.style.color = new Color(1f, 0.58f, 0f, 1f);
            dragHint.style.unityFontStyleAndWeight = FontStyle.Bold;
            dragHint.style.borderTopLeftRadius = 4;
            dragHint.style.borderTopRightRadius = 4;
            dragHint.style.borderBottomLeftRadius = 4;
            dragHint.style.borderBottomRightRadius = 4;
            dragHint.style.borderTopWidth = 2;
            dragHint.style.borderBottomWidth = 2;
            dragHint.style.borderLeftWidth = 2;
            dragHint.style.borderRightWidth = 2;
            dragHint.style.borderTopColor = new Color(1f, 0.58f, 0f, 0.6f);
            dragHint.style.borderBottomColor = new Color(1f, 0.58f, 0f, 0.6f);
            dragHint.style.borderLeftColor = new Color(1f, 0.58f, 0f, 0.6f);
            dragHint.style.borderRightColor = new Color(1f, 0.58f, 0f, 0.6f);
            dragHint.style.display = DisplayStyle.None;
            dropZone.Add(dragHint);

            // Setup drag-and-drop events
            SetupDragAndDrop(dropZone, dragHint);

            _browseBtn = new Button(OnBrowseClicked) { text = "📂" };
            _browseBtn.AddToClassList("action-btn");
            pathRow.Add(_browseBtn);

            // Drag hint for path row
            var pathHelpBox = new VisualElement();
            pathHelpBox.AddToClassList("help-box");
            pathHelpBox.style.marginTop = 8;
            pathHelpBox.style.paddingTop = 6;
            pathHelpBox.style.paddingBottom = 6;
            content.Add(pathHelpBox);

            var pathHelpText = new Label("💡 支持直接拖放 Project 中的文件夹到输入框");
            pathHelpText.AddToClassList("help-box-text");
            pathHelpText.style.fontSize = 10;
            pathHelpBox.Add(pathHelpText);

            // Download Button Row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = 12;
            buttonRow.style.marginBottom = 8;
            content.Add(buttonRow);

            _downloadBtn = new Button(OnDownloadClicked) { text = "🚀 开始下载" };
            _downloadBtn.AddToClassList("primary-btn");
            _downloadBtn.style.minWidth = 150;
            _downloadBtn.style.height = 32;
            _downloadBtn.style.fontSize = 13;
            buttonRow.Add(_downloadBtn);
        }

        private void SetupDragAndDrop(VisualElement dropZone, Label dragHint)
        {
            // Register drag enter - show hint
            dropZone.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (IsSingleFolderDrag())
                {
                    dragHint.style.display = DisplayStyle.Flex;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
            });

            // Register drag leave - hide hint
            dropZone.RegisterCallback<DragLeaveEvent>(evt =>
            {
                dragHint.style.display = DisplayStyle.None;
            });

            // Register drag updated - update visual mode
            dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (IsSingleFolderDrag())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
            });

            // Register drag perform - accept the drop
            dropZone.RegisterCallback<DragPerformEvent>(evt =>
            {
                dragHint.style.display = DisplayStyle.None;

                if (!IsSingleFolderDrag())
                {
                    EditorUtility.DisplayDialog("提示", "请只拖放一个文件夹！", "确定");
                    return;
                }

                DragAndDrop.AcceptDrag();

                string folderPath = DragAndDrop.paths[0];
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    _savePath = folderPath;
                    _savePathField.SetValueWithoutNotify(_savePath);
                }
            });

            // Register drag exited - cleanup
            dropZone.RegisterCallback<DragExitedEvent>(evt =>
            {
                dragHint.style.display = DisplayStyle.None;
            });
        }

        private bool IsSingleFolderDrag()
        {
            // Check if exactly one item is being dragged
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length != 1)
            {
                return false;
            }

            // Check if the dragged item is a folder
            string path = DragAndDrop.paths[0];
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }

        private void CreateProgressPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("📊 下载进度");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.paddingTop = 12;
            content.style.paddingBottom = 12;
            container.Add(content);

            // Status Label
            _statusLabel = new Label("准备就绪");
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _statusLabel.style.marginBottom = 8;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            content.Add(_statusLabel);

            // Progress Bar
            _progressBar = new ProgressBar();
            _progressBar.title = "0%";
            _progressBar.lowValue = 0;
            _progressBar.highValue = 100;
            _progressBar.value = 0;
            _progressBar.style.height = 20;
            content.Add(_progressBar);
        }

        private void CreatePreviewPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;
            container.style.minHeight = 120;
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("📁 已下载文件");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            _previewScrollView = new ScrollView(ScrollViewMode.Vertical);
            _previewScrollView.AddToClassList("group-content");
            _previewScrollView.style.flexGrow = 1;
            container.Add(_previewScrollView);

            // Empty state
            UpdatePreviewList();
        }

        private void UpdatePreviewList()
        {
            _previewScrollView.Clear();

            if (_downloadedFiles.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label("暂无已下载文件");
                emptyText.AddToClassList("empty-state-text");
                emptyState.Add(emptyText);
                _previewScrollView.Add(emptyState);
            }
            else
            {
                foreach (var filePath in _downloadedFiles)
                {
                    var row = new VisualElement();
                    row.AddToClassList("item-row");

                    var fileName = Path.GetFileName(filePath);
                    var nameLabel = new Label($"✅ {fileName}");
                    nameLabel.AddToClassList("item-name");
                    nameLabel.style.color = new Color(0.5f, 0.9f, 0.5f, 1f);
                    row.Add(nameLabel);

                    var locateBtn = new Button(() => OnLocateFile(filePath)) { text = "📍" };
                    locateBtn.AddToClassList("action-btn");
                    row.Add(locateBtn);

                    _previewScrollView.Add(row);
                }
            }
        }

        #region Event Handlers

        private void UpdateDownloadButtonState()
        {
            if (_downloadBtn == null) return;

            bool anyEnabled = _enableRobot || _enablePixel || _enableHuman;
            _downloadBtn.SetEnabled(anyEnabled && !_isDownloading);

            if (!anyEnabled)
            {
                _downloadBtn.text = "⚠️ 请至少选择一种风格";
            }
            else if (_isDownloading)
            {
                _downloadBtn.text = "⏳ 下载中...";
            }
            else
            {
                _downloadBtn.text = "🚀 开始下载";
            }
        }

        private void OnBrowseClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择保存路径", Application.dataPath, "DownloadedAvatars");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert to relative path if inside Assets
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                _savePath = selectedPath;
                _savePathField.SetValueWithoutNotify(_savePath);
            }
        }

        private async void OnDownloadClicked()
        {
            if (_isDownloading) return;

            // Validate settings
            if (!_enableRobot && !_enablePixel && !_enableHuman)
            {
                EditorUtility.DisplayDialog("错误", "请至少选择一种头像风格！", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(_savePath))
            {
                EditorUtility.DisplayDialog("错误", "请指定保存路径！", "确定");
                return;
            }

            // Create directory if not exists
            string fullPath = Path.GetFullPath(_savePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Start download
            _isDownloading = true;
            _downloadedFiles.Clear();
            UpdateDownloadButtonState();
            UpdatePreviewList();

            try
            {
                await StartBatchDownload(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"下载出错: {ex.Message}");
                _statusLabel.text = $"❌ 下载出错: {ex.Message}";
            }
            finally
            {
                _isDownloading = false;
                UpdateDownloadButtonState();
                AssetDatabase.Refresh();

                // Set downloaded textures to Sprite type
                SetTextureImportSettings();
            }
        }

        private void SetTextureImportSettings()
        {
            if (_downloadedFiles.Count == 0) return;

            _statusLabel.text = "⚙️ 正在设置纹理导入参数...";

            int processedCount = 0;
            foreach (var assetPath in _downloadedFiles)
            {
                try
                {
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.mipmapEnabled = false;
                        importer.alphaIsTransparency = true;
                        importer.SaveAndReimport();
                        processedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"设置纹理参数失败: {assetPath}, 错误: {ex.Message}");
                }
            }

            _statusLabel.text = $"🎉 下载完成！成功 {_downloadedFiles.Count}/{_totalCount} 张头像，已设置为 Sprite 类型";
        }

        private async Task StartBatchDownload(string savePath)
        {
            // Calculate distribution
            List<string> enabledStyles = new List<string>();
            if (_enableRobot) enabledStyles.Add("robot");
            if (_enablePixel) enabledStyles.Add("pixel");
            if (_enableHuman) enabledStyles.Add("human");

            int countPerStyle = _downloadCount / enabledStyles.Count;
            int remainder = _downloadCount % enabledStyles.Count;

            _totalCount = _downloadCount;
            _completedCount = 0;

            _statusLabel.text = $"开始下载 {_totalCount} 张头像...";
            _progressBar.value = 0;
            _progressBar.title = "0%";

            // Generate URLs for each style
            List<(string url, string style, int index)> downloadTasks = new List<(string, string, int)>();
            int globalIndex = 0;

            for (int i = 0; i < enabledStyles.Count; i++)
            {
                string style = enabledStyles[i];
                int count = countPerStyle + (i < remainder ? 1 : 0);

                for (int j = 0; j < count; j++)
                {
                    string url = GenerateAvatarUrl(style);
                    downloadTasks.Add((url, style, globalIndex++));
                }
            }

            // Download each avatar
            foreach (var task in downloadTasks)
            {
                string fileName = $"{task.style}_{task.index:D4}_{DateTime.Now.Ticks}.png";
                string filePath = Path.Combine(savePath, fileName);

                bool success = await DownloadImage(task.url, filePath);

                _completedCount++;
                float progress = (float)_completedCount / _totalCount * 100f;
                _progressBar.value = progress;
                _progressBar.title = $"{progress:F0}%";

                if (success)
                {
                    string relativePath = filePath;
                    if (filePath.Contains("Assets"))
                    {
                        relativePath = "Assets" + filePath.Substring(filePath.IndexOf("Assets") + 6);
                    }
                    _downloadedFiles.Add(relativePath);
                    _statusLabel.text = $"✅ 已下载 {_completedCount}/{_totalCount}: {fileName}";
                }
                else
                {
                    _statusLabel.text = $"⚠️ 下载失败 {_completedCount}/{_totalCount}: {fileName}";
                }

                UpdatePreviewList();

                // Small delay to avoid overwhelming the servers
                await Task.Delay(100);
            }

            _statusLabel.text = $"🎉 下载完成！成功 {_downloadedFiles.Count}/{_totalCount} 张头像";
        }

        private string GenerateAvatarUrl(string style)
        {
            return style switch
            {
                "robot" => RandomAvatarUrl.GetRobot(),
                "pixel" => RandomAvatarUrl.GetPixel(),
                "human" => RandomAvatarUrl.GetHuman((RandomAvatarUrl.HumanType)UnityEngine.Random.Range(0, 3)),
                _ => RandomAvatarUrl.GetRobot()
            };
        }

        private async Task<bool> DownloadImage(string url, string savePath)
        {
            try
            {
                using var request = UnityWebRequest.Get(url);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Delay(50);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = request.downloadHandler.data;
                    await Task.Run(() => File.WriteAllBytes(savePath, data));
                    return true;
                }
                else
                {
                    Debug.LogWarning($"下载失败: {url}, 错误: {request.error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"下载异常: {url}, 错误: {ex.Message}");
                return false;
            }
        }

        private void OnLocateFile(string filePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        #endregion

        #region Preferences

        private void LoadPreferences()
        {
            _enableRobot = EditorPrefs.GetBool(PrefsKeyRobot, true);
            _enablePixel = EditorPrefs.GetBool(PrefsKeyPixel, true);
            _enableHuman = EditorPrefs.GetBool(PrefsKeyHuman, true);
            _downloadCount = EditorPrefs.GetInt(PrefsKeyCount, 10);
            _savePath = EditorPrefs.GetString(PrefsKeySavePath, "Assets/DownloadedAvatars");
        }

        private void SavePreferences()
        {
            EditorPrefs.SetBool(PrefsKeyRobot, _enableRobot);
            EditorPrefs.SetBool(PrefsKeyPixel, _enablePixel);
            EditorPrefs.SetBool(PrefsKeyHuman, _enableHuman);
            EditorPrefs.SetInt(PrefsKeyCount, _downloadCount);
            EditorPrefs.SetString(PrefsKeySavePath, _savePath);
        }

        #endregion
    }
}
