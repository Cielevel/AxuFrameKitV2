using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit._Core.Extension;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using AxuFrameKit.Common.Editor.Utility.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.TableGroup
{
    public class SelectionToCsvRevPage : IKitPageRevV2
    {
        public string Title => "资产导出CSV";
        private IKitBoxRevV2 _kitBox;

        private TextField _pathField;
        private TextField _nameField;
        private ListView _assetListView;
        private Label _countLabel;
        private Button _exportButton;

        private List<string> _currentGuids = new List<string>();
        private string CsvTitle => $"资产名称,资产路径,资产加载路径,GUID{Environment.NewLine}assetName,assetPath,assetLoadPath,assetGuid{Environment.NewLine}string,string,string,string";

        public void Init(IKitBoxRevV2 kitBox) => _kitBox = kitBox;

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            // --- Header ---
            var header = new Label("选中资产导出为 CSV");
            header.AddToClassList("page-header");
            header.AddToClassList("mb-15");
            root.Add(header);

            // --- Path Settings ---
            var configBox = new VisualElement();
            configBox.AddToClassList("config-box");
            root.Add(configBox);

            var pathRow = new VisualElement();
            pathRow.AddToClassList("row");
            pathRow.AddToClassList("mb-10");
            configBox.Add(pathRow);

            _pathField = new TextField("保存路径") { value = Application.dataPath };
            _pathField.AddToClassList("flex-grow");
            pathRow.Add(_pathField);

            var browseBtn = new Button(BrowseFolder) { text = "浏览..." };
            browseBtn.AddToClassList("browse-button");
            pathRow.Add(browseBtn);

            _nameField = new TextField("文件名") { value = "CsvAsset.csv" };
            configBox.Add(_nameField);

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
        }

        private void BrowseFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("选择保存路径", _pathField.value, "");
            if (!string.IsNullOrEmpty(selected))
            {
                _pathField.value = selected;
            }
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

            string suffix = Path.GetExtension(_nameField.value).Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "" : ".csv";
            string path = (_pathField.value + "/" + _nameField.value + suffix).MakeSureFilePath();

            if (string.IsNullOrEmpty(path)) return;

            var csvUnits = selectedAssets.Select(e => new CsvRowUnit(
                e.name,
                AssetDatabase.GetAssetPath(e),
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e))
            ));

            if (CsvIOUtil.WriteCsv(path, CsvTitle, csvUnits))
            {
                EditorUtility.DisplayDialog("成功", "导出成功！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "导出失败", "确定");
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
