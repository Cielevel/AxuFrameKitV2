using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.TableGroup
{
    /// <summary>
    /// CSV 简易编辑与查看页面
    /// </summary>
    public class CsvEditViewLitePage : IKitPageV2
    {
        public string Title => "CSV 编辑与查看";

        private IKitBoxV2 _kitBox;
        private List<List<string>> _tableList = new();
        private int _row, _column;
        private Vector2 _scrollPosition;
        private string _currentCsvPath;
        private float _cellScale = 1f;
        private float _columnWidth = 150f; // 默认列宽


        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
        }

        public void Draw()
        {
            // 工具栏
            DrawToolbar();

            GUILayout.Space(10);

            // 表格区域
            if (_tableList.Count == 0 || _tableList[0].Count == 0)
            {
                EditorGUILayout.HelpBox("暂无数据。把 .csv 文件拖拽到此处，或点击按钮加载。", MessageType.Info);
            }
            else
            {
                DrawTableArea();
            }

            // 处理拖拽检测
            HandleDragAndDrop();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一行：文件操作
            EditorGUILayout.BeginHorizontal();

            Color originalBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("📂 打开 CSV", GUILayout.Height(28)))
            {
                LoadCsvFile();
            }

            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.5f);
            if (GUILayout.Button("📁 从选中加载", GUILayout.Height(28)))
            {
                LoadCsvFromSelection();
            }

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
            if (GUILayout.Button("💾 保存 CSV", GUILayout.Height(28)))
            {
                SaveCsv();
            }

            GUI.backgroundColor = originalBg;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 第二行：行列操作
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("➕ 增加行", GUILayout.Height(24)))
            {
                AddRow();
            }
            if (GUILayout.Button("➖ 减少行", GUILayout.Height(24)))
            {
                RemoveRow();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("➕ 增加列", GUILayout.Height(24)))
            {
                AddColumn();
            }
            if (GUILayout.Button("➖ 减少列", GUILayout.Height(24)))
            {
                RemoveColumn();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 第三行：缩放与列宽控制
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("缩放", GUILayout.Width(35));
            _cellScale = EditorGUILayout.Slider(_cellScale, 0.8f, 1.5f, GUILayout.Width(120));

            GUILayout.Space(10);

            EditorGUILayout.LabelField("列宽", GUILayout.Width(35));
            _columnWidth = EditorGUILayout.Slider(_columnWidth, 50f, 600f);

            if (GUILayout.Button("✨ 自动适配", GUILayout.Width(80)))
            {
                AutoFitColumns();
            }

            if (!string.IsNullOrEmpty(_currentCsvPath))
            {
                GUILayout.Space(10);
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                EditorGUILayout.LabelField($"📄 {System.IO.Path.GetFileName(_currentCsvPath)}", pathStyle);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTableArea()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            const float baseHeight = 22f;
            float cellHeight = baseHeight * _cellScale;
            float cellWidth = _columnWidth;

            var titleStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12 * _cellScale),
                normal = { textColor = Color.white }
            };

            var contentStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = Mathf.RoundToInt(11 * _cellScale),
                alignment = TextAnchor.MiddleLeft
            };

            // 获取当前列数
            int columnCount = _tableList[0].Count;
            float totalTableWidth = columnCount * cellWidth;
            float totalTableHeight = _tableList.Count * cellHeight;

            // 预留一块足够大的矩形区域，触发 ScrollView 的滚动条
            Rect tableRect = GUILayoutUtility.GetRect(totalTableWidth, totalTableHeight);

            // 绘制表格内容
            for (int i = 0; i < _tableList.Count; i++)
            {
                Rect rowRect = new Rect(tableRect.x, tableRect.y + i * cellHeight, totalTableWidth, cellHeight);

                // 剔除不可见区域的绘制 (简单的裁剪优化)
                if (rowRect.y + cellHeight < _scrollPosition.y || rowRect.y > _scrollPosition.y + 800) // 假设可见高度800
                    continue;

                // 交替行背景色
                if (i % 2 == 1)
                {
                    Color bgColor = new Color(0.25f, 0.25f, 0.25f, 0.3f);
                    EditorGUI.DrawRect(rowRect, bgColor);
                }

                for (int j = 0; j < columnCount; j++)
                {
                    Color originalBg = GUI.backgroundColor;
                    GUI.backgroundColor = i == 0 ? new Color(0.3f, 0.5f, 0.3f) : Color.white;

                    Rect cellRect = new Rect(rowRect.x + j * cellWidth, rowRect.y, cellWidth - 1, cellHeight - 1);

                    // 只有在屏幕横向可见时才绘制
                    if (cellRect.x + cellWidth >= _scrollPosition.x && cellRect.x <= _scrollPosition.x + EditorGUIUtility.currentViewWidth)
                    {
                        string cellValue = _tableList[i][j];
                        // 移除外层引号显示（仅显示，保存时会处理）
                        string displayValue = cellValue.Trim('"');

                        string newValue = EditorGUI.TextField(cellRect, displayValue, i == 0 ? titleStyle : contentStyle);
                        if (newValue != displayValue)
                        {
                            _tableList[i][j] = newValue;
                        }
                    }

                    GUI.backgroundColor = originalBg;
                }
            }

            // 绘制网格线
            DrawGridLines(tableRect, cellHeight, cellWidth);

            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

            // 严谨检查：仅接受单个对象拖拽
            if (DragAndDrop.objectReferences.Length != 1) return;

            Object obj = DragAndDrop.objectReferences[0];
            string path = AssetDatabase.GetAssetPath(obj);
            bool isCsv = !string.IsNullOrEmpty(path) && path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

            if (!isCsv) return;

            // 只有确认为单个 CSV 时才改变图标并消耗事件
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                string fullPath = System.IO.Path.GetFullPath(path);
                LoadCsvFromPath(fullPath);
            }
            evt.Use();
        }


        private void DrawGridLines(Rect tableRect, float cellHeight, float cellWidth)
        {
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            float lineWidth = 1f;

            // 水平线
            for (int i = 0; i <= _tableList.Count; i++)
            {
                float y = tableRect.y + i * cellHeight;
                Rect lineRect = new Rect(tableRect.x, y, tableRect.width, lineWidth);
                EditorGUI.DrawRect(lineRect, lineColor);
            }

            // 垂直线
            for (int j = 0; j <= _tableList[0].Count; j++)
            {
                float x = tableRect.x + j * cellWidth;
                Rect lineRect = new Rect(x, tableRect.y, lineWidth, _tableList.Count * cellHeight);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
        }

        private void LoadCsvFile()
        {
            string path = EditorUtility.OpenFilePanel("选择一个 CSV 文件", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            LoadCsvFromPath(path);
        }

        private void LoadCsvFromSelection()
        {
            var obj = Selection.activeObject;
            if (obj == null)
            {
                EditorUtility.DisplayDialog("提示", "请先在 Project 中选中一个 CSV 文件", "确定");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (!assetPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("提示", "选中的不是 CSV 文件", "确定");
                return;
            }

            string fullPath = System.IO.Path.GetFullPath(assetPath);
            LoadCsvFromPath(fullPath);
        }

        private void LoadCsvFromPath(string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);

            _currentCsvPath = path;
            _tableList.Clear();

            foreach (var line in lines)
            {
                var cells = line.Split(',');
                _tableList.Add(new List<string>(cells));
            }

            _row = _tableList.Count;
            _column = _tableList.Count > 0 ? _tableList[0].Count : 0;
        }

        private void SaveCsv()
        {
            if (string.IsNullOrEmpty(_currentCsvPath))
            {
                EditorUtility.DisplayDialog("提示", "请先加载一个 CSV 文件再保存", "确定");
                return;
            }

            try
            {
                var lines = new List<string>();
                foreach (var row in _tableList)
                {
                    lines.Add(string.Join(",", row));
                }

                System.IO.File.WriteAllLines(_currentCsvPath, lines, System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();

                Debug.Log($"[CsvEditor] CSV 保存成功：{_currentCsvPath}");
                EditorUtility.DisplayDialog("成功", "CSV 保存成功！", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvEditor] 保存 CSV 时出错：{ex.Message}");
                EditorUtility.DisplayDialog("错误", $"保存失败：{ex.Message}", "确定");
            }
        }

        private void AddRow()
        {
            var list = new List<string>();
            for (int i = 0; i < _column; i++)
                list.Add(string.Empty);
            _tableList.Add(list);
            _row++;
        }

        private void RemoveRow()
        {
            if (_tableList.Count > 0)
            {
                _tableList.RemoveAt(_tableList.Count - 1);
                _row--;
            }
        }

        private void AddColumn()
        {
            if (_tableList.Count > 0)
            {
                foreach (var t in _tableList)
                    t.Add(string.Empty);
            }
            _column++;
        }

        private void RemoveColumn()
        {
            if (_tableList.Count > 0)
            {
                foreach (var t in _tableList)
                    if (t.Count > 0)
                        t.RemoveAt(t.Count - 1);
            }
            _column = Math.Max(_column - 1, 0);
        }

        private void AutoFitColumns()
        {
            if (_tableList.Count == 0) return;

            float maxCalculatedWidth = 80f; // 最小宽度
            GUIStyle style = new GUIStyle(EditorStyles.textField);
            style.fontSize = Mathf.RoundToInt(11 * _cellScale);

            // 采样前 100 行（性能权衡）来计算所需的最大宽度
            int sampleCount = Mathf.Min(_tableList.Count, 100);
            for (int i = 0; i < sampleCount; i++)
            {
                foreach (string cell in _tableList[i])
                {
                    float width = style.CalcSize(new GUIContent(cell)).x + 20f; // 留一点 padding
                    if (width > maxCalculatedWidth) maxCalculatedWidth = width;
                }
            }

            _columnWidth = Mathf.Clamp(maxCalculatedWidth, 100f, 600f);
        }

        public void Refresh()
        {
            _tableList.Clear();
            _currentCsvPath = "";
            _row = 0;
            _column = 0;
            _columnWidth = 150f;
        }
    }
}

