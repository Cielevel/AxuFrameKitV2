using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AxuFrameKit.Common.Editor.Window
{
    /// <summary>
    /// Sprite序列转AnimationClip工具窗口
    /// 支持将选中的Sprites快速整合为AnimationClip，并可选创建Animator Controller
    /// </summary>
    public class SpriteToAnimationWindow : EditorWindow
    {
        #region Constants

        private const string PrefsKeySavePath = "SpriteToAnim_SavePath";
        private const string PrefsKeyFrameRate = "SpriteToAnim_FrameRate";
        private const string PrefsKeyClipName = "SpriteToAnim_ClipName";
        private const string PrefsKeyCreateAnimator = "SpriteToAnim_CreateAnimator";
        private const string PrefsKeyAnimatorName = "SpriteToAnim_AnimatorName";
        private const string PrefsKeyLoopAnimation = "SpriteToAnim_LoopAnimation";
        private const string PrefsKeySetAsDefault = "SpriteToAnim_SetAsDefault";

        #endregion

        #region Fields

        // Settings
        private string _savePath = "Assets";
        private string _clipName = "NewAnimation";
        private int _frameRate = 12;
        private bool _createAnimator = false;
        private string _animatorName = "NewAnimator";
        private bool _loopAnimation = true;
        private bool _setAsDefault = false;

        // UI Elements
        private TextField _savePathField;
        private TextField _clipNameField;
        private IntegerField _frameRateField;
        private Toggle _createAnimatorToggle;
        private VisualElement _animatorSettingsContainer;
        private TextField _animatorNameField;
        private Toggle _loopAnimationToggle;
        private Toggle _setAsDefaultToggle;
        private Button _createBtn;
        private ScrollView _spriteListScrollView;
        private Label _statusLabel;

        // Sprite data
        private List<Sprite> _selectedSprites = new List<Sprite>();

        #endregion

        [MenuItem("AxuFrameKit/Utility/Sprite To Animation")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteToAnimationWindow>("Sprite To Animation");
            window.minSize = new Vector2(450, 520);
        }

        private void OnEnable()
        {
            LoadPreferences();
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            SavePreferences();
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            RefreshSpriteList();
        }

        private void CreateGUI()
        {
            // Load and add USS
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/CommonWindowStyles.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("ue5-root");

            // Window Title
            var titleLabel = new Label("🎬 Sprite To Animation");
            titleLabel.AddToClassList("window-title");
            rootVisualElement.Add(titleLabel);

            // Selected Sprites Panel
            CreateSpritesPanel();

            // Animation Settings Panel
            CreateAnimationSettingsPanel();

            // Animator Settings Panel
            CreateAnimatorSettingsPanel();

            // Action Panel
            CreateActionPanel();

            // Initial refresh
            RefreshSpriteList();
            UpdateCreateButtonState();
        }

        #region UI Creation

        private void CreateSpritesPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;
            container.style.minHeight = 150;
            container.style.maxHeight = 220;
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("🖼️ 已选择的Sprites");
            title.AddToClassList("group-title");
            header.Add(title);

            // Helper buttons
            var helperRow = new VisualElement();
            helperRow.style.flexDirection = FlexDirection.Row;
            helperRow.style.marginLeft = StyleKeyword.Auto;
            header.Add(helperRow);

            var refreshBtn = new Button(RefreshSpriteList) { text = "🔄" };
            refreshBtn.tooltip = "刷新选择列表";
            refreshBtn.AddToClassList("action-btn");
            helperRow.Add(refreshBtn);

            var sortBtn = new Button(SortSpritesByName) { text = "🔤" };
            sortBtn.tooltip = "按名称排序";
            sortBtn.AddToClassList("action-btn");
            helperRow.Add(sortBtn);

            // Content - Sprite List
            _spriteListScrollView = new ScrollView(ScrollViewMode.Vertical);
            _spriteListScrollView.AddToClassList("group-content");
            _spriteListScrollView.style.flexGrow = 1;
            container.Add(_spriteListScrollView);
        }

        private void CreateAnimationSettingsPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("⚙️ Animation设置");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

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

            var browseBtn = new Button(OnBrowseClicked) { text = "📂" };
            browseBtn.AddToClassList("action-btn");
            pathRow.Add(browseBtn);

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

            // Clip Name Row
            var clipNameRow = new VisualElement();
            clipNameRow.AddToClassList("item-row");
            content.Add(clipNameRow);

            var clipNameLabel = new Label("动画名称:");
            clipNameLabel.AddToClassList("toolbar-label");
            clipNameLabel.style.minWidth = 80;
            clipNameRow.Add(clipNameLabel);

            _clipNameField = new TextField();
            _clipNameField.AddToClassList("ue5-text-field");
            _clipNameField.value = _clipName;
            _clipNameField.style.flexGrow = 1;
            _clipNameField.RegisterValueChangedCallback(evt =>
            {
                _clipName = evt.newValue;
                UpdateCreateButtonState();
            });
            clipNameRow.Add(_clipNameField);

            // Frame Rate Row
            var frameRateRow = new VisualElement();
            frameRateRow.AddToClassList("item-row");
            content.Add(frameRateRow);

            var frameRateLabel = new Label("帧率:");
            frameRateLabel.AddToClassList("toolbar-label");
            frameRateLabel.style.minWidth = 80;
            frameRateRow.Add(frameRateLabel);

            _frameRateField = new IntegerField();
            _frameRateField.value = _frameRate;
            _frameRateField.style.flexGrow = 1;
            _frameRateField.style.minWidth = 80;
            _frameRateField.RegisterValueChangedCallback(evt =>
            {
                _frameRate = Mathf.Max(1, evt.newValue);
                _frameRateField.SetValueWithoutNotify(_frameRate);
            });
            frameRateRow.Add(_frameRateField);

            var fpsHint = new Label("FPS");
            fpsHint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            fpsHint.style.marginLeft = 8;
            frameRateRow.Add(fpsHint);

            // Loop Animation Toggle
            var loopRow = new VisualElement();
            loopRow.AddToClassList("item-row");
            content.Add(loopRow);

            var loopLabel = new Label("循环播放:");
            loopLabel.AddToClassList("toolbar-label");
            loopLabel.style.minWidth = 80;
            loopRow.Add(loopLabel);

            _loopAnimationToggle = new Toggle();
            _loopAnimationToggle.value = _loopAnimation;
            _loopAnimationToggle.RegisterValueChangedCallback(evt =>
            {
                _loopAnimation = evt.newValue;
            });
            loopRow.Add(_loopAnimationToggle);
        }

        private void CreateAnimatorSettingsPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("🎮 Animator设置 (可选)");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            // Create Animator Toggle Row
            var toggleRow = new VisualElement();
            toggleRow.AddToClassList("item-row");
            content.Add(toggleRow);

            var toggleLabel = new Label("同时创建Animator:");
            toggleLabel.AddToClassList("toolbar-label");
            toggleLabel.style.minWidth = 120;
            toggleRow.Add(toggleLabel);

            _createAnimatorToggle = new Toggle();
            _createAnimatorToggle.value = _createAnimator;
            _createAnimatorToggle.RegisterValueChangedCallback(evt =>
            {
                _createAnimator = evt.newValue;
                UpdateAnimatorSettingsVisibility();
            });
            toggleRow.Add(_createAnimatorToggle);

            // Animator Settings Container (initially hidden)
            _animatorSettingsContainer = new VisualElement();
            _animatorSettingsContainer.style.marginTop = 8;
            content.Add(_animatorSettingsContainer);

            // Animator Name Row
            var animatorNameRow = new VisualElement();
            animatorNameRow.AddToClassList("item-row");
            _animatorSettingsContainer.Add(animatorNameRow);

            var animatorNameLabel = new Label("Animator名称:");
            animatorNameLabel.AddToClassList("toolbar-label");
            animatorNameLabel.style.minWidth = 120;
            animatorNameRow.Add(animatorNameLabel);

            _animatorNameField = new TextField();
            _animatorNameField.AddToClassList("ue5-text-field");
            _animatorNameField.value = _animatorName;
            _animatorNameField.style.flexGrow = 1;
            _animatorNameField.RegisterValueChangedCallback(evt =>
            {
                _animatorName = evt.newValue;
            });
            animatorNameRow.Add(_animatorNameField);

            // Set As Default Row
            var defaultRow = new VisualElement();
            defaultRow.AddToClassList("item-row");
            _animatorSettingsContainer.Add(defaultRow);

            var defaultLabel = new Label("创建动画为Default:");
            defaultLabel.AddToClassList("toolbar-label");
            defaultLabel.style.minWidth = 120;
            defaultRow.Add(defaultLabel);

            _setAsDefaultToggle = new Toggle();
            _setAsDefaultToggle.value = _setAsDefault;
            _setAsDefaultToggle.RegisterValueChangedCallback(evt =>
            {
                _setAsDefault = evt.newValue;
            });
            defaultRow.Add(_setAsDefaultToggle);

            // Help box
            var helpBox = new VisualElement();
            helpBox.AddToClassList("help-box");
            helpBox.style.marginTop = 8;
            _animatorSettingsContainer.Add(helpBox);

            var helpText = new Label("💡 将创建包含 Default (空状态) 和动画状态的 Animator Controller");
            helpText.AddToClassList("help-box-text");
            helpBox.Add(helpText);

            UpdateAnimatorSettingsVisibility();
        }

        private void CreateActionPanel()
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            rootVisualElement.Add(container);

            // Header
            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("🚀 创建");
            title.AddToClassList("group-title");
            header.Add(title);

            // Content
            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.paddingTop = 12;
            content.style.paddingBottom = 12;
            container.Add(content);

            // Status Label
            _statusLabel = new Label("选择Sprites后点击创建");
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _statusLabel.style.marginBottom = 8;
            _statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            content.Add(_statusLabel);

            // Create Button Row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = 8;
            content.Add(buttonRow);

            _createBtn = new Button(OnCreateClicked) { text = "🎬 创建AnimationClip" };
            _createBtn.AddToClassList("primary-btn");
            _createBtn.style.minWidth = 180;
            _createBtn.style.height = 32;
            _createBtn.style.fontSize = 13;
            buttonRow.Add(_createBtn);
        }

        #endregion

        #region Drag and Drop

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

        #endregion

        #region Event Handlers

        private void RefreshSpriteList()
        {
            _selectedSprites.Clear();

            // Get all selected objects
            var selectedObjects = Selection.objects;

            foreach (var obj in selectedObjects)
            {
                if (obj is Sprite sprite)
                {
                    _selectedSprites.Add(sprite);
                }
                else if (obj is Texture2D texture)
                {
                    // Check if texture is set to Sprite type
                    string path = AssetDatabase.GetAssetPath(texture);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    {
                        // Load all sprites from this texture
                        var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                            .OfType<Sprite>()
                            .ToArray();

                        if (sprites.Length > 0)
                        {
                            _selectedSprites.AddRange(sprites);
                        }
                    }
                }
            }

            UpdateSpriteListUI();
            UpdateCreateButtonState();
        }

        private void UpdateSpriteListUI()
        {
            if (_spriteListScrollView == null) return;

            _spriteListScrollView.Clear();

            if (_selectedSprites.Count == 0)
            {
                var emptyState = new VisualElement();
                emptyState.AddToClassList("empty-state");
                var emptyText = new Label("请在Project中选择Sprites或Sprite类型的Textures");
                emptyText.AddToClassList("empty-state-text");
                emptyText.style.whiteSpace = WhiteSpace.Normal;
                emptyState.Add(emptyText);
                _spriteListScrollView.Add(emptyState);
            }
            else
            {
                for (int i = 0; i < _selectedSprites.Count; i++)
                {
                    var sprite = _selectedSprites[i];
                    var row = CreateSpriteRow(i, sprite);
                    _spriteListScrollView.Add(row);
                }
            }
        }

        private VisualElement CreateSpriteRow(int index, Sprite sprite)
        {
            var row = new VisualElement();
            row.AddToClassList("item-row");

            // Index label
            var indexLabel = new Label($"{index + 1:00}");
            indexLabel.style.width = 24;
            indexLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            indexLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(indexLabel);

            // Sprite preview
            var preview = new VisualElement();
            preview.style.width = 24;
            preview.style.height = 24;
            preview.style.marginRight = 8;
            preview.style.backgroundImage = new StyleBackground(sprite);
            preview.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            row.Add(preview);

            // Name label
            var nameLabel = new Label(sprite.name);
            nameLabel.AddToClassList("item-name");
            row.Add(nameLabel);

            // Locate button
            var locateBtn = new Button(() =>
            {
                EditorGUIUtility.PingObject(sprite);
                Selection.activeObject = sprite;
            })
            { text = "📍" };
            locateBtn.AddToClassList("action-btn");
            locateBtn.tooltip = "在Project中定位";
            row.Add(locateBtn);

            // Remove button
            var removeBtn = new Button(() =>
            {
                _selectedSprites.Remove(sprite);
                UpdateSpriteListUI();
                UpdateCreateButtonState();
            })
            { text = "×" };
            removeBtn.AddToClassList("remove-btn");
            removeBtn.tooltip = "从列表中移除";
            row.Add(removeBtn);

            return row;
        }

        private void SortSpritesByName()
        {
            _selectedSprites = _selectedSprites.OrderBy(s => s.name).ToList();
            UpdateSpriteListUI();
        }

        private void UpdateAnimatorSettingsVisibility()
        {
            if (_animatorSettingsContainer != null)
            {
                _animatorSettingsContainer.style.display = _createAnimator
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void UpdateCreateButtonState()
        {
            if (_createBtn == null) return;

            bool hasSprites = _selectedSprites.Count > 0;
            bool hasClipName = !string.IsNullOrWhiteSpace(_clipName);

            _createBtn.SetEnabled(hasSprites && hasClipName);

            if (!hasSprites)
            {
                _createBtn.text = "⚠️ 请选择Sprites";
            }
            else if (!hasClipName)
            {
                _createBtn.text = "⚠️ 请输入动画名称";
            }
            else
            {
                _createBtn.text = _createAnimator
                    ? "🎬 创建 AnimationClip + Animator"
                    : "🎬 创建 AnimationClip";
            }
        }

        private void OnBrowseClicked()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择保存路径", Application.dataPath, "");
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

        private void OnCreateClicked()
        {
            if (_selectedSprites.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择Sprites！", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(_clipName))
            {
                EditorUtility.DisplayDialog("错误", "请输入动画名称！", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(_savePath))
            {
                EditorUtility.DisplayDialog("错误", "请指定保存路径！", "确定");
                return;
            }

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(_savePath))
            {
                // Try to create the folder
                string fullPath = Path.GetFullPath(_savePath);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    AssetDatabase.Refresh();
                }
            }

            try
            {
                // Create AnimationClip
                var animClip = CreateAnimationClip();

                if (animClip == null)
                {
                    _statusLabel.text = "❌ 创建AnimationClip失败";
                    return;
                }

                string clipPath = $"{_savePath}/{_clipName}.anim";
                clipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);
                AssetDatabase.CreateAsset(animClip, clipPath);

                _statusLabel.text = $"✅ 已创建: {Path.GetFileName(clipPath)}";

                // Optionally create Animator Controller
                if (_createAnimator)
                {
                    var animatorController = CreateAnimatorController(animClip);

                    if (animatorController != null)
                    {
                        string animatorPath = $"{_savePath}/{_animatorName}.controller";
                        animatorPath = AssetDatabase.GenerateUniqueAssetPath(animatorPath);
                        AssetDatabase.CreateAsset(animatorController, animatorPath);

                        _statusLabel.text = $"✅ 已创建: {Path.GetFileName(clipPath)} 和 {Path.GetFileName(animatorPath)}";

                        EditorGUIUtility.PingObject(animatorController);
                    }
                }
                else
                {
                    EditorGUIUtility.PingObject(animClip);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建动画失败: {ex.Message}");
                _statusLabel.text = $"❌ 创建失败: {ex.Message}";
                EditorUtility.DisplayDialog("错误", $"创建失败: {ex.Message}", "确定");
            }
        }

        #endregion

        #region Animation Creation

        private AnimationClip CreateAnimationClip()
        {
            var animClip = new AnimationClip();
            animClip.name = _clipName;
            animClip.frameRate = _frameRate;

            // Create keyframes for each sprite
            var keyframes = new ObjectReferenceKeyframe[_selectedSprites.Count];
            float frameDuration = 1f / _frameRate;

            for (int i = 0; i < _selectedSprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameDuration,
                    value = _selectedSprites[i]
                };
            }

            // Create the curve binding for SpriteRenderer
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

            // Set the curve
            AnimationUtility.SetObjectReferenceCurve(animClip, binding, keyframes);

            // Set loop
            var settings = AnimationUtility.GetAnimationClipSettings(animClip);
            settings.loopTime = _loopAnimation;
            AnimationUtility.SetAnimationClipSettings(animClip, settings);

            return animClip;
        }

        private AnimatorController CreateAnimatorController(AnimationClip animClip)
        {
            var controller = new AnimatorController();
            controller.name = _animatorName;

            // Add a layer
            controller.AddLayer("Base Layer");

            // Get the state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            if (_setAsDefault)
            {
                // Create animation state as default
                var animState = rootStateMachine.AddState(_clipName, new Vector3(250, 100, 0));
                animState.motion = animClip;
                rootStateMachine.defaultState = animState;
            }
            else
            {
                // Create Default empty state with position (Original Logic)
                var defaultState = rootStateMachine.AddState("Default", new Vector3(250, 100, 0));
                defaultState.motion = null; // Empty state

                // Create animation state with position
                var animState = rootStateMachine.AddState(_clipName, new Vector3(250, 200, 0));
                animState.motion = animClip;

                // Set Default as the entry state
                rootStateMachine.defaultState = defaultState;
            }

            return controller;
        }

        #endregion

        #region Preferences

        private void LoadPreferences()
        {
            _savePath = EditorPrefs.GetString(PrefsKeySavePath, "Assets");
            _frameRate = EditorPrefs.GetInt(PrefsKeyFrameRate, 12);
            _clipName = EditorPrefs.GetString(PrefsKeyClipName, "NewAnimation");
            _createAnimator = EditorPrefs.GetBool(PrefsKeyCreateAnimator, false);
            _animatorName = EditorPrefs.GetString(PrefsKeyAnimatorName, "NewAnimator");
            _loopAnimation = EditorPrefs.GetBool(PrefsKeyLoopAnimation, true);
            _setAsDefault = EditorPrefs.GetBool(PrefsKeySetAsDefault, false);
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(PrefsKeySavePath, _savePath);
            EditorPrefs.SetInt(PrefsKeyFrameRate, _frameRate);
            EditorPrefs.SetString(PrefsKeyClipName, _clipName);
            EditorPrefs.SetBool(PrefsKeyCreateAnimator, _createAnimator);
            EditorPrefs.SetString(PrefsKeyAnimatorName, _animatorName);
            EditorPrefs.SetBool(PrefsKeyLoopAnimation, _loopAnimation);
            EditorPrefs.SetBool(PrefsKeySetAsDefault, _setAsDefault);
        }

        #endregion
    }
}
