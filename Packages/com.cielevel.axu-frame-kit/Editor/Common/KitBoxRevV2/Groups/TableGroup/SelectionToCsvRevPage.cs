using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.TableGroup
{
    public class SelectionToCsvRevPage : IKitPageRevV2
    {
        public string Title => "资产信息导出CSV";
        private IKitBoxRevV2 _kitBox;

        private TextField _pathField;
        private TextField _nameField;
        private ListView _assetListView;
        private Label _countLabel;
        private Button _exportButton;
        private Button _exportToSelectedFolderBtn;

        // Header Options
        private Toggle _includeEnglishKeysToggle;
        private Toggle _includeTypesToggle;

        private List<string> _currentGuids = new List<string>();

        // Column Flags
        private enum ColumnType
        {
            Guid,
            AssetName,
            AssetPath,
            ResourcesPath,
            AssetType,
            Extension,
            FileSize,
            Labels
        }

        private readonly Dictionary<ColumnType, bool> _columnStates = new Dictionary<ColumnType, bool>();
        private readonly Dictionary<ColumnType, string> _columnNames = new Dictionary<ColumnType, string>()
        {
            { ColumnType.Guid, "GUID" },
            { ColumnType.AssetName, "资产名称" },
            { ColumnType.AssetPath, "资产路径" },
            { ColumnType.ResourcesPath, "Resources加载路径" },
            { ColumnType.AssetType, "资产类型" },
            { ColumnType.Extension, "文件后缀" },
            { ColumnType.FileSize, "文件大小(KB)" },
            { ColumnType.Labels, "资产标签" }
        };

        public void Init(IKitBoxRevV2 kitBox)
        {
            _kitBox = kitBox;
            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                _columnStates[type] = true; // Default all selected
            }
        }

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            // --- Header ---
            var header = new Label("选中资产信息导出为 CSV");
            header.AddToClassList("page-header");
            header.AddToClassList("mb-15");
            root.Add(header);

            // --- Path Settings ---
            var configBox = new VisualElement();
            configBox.AddToClassList("config-box");
            root.Add(configBox);

            // Path input row
            _pathField = new TextField("保存路径") { value = Application.dataPath };
            _pathField.AddToClassList("mb-10");
            configBox.Add(_pathField);

            // Buttons row - right aligned below path field
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.FlexEnd;
            buttonRow.AddToClassList("mb-10");
            configBox.Add(buttonRow);

            var browseBtn = new Button(BrowseFolder) { text = "浏览..." };
            browseBtn.AddToClassList("browse-button");
            buttonRow.Add(browseBtn);

            _exportToSelectedFolderBtn = new Button(SelectCurrentFolder) { text = "选择当前文件夹" };
            _exportToSelectedFolderBtn.AddToClassList("browse-button");
            _exportToSelectedFolderBtn.style.width = 140; // 2x width of browse button (70px)
            _exportToSelectedFolderBtn.style.marginLeft = 8;
            _exportToSelectedFolderBtn.SetEnabled(false); // Default disabled
            _exportToSelectedFolderBtn.tooltip = "在 Project 窗口中选中一个文件夹后可用";
            buttonRow.Add(_exportToSelectedFolderBtn);

            _nameField = new TextField("文件名") { value = "CsvAsset.csv" };
            configBox.Add(_nameField);

            // --- Column Selection Area ---
            var columnBox = new VisualElement();
            columnBox.AddToClassList("column-selection-container");
            root.Add(columnBox);

            var colHeader = new Label("选择导出列:");
            colHeader.AddToClassList("section-header");
            colHeader.style.marginTop = 0; // Override default top margin for inside container
            colHeader.style.marginBottom = 10;
            columnBox.Add(colHeader);

            var colRow = new VisualElement();
            colRow.style.flexDirection = FlexDirection.Row;
            colRow.style.flexWrap = Wrap.Wrap;
            columnBox.Add(colRow);

            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                var t = type;
                var btn = new Button(() => ToggleColumn(t));
                btn.text = GetButtonText(t);
                btn.name = $"col-btn-{t}";
                btn.AddToClassList("column-toggle-btn");

                UpdateButtonVisual(btn, _columnStates[t]);
                colRow.Add(btn);
            }

            // --- Helper Buttons ---
            var toolsRow = new VisualElement();
            toolsRow.AddToClassList("helper-btn-row");

            var selectAllBtn = new Button(() => SetAllColumns(true)) { text = "全选" };
            selectAllBtn.AddToClassList("helper-text-btn");
            toolsRow.Add(selectAllBtn);

            var selectNoneBtn = new Button(() => SetAllColumns(false)) { text = "全不选" };
            selectNoneBtn.AddToClassList("helper-text-btn");
            toolsRow.Add(selectNoneBtn);

            columnBox.Add(toolsRow);

            // --- Header Options ---
            var optionsBox = new VisualElement();
            optionsBox.AddToClassList("options-row");

            _includeEnglishKeysToggle = new Toggle("导出Key行 (Row 2)");
            _includeEnglishKeysToggle.value = true;
            _includeEnglishKeysToggle.AddToClassList("option-toggle");
            optionsBox.Add(_includeEnglishKeysToggle);

            _includeTypesToggle = new Toggle("导出类型行 (Row 3)");
            _includeTypesToggle.value = true;
            _includeTypesToggle.AddToClassList("option-toggle");
            optionsBox.Add(_includeTypesToggle);

            root.Add(optionsBox);

            // --- Selection List ---
            var listHeader = new VisualElement();
            listHeader.AddToClassList("row-space-between");
            listHeader.AddToClassList("mb-5");
            root.Add(listHeader);

            var listTitle = new Label("📦 当前选中的资产");
            listTitle.AddToClassList("text-bold");
            listHeader.Add(listTitle);

            _countLabel = new Label("0 个资产");
            _countLabel.AddToClassList("text-secondary");
            listHeader.Add(_countLabel);

            _assetListView = new ListView(_currentGuids, 24, MakeItem, BindItem);
            _assetListView.AddToClassList("asset-list");
            root.Add(_assetListView);

            // --- Export Action ---
            _exportButton = new Button(ExportSelectedAssets);
            _exportButton.text = "📤 导出非文件夹资产到 CSV";
            _exportButton.AddToClassList("btn-success");
            _exportButton.AddToClassList("mt-20");
            root.Add(_exportButton);

            // Monitor selection
            Selection.selectionChanged += UpdateSelection;
            UpdateSelection();

            return root;
        }

        private void ToggleColumn(ColumnType type)
        {
            _columnStates[type] = !_columnStates[type];
            RefreshColumnButtons();
        }

        private void SetAllColumns(bool state)
        {
            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                _columnStates[type] = state;
            }
            RefreshColumnButtons();
        }

        private void RefreshColumnButtons()
        {
            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                // Note: Searching inside the container which is a few levels deep or stored ref
                // For simplicity, we can just search from root if we stored it, or assume parent structure
                // But simpler is to rebuild or find via name.
                // Or better: Just re-query.

                // Since we don't hold a reference to 'root', let's use the export button's hierarchy helper
                var btn = _exportButton.parent.Q<Button>($"col-btn-{type}");
                if (btn != null)
                {
                    btn.text = GetButtonText(type);
                    UpdateButtonVisual(btn, _columnStates[type]);
                }
            }
        }

        private string GetButtonText(ColumnType type)
        {
            // Simplified text since visual state is now color/icon
            return _columnNames[type];
        }

        private void UpdateButtonVisual(Button btn, bool isActive)
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

        private VisualElement MakeItem()
        {
            var row = new VisualElement();
            row.AddToClassList("list-item-row");

            var icon = new Image();
            icon.name = "asset-icon";
            icon.AddToClassList("asset-icon");
            row.Add(icon);

            var name = new Label();
            name.name = "asset-name";
            name.AddToClassList("flex-grow");
            row.Add(name);

            var type = new Label();
            type.name = "asset-type";
            type.AddToClassList("asset-type-label");
            row.Add(type);

            return row;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _currentGuids.Count) return;
            var guid = _currentGuids[index];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);

            element.Q<Image>("asset-icon").image = AssetDatabase.GetCachedIcon(path);
            element.Q<Label>("asset-name").text = obj != null ? obj.name : "(Missing)";
            element.Q<Label>("asset-type").text = obj != null ? obj.GetType().Name : "Unknown";
        }

        private void UpdateSelection()
        {
            _currentGuids.Clear();
            _currentGuids.AddRange(Selection.assetGUIDs);
            _countLabel.text = $"{_currentGuids.Count} 个资产";
            _assetListView.RefreshItems();
            _exportButton.SetEnabled(_currentGuids.Count > 0);

            // Check if exactly one folder is selected for "Export to Selected Folder" button
            UpdateExportToSelectedFolderButton();

            // Re-check export button class for disabled state (handled by built-in Unity usually, but we have custom class)
            if (_currentGuids.Count > 0)
            {
                _exportButton.RemoveFromClassList("disabled"); // if we had one
            }
            else
            {
                // _exportButton.AddToClassList("disabled");
            }
        }

        /// <summary>
        /// Check if exactly one folder is selected in Editor, and enable/disable the button accordingly
        /// </summary>
        private void UpdateExportToSelectedFolderButton()
        {
            bool canSelect = false;
            string selectedFolderPath = null;

            // Check all selected objects in Project window
            var selectedObjects = Selection.objects;

            if (selectedObjects != null && selectedObjects.Length == 1)
            {
                var obj = selectedObjects[0];
                if (obj != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath))
                    {
                        canSelect = true;
                        selectedFolderPath = assetPath;
                    }
                }
            }

            _exportToSelectedFolderBtn.SetEnabled(canSelect);

            if (canSelect)
            {
                _exportToSelectedFolderBtn.tooltip = $"设置保存路径为: {selectedFolderPath}";
                _exportToSelectedFolderBtn.text = "📂 选择当前文件夹";
            }
            else
            {
                _exportToSelectedFolderBtn.tooltip = "在 Project 窗口中选中一个文件夹后可用";
                _exportToSelectedFolderBtn.text = "选择当前文件夹";
            }
        }

        /// <summary>
        /// Get the currently selected folder path (only if exactly one folder is selected)
        /// </summary>
        private string GetSelectedFolderPath()
        {
            var selectedObjects = Selection.objects;

            if (selectedObjects != null && selectedObjects.Length == 1)
            {
                var obj = selectedObjects[0];
                if (obj != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath))
                    {
                        // Convert Assets/... path to full system path
                        return Path.GetFullPath(assetPath);
                    }
                }
            }
            return null;
        }

        private void BrowseFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("选择保存路径", _pathField.value, "");
            if (!string.IsNullOrEmpty(selected))
            {
                _pathField.value = selected;
            }
        }

        /// <summary>
        /// Select the currently selected folder as export path (does not trigger export)
        /// </summary>
        private void SelectCurrentFolder()
        {
            string folderPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog("提示", "请在 Project 窗口中选中一个文件夹！", "确定");
                return;
            }

            // Only set the path field, do NOT trigger export
            _pathField.value = folderPath;
        }

        private void ExportSelectedAssets()
        {
            var selectedAssets = Selection.objects.Where(obj => obj != null).ToList();
            if (selectedAssets.Count == 0) return;

            foreach (var asset in selectedAssets)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset)))
                {
                    EditorUtility.DisplayDialog("提示", "请不要选择文件夹！", "确定");
                    return;
                }
            }

            if (_columnStates.Values.All(v => !v))
            {
                EditorUtility.DisplayDialog("提示", "请至少选择一列导出！", "确定");
                return;
            }

            string suffix = Path.GetExtension(_nameField.value).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "" : ".csv";
            string path = (_pathField.value + "/" + _nameField.value + suffix).MakeSureFilePath();

            if (string.IsNullOrEmpty(path)) return;

            // Build CSV Content
            var builder = new StringBuilder();

            // 1. Title Row
            var headers = new List<string>();
            var typeKeys = new List<string>();
            var typeValues = new List<string>();

            var activeTypes = Enum.GetValues(typeof(ColumnType)).Cast<ColumnType>().Where(t => _columnStates[t]).ToList();

            foreach (var type in activeTypes)
            {
                headers.Add(_columnNames[type]);
                switch (type)
                {
                    case ColumnType.Guid:
                        typeKeys.Add("assetGuid");
                        typeValues.Add("string");
                        break;
                    case ColumnType.AssetName:
                        typeKeys.Add("assetName");
                        typeValues.Add("string");
                        break;
                    case ColumnType.AssetPath:
                        typeKeys.Add("assetPath");
                        typeValues.Add("string");
                        break;
                    case ColumnType.ResourcesPath:
                        typeKeys.Add("assetLoadPath");
                        typeValues.Add("string");
                        break;
                    case ColumnType.AssetType:
                        typeKeys.Add("assetType");
                        typeValues.Add("string");
                        break;
                    case ColumnType.Extension:
                        typeKeys.Add("extension");
                        typeValues.Add("string");
                        break;
                    case ColumnType.FileSize:
                        typeKeys.Add("fileSizeKb");
                        typeValues.Add("float");
                        break;
                    case ColumnType.Labels:
                        typeKeys.Add("labels");
                        typeValues.Add("string");
                        break;
                }
            }

            builder.AppendLine(string.Join(",", headers));

            if (_includeEnglishKeysToggle.value)
            {
                builder.AppendLine(string.Join(",", typeKeys));
            }

            if (_includeTypesToggle.value)
            {
                builder.AppendLine(string.Join(",", typeValues));
            }

            // 2. Data Rows
            foreach (var asset in selectedAssets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                var rowData = new List<string>();

                foreach (var type in activeTypes)
                {
                    string value = "";
                    switch (type)
                    {
                        case ColumnType.Guid:
                            value = AssetDatabase.AssetPathToGUID(assetPath);
                            break;

                        case ColumnType.AssetName:
                            value = asset.name;
                            break;

                        case ColumnType.AssetPath:
                            value = assetPath;
                            break;

                        case ColumnType.ResourcesPath:
                            var idx = assetPath.LastIndexOf("Resources/", StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0)
                            {
                                string resPath = assetPath[(idx + "Resources/".Length)..];
                                int dotIndex = resPath.LastIndexOf('.');
                                if (dotIndex >= 0) resPath = resPath.Substring(0, dotIndex);
                                value = resPath;
                            }
                            else
                            {
                                value = $"未在Resources中: {asset.name}";
                            }
                            break;

                        case ColumnType.AssetType:
                            value = asset.GetType().Name;
                            break;

                        case ColumnType.Extension:
                            value = Path.GetExtension(assetPath);
                            break;

                        case ColumnType.FileSize:
                            var fileInfo = new FileInfo(assetPath);
                            if (fileInfo.Exists)
                            {
                                float kb = fileInfo.Length / 1024f;
                                value = kb.ToString("F2");
                            }
                            else
                            {
                                value = "0";
                            }
                            break;

                        case ColumnType.Labels:
                            var labels = AssetDatabase.GetLabels(asset);
                            value = string.Join(";", labels);
                            break;
                    }

                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                    {
                        value = "\"" + value.Replace("\"", "\"\"") + "\"";
                    }

                    rowData.Add(value);
                }

                builder.AppendLine(string.Join(",", rowData));
            }

            try
            {
                File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
                EditorUtility.DisplayDialog("成功", "导出成功！", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"导出失败: {ex.Message}", "确定");
            }

            AssetDatabase.Refresh();
        }

        public void Refresh()
        {
            UpdateSelection();
        }

        public void DrawIMGUI() { }
    }
}
