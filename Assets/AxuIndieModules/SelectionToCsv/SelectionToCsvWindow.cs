#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AxuIndieModules.SelectionToCsv
{
    public class SelectionToCsvWindow : EditorWindow
    {
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

        [MenuItem("AxuIndieModules/Selection To Csv")]
        public static void ShowWindow()
        {
            var window = GetWindow<SelectionToCsvWindow>("Selection To Csv");
            window.minSize = new Vector2(450, 520);
        }

        private void OnEnable()
        {
            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                if (!_columnStates.ContainsKey(type))
                    _columnStates[type] = true;
            }

            Selection.selectionChanged += UpdateSelection;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= UpdateSelection;
        }

        private void CreateGUI()
        {
            // Load and add USS
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            var dirPath = System.IO.Path.GetDirectoryName(scriptPath);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{dirPath}/SelectionToCsvWindow.uss");
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("ue5-root");

            var titleLabel = new Label("📋 Selection To CSV");
            titleLabel.AddToClassList("window-title");
            rootVisualElement.Add(titleLabel);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            rootVisualElement.Add(scrollView);

            // --- Path Settings ---
            CreatePathSettingsPanel(scrollView);

            // --- Column Selection Area ---
            CreateColumnSettingsPanel(scrollView);

            // --- Selection List ---
            CreateSelectionListPanel(scrollView);

            // --- Export Action ---
            CreateActionPanel(rootVisualElement);

            UpdateSelection();
        }

        private void CreatePathSettingsPanel(VisualElement parent)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            parent.Add(container);

            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("⚙️ 导出设置");
            title.AddToClassList("group-title");
            header.Add(title);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            container.Add(content);

            // Path input row
            var pathRow = new VisualElement();
            pathRow.AddToClassList("item-row");
            content.Add(pathRow);

            var pathLabel = new Label("保存路径:");
            pathLabel.AddToClassList("toolbar-label");
            pathLabel.style.minWidth = 60;
            pathRow.Add(pathLabel);

            _pathField = new TextField();
            _pathField.AddToClassList("ue5-text-field");
            _pathField.value = Application.dataPath;
            pathRow.Add(_pathField);

            var browseBtn = new Button(BrowseFolder) { text = "📂" };
            browseBtn.AddToClassList("action-btn");
            browseBtn.tooltip = "浏览文件夹...";
            pathRow.Add(browseBtn);

            _exportToSelectedFolderBtn = new Button(SelectCurrentFolder) { text = "👈 当前目录" };
            _exportToSelectedFolderBtn.AddToClassList("action-btn");
            _exportToSelectedFolderBtn.tooltip = "在 Project 窗口中选中一个文件夹后可用";
            _exportToSelectedFolderBtn.SetEnabled(false);
            pathRow.Add(_exportToSelectedFolderBtn);

            // File Name Row
            var nameRow = new VisualElement();
            nameRow.AddToClassList("item-row");
            content.Add(nameRow);

            var nameLabel = new Label("文件名称:");
            nameLabel.AddToClassList("toolbar-label");
            nameLabel.style.minWidth = 60;
            nameRow.Add(nameLabel);

            _nameField = new TextField();
            _nameField.AddToClassList("ue5-text-field");
            _nameField.value = "CsvAsset.csv";
            nameRow.Add(_nameField);

            // Header Options Row
            var optionsRow = new VisualElement();
            optionsRow.AddToClassList("item-row");
            optionsRow.style.paddingTop = 6;
            optionsRow.style.paddingBottom = 6;
            content.Add(optionsRow);

            _includeEnglishKeysToggle = new Toggle("导出Key行 (Row 2)");
            _includeEnglishKeysToggle.value = true;
            _includeEnglishKeysToggle.style.marginRight = 10;
            optionsRow.Add(_includeEnglishKeysToggle);

            _includeTypesToggle = new Toggle("导出类型行 (Row 3)");
            _includeTypesToggle.value = true;
            optionsRow.Add(_includeTypesToggle);
        }

        private void CreateColumnSettingsPanel(VisualElement parent)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            parent.Add(container);

            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("📊 导出列");
            title.AddToClassList("group-title");
            header.Add(title);

            var helperRow = new VisualElement();
            helperRow.style.flexDirection = FlexDirection.Row;
            helperRow.style.marginLeft = StyleKeyword.Auto;
            header.Add(helperRow);

            var selectAllBtn = new Button(() => SetAllColumns(true)) { text = "全选" };
            selectAllBtn.AddToClassList("action-btn");
            helperRow.Add(selectAllBtn);

            var selectNoneBtn = new Button(() => SetAllColumns(false)) { text = "全不选" };
            selectNoneBtn.AddToClassList("action-btn");
            helperRow.Add(selectNoneBtn);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexDirection = FlexDirection.Row;
            content.style.flexWrap = Wrap.Wrap;
            container.Add(content);

            foreach (ColumnType type in Enum.GetValues(typeof(ColumnType)))
            {
                var t = type;
                var btn = new Button(() => ToggleColumn(t));
                btn.text = GetButtonText(t);
                btn.name = $"col-btn-{t}";
                btn.AddToClassList("column-toggle-btn");

                UpdateButtonVisual(btn, _columnStates[t]);
                content.Add(btn);
            }
        }

        private void CreateSelectionListPanel(VisualElement parent)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.flexGrow = 1;
            container.style.minHeight = 150;
            parent.Add(container);

            var header = new VisualElement();
            header.AddToClassList("group-header");
            container.Add(header);

            var title = new Label("📦 当前选中资产");
            title.AddToClassList("group-title");
            header.Add(title);

            _countLabel = new Label("0 个资产");
            _countLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            _countLabel.style.fontSize = 11;
            header.Add(_countLabel);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.flexGrow = 1;
            content.style.paddingLeft = 0;
            content.style.paddingRight = 0;
            container.Add(content);

            _assetListView = new ListView(_currentGuids, 24, MakeItem, BindItem);
            _assetListView.style.flexGrow = 1;
            content.Add(_assetListView);
        }

        private void CreateActionPanel(VisualElement parent)
        {
            var container = new VisualElement();
            container.AddToClassList("group-container");
            container.style.marginBottom = 0;
            container.style.marginTop = 5;
            parent.Add(container);

            var content = new VisualElement();
            content.AddToClassList("group-content");
            content.style.paddingTop = 10;
            content.style.paddingBottom = 10;
            container.Add(content);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            content.Add(buttonRow);

            _exportButton = new Button(ExportSelectedAssets) { text = "📤 导出到 CSV" };
            _exportButton.AddToClassList("primary-btn");
            _exportButton.style.minWidth = 180;
            _exportButton.style.height = 32;
            _exportButton.style.fontSize = 13;
            buttonRow.Add(_exportButton);
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
                var btn = rootVisualElement.Q<Button>($"col-btn-{type}");
                if (btn != null)
                {
                    UpdateButtonVisual(btn, _columnStates[type]);
                }
            }
        }

        private string GetButtonText(ColumnType type)
        {
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
            row.AddToClassList("item-row");

            var icon = new Image();
            icon.name = "asset-icon";
            icon.AddToClassList("item-icon");
            row.Add(icon);

            var name = new Label();
            name.name = "asset-name";
            name.AddToClassList("item-name");
            row.Add(name);

            var type = new Label();
            type.name = "asset-type";
            type.style.fontSize = 10;
            type.style.color = new Color(0.6f, 0.6f, 0.6f);
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
            if (_countLabel != null) _countLabel.text = $"{_currentGuids.Count} 个资产";
            if (_assetListView != null) _assetListView.RefreshItems();
            if (_exportButton != null) _exportButton.SetEnabled(_currentGuids.Count > 0);

            UpdateExportToSelectedFolderButton();
        }

        private void UpdateExportToSelectedFolderButton()
        {
            if (_exportToSelectedFolderBtn == null) return;

            bool canSelect = false;
            string selectedFolderPath = null;

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
                _exportToSelectedFolderBtn.text = "👈 选择当前";
            }
            else
            {
                _exportToSelectedFolderBtn.tooltip = "在 Project 窗口中选中一个文件夹后可用";
                _exportToSelectedFolderBtn.text = "👈 当前目录";
            }
        }

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

        private void SelectCurrentFolder()
        {
            string folderPath = GetSelectedFolderPath();
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog("提示", "请在 Project 窗口中选中一个文件夹！", "确定");
                return;
            }

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
            string path = _pathField.value + "/" + _nameField.value + suffix;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

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
    }
}
#endif
