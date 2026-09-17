using System.Collections.Generic;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    /// <summary>
    /// 多场景组合页面：把"当前同时打开的一组场景（Additive 叠加）"存为命名预设，一键恢复。
    /// 与"场景跳转"（单场景快跳）互补，本页只管多场景组合。
    /// </summary>
    public class SceneSetPresetsRevPage : IKitPageRevV2
    {
        public string Title => "多场景组合";
        private IKitBoxRevV2 _kitBox;

        private List<SceneSetPreset> _presets = new List<SceneSetPreset>();

        // 与 _presets 平行的缺失场景计数（列表展示 "⚠ K 个场景缺失" 用）
        private readonly List<int> _missingCounts = new List<int>();

        // 选中组合的详情数据
        private readonly List<string> _detailPaths = new List<string>();
        private HashSet<string> _detailMissingPaths = new HashSet<string>();

        // UI Elements
        private TextField _nameField;
        private Button _saveBtn;
        private ListView _presetListView;
        private Label _emptyHint;
        private VisualElement _detailBox;
        private Label _detailTitle;
        private ListView _detailListView;
        private Button _applyBtn;
        private Button _updateBtn;
        private Button _deleteBtn;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            ReloadPresets();
            // hierarchyChanged 订阅移至 OnEnter/OnExit：页面在窗口启动时即被 Init，
            // 在此订阅会在未展示期间持续接收回调且切页后无法注销
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            // === 配置区域：保存当前组合 ===
            var configBox = new VisualElement();
            configBox.AddToClassList("config-box");
            root.Add(configBox);

            var configTitle = new Label("保存当前组合");
            configTitle.AddToClassList("section-header");
            configTitle.style.marginTop = 0;
            configBox.Add(configTitle);

            var saveRow = new VisualElement();
            saveRow.AddToClassList("row");
            saveRow.style.alignItems = Align.Center;
            configBox.Add(saveRow);

            _nameField = new TextField("组合名称") { value = "", tooltip = "如：Bootstrap+关卡+UI" };
            _nameField.style.flexGrow = 1;
            saveRow.Add(_nameField);

            _saveBtn = new Button(OnSaveClicked) { text = "保存当前组合" };
            _saveBtn.AddToClassList("btn-success");
            _saveBtn.style.marginLeft = 10;
            saveRow.Add(_saveBtn);

            var captureHint = new Label(
                "把当前同时打开的一组场景（Additive 叠加）保存为命名预设；保持打开顺序，第一个场景即激活场景。");
            captureHint.AddToClassList("text-muted");
            captureHint.style.marginTop = 5;
            configBox.Add(captureHint);

            // === 组合列表标题 ===
            var listHeader = new VisualElement();
            listHeader.AddToClassList("row-space-between");
            listHeader.AddToClassList("mb-5");
            listHeader.style.marginTop = 15;
            root.Add(listHeader);

            var listTitle = new Label("已保存组合");
            listTitle.AddToClassList("text-bold");
            listHeader.Add(listTitle);

            var refreshBtn = new Button(Refresh) { text = "刷新" };
            refreshBtn.AddToClassList("helper-text-btn");
            listHeader.Add(refreshBtn);

            // === 组合列表 ===
            _presetListView = new ListView(new List<SceneSetPreset>(), 30, MakePresetItem, BindPresetItem);
            _presetListView.selectionType = SelectionType.Single;
            _presetListView.AddToClassList("asset-list");
            _presetListView.style.minHeight = 110;
            _presetListView.selectionChanged += _ => OnPresetSelectionChanged();
            root.Add(_presetListView);

            // 空状态提示
            _emptyHint = new Label("尚未保存任何组合。打开一组场景（如 Bootstrap+关卡+UI）后点击「保存当前组合」。");
            _emptyHint.AddToClassList("text-muted");
            _emptyHint.style.marginTop = 5;
            root.Add(_emptyHint);

            // === 详情区：选中组合的场景路径清单 ===
            _detailBox = new VisualElement();
            _detailBox.AddToClassList("config-box");
            _detailBox.style.display = DisplayStyle.None;
            root.Add(_detailBox);

            _detailTitle = new Label("组合详情");
            _detailTitle.AddToClassList("text-bold");
            _detailTitle.style.marginBottom = 5;
            _detailBox.Add(_detailTitle);

            _detailListView = new ListView(new List<string>(), 22, MakeDetailItem, BindDetailItem);
            _detailListView.selectionType = SelectionType.None;
            _detailListView.style.minHeight = 60;
            _detailListView.style.maxHeight = 160;
            _detailBox.Add(_detailListView);

            var detailHint = new Label("点击单条路径可在 Project 窗口中定位对应场景。");
            detailHint.AddToClassList("text-muted");
            detailHint.style.marginTop = 5;
            _detailBox.Add(detailHint);

            // === 底部操作按钮 ===
            var actionRow = new VisualElement();
            actionRow.AddToClassList("row");
            actionRow.AddToClassList("mt-20");
            root.Add(actionRow);

            _applyBtn = new Button(OnApplyClicked) { text = "恢复打开" };
            _applyBtn.AddToClassList("btn-success");
            _applyBtn.style.marginRight = 8;
            actionRow.Add(_applyBtn);

            _updateBtn = new Button(OnUpdateClicked) { text = "更新为当前" };
            _updateBtn.AddToClassList("btn-secondary");
            _updateBtn.style.marginRight = 8;
            actionRow.Add(_updateBtn);

            _deleteBtn = new Button(OnDeleteClicked) { text = "删除" };
            _deleteBtn.AddToClassList("btn-danger");
            actionRow.Add(_deleteBtn);

            // 初始状态
            ReloadPresets();
            UpdateSaveButtonState();

            return root;
        }

        // ───────────────────── 列表项构建与绑定 ─────────────────────

        private VisualElement MakePresetItem()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");
            row.style.alignItems = Align.Center;

            var nameLabel = new Label();
            nameLabel.name = "preset-name";
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            var countLabel = new Label();
            countLabel.name = "scene-count";
            countLabel.AddToClassList("text-muted");
            countLabel.style.marginRight = 10;
            row.Add(countLabel);

            var missingLabel = new Label();
            missingLabel.name = "missing-tag";
            missingLabel.AddToClassList("text-bad");
            row.Add(missingLabel);

            return row;
        }

        private void BindPresetItem(VisualElement element, int index)
        {
            var presets = _presetListView.itemsSource as List<SceneSetPreset>;
            if (presets == null || index < 0 || index >= presets.Count) return;
            var preset = presets[index];
            int missing = index < _missingCounts.Count ? _missingCounts[index] : 0;
            int total = preset.ScenePaths != null ? preset.ScenePaths.Length : 0;

            var nameLabel = element.Q<Label>("preset-name");
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(preset.Name) ? "(未命名)" : preset.Name;
                nameLabel.tooltip = preset.Name;
            }

            var countLabel = element.Q<Label>("scene-count");
            if (countLabel != null) countLabel.text = $"{total} 个场景";

            var missingLabel = element.Q<Label>("missing-tag");
            if (missingLabel != null)
            {
                missingLabel.text = $"⚠ {missing} 个场景缺失";
                missingLabel.style.display = missing > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private VisualElement MakeDetailItem()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");
            row.style.alignItems = Align.Center;

            var pathLabel = new Label();
            pathLabel.name = "scene-path";
            pathLabel.style.flexGrow = 1;
            row.Add(pathLabel);

            // 点击单条路径 → Ping 对应 SceneAsset（路径在绑定时写入 userData）
            row.RegisterCallback<ClickEvent>(_ =>
            {
                var path = row.userData as string;
                if (!string.IsNullOrEmpty(path))
                    PingSceneAsset(path);
            });

            return row;
        }

        private void BindDetailItem(VisualElement element, int index)
        {
            var paths = _detailListView.itemsSource as List<string>;
            if (paths == null || index < 0 || index >= paths.Count) return;
            var path = paths[index];

            element.userData = path;

            var pathLabel = element.Q<Label>("scene-path");
            if (pathLabel != null)
            {
                pathLabel.text = path;
                pathLabel.tooltip = "点击在 Project 窗口中定位";
                // 缺失路径标红（复用既有 text-bad 类）
                pathLabel.EnableInClassList("text-bad", _detailMissingPaths.Contains(path));
            }
        }

        // ───────────────────── 状态刷新 ─────────────────────

        /// <summary>重新读盘并刷新列表/详情/按钮状态（窗口 Refresh 与各操作后都会走这里）。</summary>
        private void ReloadPresets()
        {
            _presets = SceneSetPresetStore.LoadPresets();

            _missingCounts.Clear();
            foreach (var preset in _presets)
                _missingCounts.Add(SceneSetPresetStore.ValidatePreset(preset).Count);

            if (_presetListView != null)
            {
                _presetListView.itemsSource = _presets;
                _presetListView.RefreshItems();
            }

            if (_emptyHint != null)
                _emptyHint.style.display = _presets.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateDetail();
            UpdateActionButtons();
        }

        private void UpdateSaveButtonState()
        {
            if (_saveBtn == null) return;
            // 当前没有任何打开场景时禁用保存
            _saveBtn.SetEnabled(SceneManager.loadedSceneCount > 0);
        }

        private void UpdateActionButtons()
        {
            if (_applyBtn == null) return;
            bool hasSelection = GetSelectedPreset() != null;
            _applyBtn.SetEnabled(hasSelection);
            _updateBtn.SetEnabled(hasSelection);
            _deleteBtn.SetEnabled(hasSelection);
        }

        private void OnPresetSelectionChanged()
        {
            UpdateDetail();
            UpdateActionButtons();
        }

        private void UpdateDetail()
        {
            if (_detailBox == null) return;

            var preset = GetSelectedPreset();
            _detailBox.style.display = preset == null ? DisplayStyle.None : DisplayStyle.Flex;

            _detailPaths.Clear();
            _detailMissingPaths = new HashSet<string>();

            if (preset != null)
            {
                _detailTitle.text = $"组合详情：{preset.Name}";
                _detailMissingPaths = new HashSet<string>(SceneSetPresetStore.ValidatePreset(preset));
                if (preset.ScenePaths != null)
                    _detailPaths.AddRange(preset.ScenePaths);
            }

            if (_detailListView != null)
            {
                _detailListView.itemsSource = _detailPaths;
                _detailListView.RefreshItems();
            }
        }

        private SceneSetPreset GetSelectedPreset()
        {
            return _presetListView != null ? _presetListView.selectedItem as SceneSetPreset : null;
        }

        /// <summary>在刷新后的列表里尽量恢复对某组合的选中（先按引用找，退化为按名称找）。</summary>
        private void RestoreSelection(SceneSetPreset preset)
        {
            if (preset == null || _presetListView == null) return;

            int index = _presets.FindIndex(p => ReferenceEquals(p, preset));
            if (index < 0)
                index = _presets.FindIndex(p => p.Name == preset.Name);

            _presetListView.selectedIndex = index; // -1 即清空选中
        }

        // ───────────────────── 操作 ─────────────────────

        private void OnSaveClicked()
        {
            string name = _nameField.value != null ? _nameField.value.Trim() : "";
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("提示", "请先填写组合名称！", "确定");
                return;
            }

            var capture = SceneSetPresetStore.CaptureCurrentSetup();
            if (capture.ScenePaths == null || capture.ScenePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前没有已保存过的打开场景可保存（未保存过的临时场景无法入组）。", "确定");
                return;
            }

            // 同名即覆盖（与「更新为当前」的底层语义一致：SavePresets 覆盖同名 Name）
            int existing = _presets.FindIndex(p => p.Name == name);
            if (existing >= 0 && !EditorUtility.DisplayDialog("覆盖确认", $"已存在同名组合「{name}」，是否覆盖？", "覆盖", "取消"))
                return;

            capture.Name = name;
            if (existing >= 0)
                _presets[existing] = capture;
            else
                _presets.Add(capture);

            SceneSetPresetStore.SavePresets(_presets);
            ReloadPresets();
            RestoreSelection(capture);
        }

        private void OnApplyClicked()
        {
            var preset = GetSelectedPreset();
            if (preset == null) return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("提示", "Play 模式下不能恢复场景组合，请先退出 Play。", "确定");
                return;
            }

            // 有缺失场景：提醒后跳过缺失项继续
            var missing = SceneSetPresetStore.ValidatePreset(preset);
            if (missing.Count > 0 && !EditorUtility.DisplayDialog(
                    "缺失场景",
                    $"组合「{preset.Name}」有 {missing.Count} 个场景已不存在，恢复时将被跳过：\n{string.Join("\n", missing)}",
                    "继续", "取消"))
                return;

            // 有未保存修改：明确警告后确认（RestoreSceneManagerSetup 不会自动保存，未存修改将丢失）
            var dirty = SceneSetPresetStore.GetDirtyOpenSceneNames();
            if (dirty.Count > 0 && !EditorUtility.DisplayDialog(
                    "未保存修改",
                    $"以下场景有未保存修改，恢复组合后将丢失：\n{string.Join("\n", dirty)}\n\n确定继续恢复？",
                    "恢复", "取消"))
                return;

            if (SceneSetPresetStore.ApplyPreset(preset))
            {
                Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("恢复失败", "组合中没有可用的场景，恢复中止。", "确定");
            }
        }

        private void OnUpdateClicked()
        {
            var preset = GetSelectedPreset();
            if (preset == null) return;

            var capture = SceneSetPresetStore.CaptureCurrentSetup();
            if (capture.ScenePaths == null || capture.ScenePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前没有已保存过的打开场景，无法更新组合。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "更新确认",
                    $"把组合「{preset.Name}」覆盖为当前打开的 {capture.ScenePaths.Length} 个场景？",
                    "更新", "取消"))
                return;

            preset.ScenePaths = capture.ScenePaths;
            SceneSetPresetStore.SavePresets(_presets);
            ReloadPresets();
            RestoreSelection(preset);
        }

        private void OnDeleteClicked()
        {
            var preset = GetSelectedPreset();
            if (preset == null) return;

            if (!EditorUtility.DisplayDialog("删除确认", $"确定删除组合「{preset.Name}」？", "删除", "取消"))
                return;

            _presets.Remove(preset);
            SceneSetPresetStore.SavePresets(_presets);
            ReloadPresets();
        }

        private void PingSceneAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"场景已不存在：{path}", "确定");
            }
        }

        // ───────────────────── IKitPageRevV2 ─────────────────────

        public void Refresh()
        {
            ReloadPresets();
            UpdateSaveButtonState();
        }

        public void OnEnter()
        {
            // 进入页面：订阅层级变化以同步"保存当前组合"按钮可用态（先防御性注销，防止重复叠加）
            EditorApplication.hierarchyChanged -= UpdateSaveButtonState;
            EditorApplication.hierarchyChanged += UpdateSaveButtonState;
            UpdateSaveButtonState();
        }

        public void OnExit()
        {
            // 离开页面：注销全局层级订阅
            EditorApplication.hierarchyChanged -= UpdateSaveButtonState;
        }

        public void DrawIMGUI() { }
    }
}
