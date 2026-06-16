using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup
{
    /// <summary>
    /// 批量重命名资产页面
    /// </summary>
    public class BatchRenameRevPage : IKitPageRevV2
    {
        private enum NumberingMode
        {
            None,
            Prefix,
            Suffix
        }

        public string Title => "批量重命名";
        private IKitBoxRevV2 _kitBox;

        private List<AssetInfo> _selectedAssets = new List<AssetInfo>();
        private ListView _previewListView;

        // 重命名选项 (第一列)
        private TextField _renameField;
        private TextField _prefixField;
        private TextField _suffixField;

        // 编号选项 (第二列)
        private NumberingMode _numberingMode = NumberingMode.None;
        private VisualElement _modeToggleGroup;
        private IntegerField _startNumberField;
        private IntegerField _digitCountField;

        private Button _activeApplyBtn;

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            RefreshSelectedAssets();
            Selection.selectionChanged += RefreshSelectedAssets;
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            // === 标题 ===
            var header = new Label("批量重命名资产");
            header.AddToClassList("page-header");
            header.AddToClassList("mb-15");
            root.Add(header);

            // === 配置区域 ===
            var configBox = new VisualElement();
            configBox.AddToClassList("config-box");
            root.Add(configBox);

            var configTitle = new Label("命名规则");
            configTitle.AddToClassList("section-header");
            configTitle.style.marginTop = 0;
            configBox.Add(configTitle);

            // 使用 FlexDirection.Row 来放置两列
            var columnsContainer = new VisualElement();
            columnsContainer.style.flexDirection = FlexDirection.Row;
            configBox.Add(columnsContainer);

            // --- 第一列: 名称相关的 (前缀, 重命名, 后缀) ---
            var col1 = new VisualElement();
            col1.style.flexGrow = 1;
            col1.style.paddingRight = 10;
            columnsContainer.Add(col1);

            _prefixField = new TextField("前缀") { value = "" };
            _prefixField.RegisterValueChangedCallback(_ => UpdatePreview());
            col1.Add(_prefixField);

            _renameField = new TextField("重命名") { value = "" };
            _renameField.tooltip = "若为空则保留原名，否则覆盖原名";
            _renameField.RegisterValueChangedCallback(_ => UpdatePreview());
            col1.Add(_renameField);

            _suffixField = new TextField("后缀") { value = "" };
            _suffixField.RegisterValueChangedCallback(_ => UpdatePreview());
            col1.Add(_suffixField);

            // --- 第二列: 编号相关的 (启用编号模式, 起始, 位数) ---
            var col2 = new VisualElement();
            col2.style.flexGrow = 1;
            col2.style.paddingLeft = 10;
            col2.style.borderLeftWidth = 1;
            col2.style.borderLeftColor = new Color(0.15f, 0.15f, 0.15f);
            columnsContainer.Add(col2);

            // 对齐标签宽度
            float labelWidth = 60f;

            // 编号模式切换按钮
            var modeRow = new VisualElement();
            modeRow.style.flexDirection = FlexDirection.Row;
            modeRow.style.alignItems = Align.Center;
            modeRow.style.marginBottom = 2; //稍微紧凑一点，以匹配字段间距
            col2.Add(modeRow);

            var modeLabel = new Label("编号位置");
            modeLabel.style.width = labelWidth;
            modeLabel.style.marginLeft = 3; // 微调以匹配 Unity 字段标签缩进
            modeLabel.style.paddingTop = 2; // 垂直居中
            modeRow.Add(modeLabel);

            // 创建简单的自定义切换组
            _modeToggleGroup = new VisualElement();
            _modeToggleGroup.style.flexDirection = FlexDirection.Row;
            modeRow.Add(_modeToggleGroup);

            CreateModeToggle("前缀", NumberingMode.Prefix);
            CreateModeToggle("后缀", NumberingMode.Suffix);
            CreateModeToggle("无", NumberingMode.None);

            _startNumberField = new IntegerField("起始") { value = 1 };
            _startNumberField.RegisterValueChangedCallback(_ => UpdatePreview());
            // 强制标签宽度以对齐
            _startNumberField.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var l = _startNumberField.Q<Label>();
                if (l != null) l.style.minWidth = labelWidth;
            });
            col2.Add(_startNumberField);

            _digitCountField = new IntegerField("位数") { value = 2 };
            _digitCountField.RegisterValueChangedCallback(_ => UpdatePreview());
            // 强制标签宽度以对齐
            _digitCountField.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var l = _digitCountField.Q<Label>();
                if (l != null) l.style.minWidth = labelWidth;
            });
            col2.Add(_digitCountField);

            // === 列表标题 ===
            var listHeader = new VisualElement();
            listHeader.AddToClassList("row-space-between");
            listHeader.AddToClassList("mb-5");
            listHeader.style.marginTop = 15;
            root.Add(listHeader);

            var listTitle = new Label("📦 预览与确认");
            listTitle.AddToClassList("text-bold");
            listHeader.Add(listTitle);

            var refreshBtn = new Button(RefreshSelectedAssets) { text = "🔄 刷新选中" };
            refreshBtn.AddToClassList("helper-text-btn");
            listHeader.Add(refreshBtn);

            // === 预览列表 ===
            _previewListView = new ListView(new List<RenamePreview>(), 30, MakePreviewItem, BindPreviewItem);
            _previewListView.selectionType = SelectionType.None;
            _previewListView.AddToClassList("asset-list");
            _previewListView.style.flexGrow = 1;
            root.Add(_previewListView);

            // === 底部操作按钮 ===
            _activeApplyBtn = new Button(ApplyRename) { text = "✓ 应用重命名" };
            _activeApplyBtn.AddToClassList("btn-success");
            _activeApplyBtn.AddToClassList("mt-20");
            root.Add(_activeApplyBtn);

            UpdatePreview();
            UpdateModeTogglesVisual();

            return root;
        }

        private void CreateModeToggle(string label, NumberingMode mode)
        {
            var btn = new Button(() =>
            {
                _numberingMode = mode;
                UpdateModeTogglesVisual();
                UpdatePreview();
            });
            btn.text = label;
            btn.userData = mode;
            btn.AddToClassList("mode-toggle");

            // 基础样式
            btn.style.marginRight = 0;
            btn.style.marginLeft = 0;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.paddingTop = 2;
            btn.style.paddingBottom = 2;

            // 按钮间距
            btn.style.marginRight = 2;

            _modeToggleGroup.Add(btn);
        }

        private void UpdateModeTogglesVisual()
        {
            if (_modeToggleGroup == null) return;
            foreach (var element in _modeToggleGroup.Children())
            {
                if (element is Button btn && btn.userData is NumberingMode mode)
                {
                    if (mode == _numberingMode)
                    {
                        // 选中时显示橙色
                        btn.style.backgroundColor = new Color(1f, 0.5f, 0f);
                        btn.style.color = Color.white;
                    }
                    else
                    {
                        btn.style.backgroundColor = new StyleColor(StyleKeyword.Null);
                        btn.style.color = new StyleColor(StyleKeyword.Null);
                    }
                }
            }
        }

        private VisualElement MakePreviewItem()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");
            row.style.alignItems = Align.Center;

            var icon = new Label();
            icon.name = "icon";
            icon.style.width = 20;
            icon.style.marginRight = 5;
            icon.style.fontSize = 14;
            row.Add(icon);

            var oldNameLabel = new Label();
            oldNameLabel.name = "old-name";
            oldNameLabel.style.width = 200;
            oldNameLabel.style.flexGrow = 1;
            oldNameLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            row.Add(oldNameLabel);

            var arrow = new Label("➜");
            arrow.style.marginLeft = 5;
            arrow.style.marginRight = 5;
            arrow.style.color = new Color(1f, 1f, 1f, 0.5f);
            row.Add(arrow);

            var newNameLabel = new Label();
            newNameLabel.name = "new-name";
            newNameLabel.style.flexGrow = 1;
            newNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(newNameLabel);

            return row;
        }

        private void BindPreviewItem(VisualElement element, int index)
        {
            var previews = _previewListView.itemsSource as List<RenamePreview>;
            if (previews == null || index < 0 || index >= previews.Count) return;
            var preview = previews[index];

            var icon = element.Q<Label>("icon");
            var oldNameLabel = element.Q<Label>("old-name");
            var newNameLabel = element.Q<Label>("new-name");

            if (icon != null) icon.text = GetAssetIcon(preview.Type);
            if (oldNameLabel != null)
            {
                oldNameLabel.text = preview.OldName;
                oldNameLabel.tooltip = preview.OldName;
            }

            if (newNameLabel != null)
            {
                newNameLabel.text = preview.NewName;

                if (preview.OldName == preview.NewName)
                {
                    newNameLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                    newNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
                else
                {
                    newNameLabel.style.color = new Color(0.4f, 1f, 0.4f);
                    newNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
            }
        }

        private void RefreshSelectedAssets()
        {
            _selectedAssets.Clear();

            var selected = Selection.objects;
            foreach (var obj in selected)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;

                _selectedAssets.Add(new AssetInfo
                {
                    Asset = obj,
                    Name = obj.name,
                    Path = path,
                    Type = obj.GetType().Name
                });
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_previewListView == null) return;

            var previews = new List<RenamePreview>();

            for (int i = 0; i < _selectedAssets.Count; i++)
            {
                var asset = _selectedAssets[i];
                string newName = GenerateNewName(asset.Name, i);

                previews.Add(new RenamePreview
                {
                    OldName = asset.Name,
                    NewName = newName,
                    Type = asset.Type
                });
            }

            _previewListView.itemsSource = previews;
            _previewListView.RefreshItems();

            if (_activeApplyBtn != null)
            {
                _activeApplyBtn.text = $"✓ 应用重命名 ({previews.Count})";
                _activeApplyBtn.SetEnabled(previews.Count > 0);
            }
        }

        private string GenerateNewName(string originalName, int index)
        {
            string baseName = originalName;
            if (_renameField != null && !string.IsNullOrEmpty(_renameField.value))
            {
                baseName = _renameField.value;
            }

            string result = baseName;
            string prefix = _prefixField?.value ?? "";
            string suffix = _suffixField?.value ?? "";

            int startNumber = _startNumberField?.value ?? 1;
            int digitCount = Mathf.Max(1, _digitCountField?.value ?? 2);

            string numberStr = "";
            bool useNumbering = _numberingMode != NumberingMode.None;

            if (useNumbering)
            {
                int number = startNumber + index;
                numberStr = number.ToString($"D{digitCount}");
            }

            // 构建新名称
            if (_numberingMode == NumberingMode.Prefix)
            {
                result = numberStr + prefix + result + suffix;
            }
            else if (_numberingMode == NumberingMode.Suffix)
            {
                result = prefix + result + suffix + numberStr;
            }
            else
            {
                result = prefix + result + suffix;
            }

            return result;
        }

        private void ApplyRename()
        {
            if (_selectedAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中任何资产！", "确定");
                return;
            }

            var previews = _previewListView.itemsSource as List<RenamePreview>;
            if (previews == null || previews.Count == 0) return;

            // 检查是否有重名
            var newNames = previews.Select(p => p.NewName).ToList();
            var duplicates = newNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                EditorUtility.DisplayDialog("错误", $"重命名后存在重名资产：\n{string.Join(", ", duplicates)}\n\n请调整命名规则！", "确定");
                return;
            }

            // 确认对话框
            string message = $"确定要重命名 {_selectedAssets.Count} 个资产吗？\n\n此操作无法撤销！";
            if (!EditorUtility.DisplayDialog("确认重命名", message, "确定", "取消"))
            {
                return;
            }

            // 执行重命名
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < _selectedAssets.Count; i++)
            {
                var asset = _selectedAssets[i];
                var preview = previews[i];

                if (preview.OldName == preview.NewName)
                {
                    continue; // 跳过没有变化的
                }

                string assetPath = AssetDatabase.GetAssetPath(asset.Asset);
                string error = AssetDatabase.RenameAsset(assetPath, preview.NewName);

                if (string.IsNullOrEmpty(error))
                {
                    successCount++;
                }
                else
                {
                    Debug.LogError($"重命名失败: {asset.Name} -> {preview.NewName}\n错误: {error}");
                    failCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 显示结果
            string resultMessage = $"重命名完成！\n\n成功: {successCount}\n失败: {failCount}";
            EditorUtility.DisplayDialog("重命名结果", resultMessage, "确定");

            // 刷新
            RefreshSelectedAssets();
        }

        private string GetAssetIcon(string typeName)
        {
            switch (typeName)
            {
                case "GameObject": return "🎮";
                case "SceneAsset": return "🎬";
                case "Material": return "🎨";
                case "Texture2D": return "🖼️";
                case "Sprite": return "🖼️";
                case "AudioClip": return "🔊";
                case "AnimationClip": return "🎞️";
                case "MonoScript": return "📄";
                case "Shader": return "✨";
                case "Font": return "🔤";
                default: return "📦";
            }
        }

        private class AssetInfo
        {
            public Object Asset;
            public string Name;
            public string Path;
            public string Type;
        }

        private class RenamePreview
        {
            public string OldName;
            public string NewName;
            public string Type;
        }

        public void Refresh()
        {
            RefreshSelectedAssets();
        }

        public void DrawIMGUI() { }
    }
}
