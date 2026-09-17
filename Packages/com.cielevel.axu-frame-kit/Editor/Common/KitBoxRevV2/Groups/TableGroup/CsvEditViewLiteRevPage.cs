using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            var openBtn = new Button(LoadCsvFile) { text = "打开" };
            toolbar.Add(openBtn);

            var saveBtn = new Button(SaveCsv) { text = "保存" };
            toolbar.Add(saveBtn);

            toolbar.Add(new ToolbarSpacer());

            var addRowBtn = new Button(AddRow) { text = "添加行" };
            toolbar.Add(addRowBtn);

            var addColBtn = new Button(AddColumn) { text = "添加列" };
            toolbar.Add(addColBtn);

            _pathLabel = new Label("未加载文件，可拖拽单个 CSV 文件到窗口内");
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

            var dragHint = new Label("释放以加载 CSV");
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

            _tableData.Clear();
            // 带引号语义加载：引号包裹的字段可内嵌换行，故先把物理行合并为逻辑行
            // （累计引号数为偶数即闭合），再逐逻辑行走 ParseCsvLine 解析，
            // 保证与保存侧的 EscapeCsvField 对称往返
            var logicalLine = new StringBuilder();
            int quoteCount = 0;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                if (logicalLine.Length > 0) logicalLine.Append('\n');
                logicalLine.Append(rawLine);
                quoteCount += rawLine.Count(c => c == '"');

                if (quoteCount % 2 == 0)
                {
                    _tableData.Add(ParseCsvLine(logicalLine.ToString()));
                    logicalLine.Clear();
                    quoteCount = 0;
                }
            }

            // 兜底：末尾引号未闭合的残行也按一行解析，不丢数据
            if (logicalLine.Length > 0) _tableData.Add(ParseCsvLine(logicalLine.ToString()));

            RefreshGrid();
        }

        /// <summary>
        /// 带引号语义解析一行（逻辑行）CSV：字段以逗号分隔；引号包裹的字段内
        /// 逗号 / 引号 / 换行均视为数据，内部引号以连续两个引号表示。
        /// 与 <see cref="EscapeCsvField"/> 对称：后者转义出的内容可被本方法原样解析回来。
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // 连续两个引号 = 转义出的一个引号；单个引号 = 引号字段结束
                        if (i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(field.ToString()); field.Clear(); }
                else field.Append(c);
            }

            // 收尾字段（空行解析为单个空字段，与旧 Split(',') 行为一致）
            fields.Add(field.ToString());
            return fields;
        }

        /// <summary>带引号语义转义字段：含逗号 / 引号 / 换行 / 回车时用引号包裹，内部引号翻倍。</summary>
        private static string EscapeCsvField(string field)
        {
            field ??= string.Empty;
            bool needQuote = field.IndexOf(',') >= 0 || field.IndexOf('"') >= 0
                || field.IndexOf('\n') >= 0 || field.IndexOf('\r') >= 0;
            return needQuote ? "\"" + field.Replace("\"", "\"\"") + "\"" : field;
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

            // 覆盖写入前把旧文件复制为 .bak 备份，误存后可手动回退（备份失败仅告警，不阻断保存）
            try
            {
                if (File.Exists(_currentCsvPath)) File.Copy(_currentCsvPath, _currentCsvPath + ".bak", true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CSV 编辑器] 覆盖前备份旧文件失败（将继续保存）: {ex.Message}");
            }

            // 逐字段做带引号语义的转义（与加载侧 ParseCsvLine 对称）；编码固定 UTF8 无 BOM
            var lines = _tableData.Select(r => string.Join(",", r.Select(EscapeCsvField)));
            File.WriteAllLines(_currentCsvPath, lines, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("提示", "保存完成", "确定");
        }

        public void Refresh() => RefreshGrid();
        public void OnEnter() { }
        public void OnExit() { }
        public void DrawIMGUI() { }
    }
}
