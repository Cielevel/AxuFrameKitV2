using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.TableGroup
{
    public class CsvEditViewLiteRevPage : IKitPageRevV2
    {
        public string Title => "CSV 编辑与查看";
        private IKitBoxRevV2 _kitBox;

        private VisualElement _gridContainer;
        private Label _pathLabel;
        private string _currentCsvPath;
        private List<List<string>> _tableData = new List<List<string>>();

        private float _columnWidth = 120f;

        public void Init(IKitBoxRevV2 kitBox) => _kitBox = kitBox;

        private VisualElement _dragOverlay;

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1;

            // --- Toolbar ---
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");
            root.Add(toolbar);

            var openBtn = new Button(LoadCsvFile) { text = "📂 打开" };
            toolbar.Add(openBtn);

            var saveBtn = new Button(SaveCsv) { text = "💾 保存" };
            toolbar.Add(saveBtn);

            toolbar.Add(new ToolbarSpacer());

            var addRowBtn = new Button(AddRow) { text = "+行" };
            toolbar.Add(addRowBtn);

            var addColBtn = new Button(AddColumn) { text = "+列" };
            toolbar.Add(addColBtn);

            _pathLabel = new Label("未加载文件 - 可拖拽单个 CSV 文件到此处");
            _pathLabel.AddToClassList("text-secondary");
            _pathLabel.style.marginLeft = 10;
            _pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(_pathLabel);

            // --- Grid Area ---
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.AddToClassList("csv-scroll-view");
            root.Add(scroll);

            _gridContainer = new VisualElement();
            _gridContainer.AddToClassList("csv-grid-container");
            scroll.Add(_gridContainer);

            // --- Drag and Drop Overlay ---
            _dragOverlay = new VisualElement();
            _dragOverlay.AddToClassList("drag-overlay");

            var dragHint = new Label("📄 释放以加载 CSV 文件");
            dragHint.AddToClassList("drag-hint-text");
            _dragOverlay.Add(dragHint);

            root.Add(_dragOverlay);

            // Drag and Drop support
            root.RegisterCallback<DragEnterEvent>(OnDragEnter);
            root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            root.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            root.RegisterCallback<DragPerformEvent>(OnDragPerform);

            return root;
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            if (IsValidSingleCsvFile())
            {
                _dragOverlay.style.display = DisplayStyle.Flex;
            }
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            _dragOverlay.style.display = DisplayStyle.None;
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (IsValidSingleCsvFile())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            _dragOverlay.style.display = DisplayStyle.None;

            if (IsValidSingleCsvFile())
            {
                DragAndDrop.AcceptDrag();
                LoadCsvFromPath(DragAndDrop.paths[0]);
                evt.StopPropagation();
            }
            else if (DragAndDrop.paths.Length > 1)
            {
                EditorUtility.DisplayDialog("错误", "请只拖拽一个 CSV 文件！", "确定");
            }
            else if (DragAndDrop.paths.Length == 1 && !IsCsvFile(DragAndDrop.paths[0]))
            {
                EditorUtility.DisplayDialog("错误", "请拖拽 CSV 文件（.csv）！", "确定");
            }
        }

        /// <summary>
        /// 验证是否为单个有效的 CSV 文件
        /// </summary>
        private bool IsValidSingleCsvFile()
        {
            return DragAndDrop.paths != null
                && DragAndDrop.paths.Length == 1
                && IsCsvFile(DragAndDrop.paths[0]);
        }

        private bool IsCsvFile(string path)
        {
            return !string.IsNullOrEmpty(path)
                && Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadCsvFile()
        {
            string path = EditorUtility.OpenFilePanel("选择 CSV", "", "csv");
            if (!string.IsNullOrEmpty(path)) LoadCsvFromPath(path);
        }

        private void LoadCsvFromPath(string path)
        {
            _currentCsvPath = path;
            _pathLabel.text = Path.GetFileName(path);

            var lines = File.ReadAllLines(path);
            _tableData.Clear();
            foreach (var l in lines) _tableData.Add(l.Split(',').ToList());

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _gridContainer.Clear();
            if (_tableData.Count == 0) return;

            for (int r = 0; r < _tableData.Count; r++)
            {
                var rowElement = new VisualElement();
                rowElement.AddToClassList("csv-row");

                for (int c = 0; c < _tableData[r].Count; c++)
                {
                    int row = r;
                    int col = c;
                    var field = new TextField { value = _tableData[r][c] };
                    field.AddToClassList("csv-cell");
                    field.RegisterValueChangedCallback(evt => _tableData[row][col] = evt.newValue);

                    if (r == 0)
                    {
                        field.AddToClassList("csv-cell-header");
                    }

                    rowElement.Add(field);
                }
                _gridContainer.Add(rowElement);
            }
        }

        private void AddRow()
        {
            if (_tableData.Count == 0) _tableData.Add(new List<string> { "" });
            else
            {
                var newRow = new List<string>();
                for (int i = 0; i < _tableData[0].Count; i++) newRow.Add("");
                _tableData.Add(newRow);
            }
            RefreshGrid();
        }

        private void AddColumn()
        {
            if (_tableData.Count == 0) _tableData.Add(new List<string> { "" });
            else
            {
                foreach (var r in _tableData) r.Add("");
            }
            RefreshGrid();
        }

        private void SaveCsv()
        {
            if (string.IsNullOrEmpty(_currentCsvPath)) return;
            var lines = _tableData.Select(r => string.Join(",", r));
            File.WriteAllLines(_currentCsvPath, lines);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("提示", "保存完成", "确定");
        }

        public void Refresh() => RefreshGrid();
        public void DrawIMGUI() { }
    }
}
