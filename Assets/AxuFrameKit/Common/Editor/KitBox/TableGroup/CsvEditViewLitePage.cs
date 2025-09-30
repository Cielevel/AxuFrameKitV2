using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.TableGroup
{
    public class CsvEditViewLitePage : IKitPage
    {
        public string Title => "Csv简易编辑与查看";

        private List<List<string>> _tableList = new();

        private int _row, _column;

        private Vector2 _scrollPosition;

        private string _currentCsvPath;

        private float _cellScale = 1f;

        public void Show()
        {
            TitleDrawerEditor.DrawTitle("Csv简易编辑与查看", Color.white);

            _cellScale = EditorGUILayout.Slider("表格大小", _cellScale, 1, 1.5f);
            
            DrawButton();

            if (_tableList.Count == 0 || _tableList[0].Count == 0)
                return;

            // 开始滚动区域
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawTableWithLines();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTableWithLines()
        {
            const float baseHeight = 20f;
            const float lineWidth  = 1f;

            float cellHeight = baseHeight * _cellScale; // 行高放大

            Color lineColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            lineColor.a = 0.7f;

            var titleLineStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize  = Mathf.RoundToInt(12 * _cellScale), // 字体放大
                normal    = new GUIStyleState { textColor = Color.white }
            };

            var contentStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize  = Mathf.RoundToInt(12 * _cellScale),
                alignment = TextAnchor.MiddleLeft
            };

            // 表格总高度要跟着 cellHeight 变化
            Rect  tableRect  = EditorGUILayout.GetControlRect(GUILayout.Height((_tableList.Count + 1) * cellHeight));
            float totalWidth = tableRect.width;
            float cellWidth  = _tableList[0].Count > 0 ? totalWidth / _tableList[0].Count : 0;

            for (var i = 0; i < _tableList.Count; i++)
            {
                Rect rowRect = new Rect(tableRect.x, tableRect.y + i * cellHeight, totalWidth, cellHeight);

                if (i % 2 == 1)
                {
                    Color bgColor = EditorGUIUtility.isProSkin
                        ? new Color(0.3f, 0.3f, 0.3f, 0.2f)
                        : new Color(0.8f, 0.8f, 0.8f, 0.2f);
                    EditorGUI.DrawRect(rowRect, bgColor);
                }

                for (var j = 0; j < _tableList[0].Count; j++)
                {
                    GUI.backgroundColor = i == 0 ? Color.green : Color.white;
                    Rect cellRect = new Rect(rowRect.x + j * cellWidth, rowRect.y, cellWidth, cellHeight);

                    if (_tableList[i][j][0].Equals('"'))
                        _tableList[i][j] = _tableList[i][j][1..];
                    if (_tableList[i][j].Last().Equals('"'))
                        _tableList[i][j] = _tableList[i][j][..^1];
                    
                    if (i == 0)
                        _tableList[i][j] = EditorGUI.TextField(cellRect, _tableList[i][j], titleLineStyle);
                    else
                        _tableList[i][j] = EditorGUI.TextField(cellRect, _tableList[i][j], contentStyle);
                }

                EditorGUILayout.Space(2);
            }

            DrawGridLines(tableRect, cellHeight, cellWidth, lineColor, lineWidth);
        }


        private void DrawGridLines(Rect tableRect, float cellHeight, float cellWidth, Color lineColor, float lineWidth)
        {
            // 水平线
            for (int i = 0; i <= _tableList.Count; i++)
            {
                float y        = tableRect.y + i * cellHeight;
                Rect  lineRect = new Rect(tableRect.x, y, tableRect.width, lineWidth);
                EditorGUI.DrawRect(lineRect, lineColor);
            }

            // 垂直线 - Toggle分割线
            Rect toggleLineRect = new Rect(tableRect.x, tableRect.y, lineWidth, _tableList.Count * cellHeight);
            EditorGUI.DrawRect(toggleLineRect, lineColor);

            // 垂直线 - 列分割线
            for (int j = 1; j < _tableList[0].Count; j++)
            {
                float x        = tableRect.x + j * cellWidth;
                Rect  lineRect = new Rect(x, tableRect.y, lineWidth, _tableList.Count * cellHeight);
                EditorGUI.DrawRect(lineRect, lineColor);
            }

            // 右边框
            Rect rightBorderRect = new Rect(tableRect.x + tableRect.width - lineWidth, tableRect.y, lineWidth,
                                            _tableList.Count * cellHeight);
            EditorGUI.DrawRect(rightBorderRect, lineColor);
        }

        private void DrawButton()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("增加一行"))
            {
                var list = new List<string>();
                for (var i = 0; i < _column; i++)
                    list.Add(string.Empty);

                _tableList.Add(list);
            }

            if (GUILayout.Button("减少一行"))
            {
                if (_tableList.Count != 0)
                    _tableList.RemoveAt(_tableList.Count - 1);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("增加一列"))
            {
                if (_tableList.Count != 0)
                    foreach (var t in _tableList)
                        t.Add(string.Empty);
                _column++;
            }

            if (GUILayout.Button("减少一列"))
            {
                if (_tableList.Count != 0)
                    foreach (var t in _tableList)
                        if (t.Count > 0)
                            t.RemoveAt(t.Count - 1);

                _column--;
                _column = Math.Max(_column, 0);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("读取CSV文件"))
                LoadCsvFile();

            if (GUILayout.Button("Editor内读取CSV文件（选中一个csv文件）"))
                LoadCsvFromSelection();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("保存CSV"))
                SaveCsv();
        }

        private void LoadCsvFile()
        {
            // 打开文件选择框，只允许选一个文件
            string path = EditorUtility.OpenFilePanel("选择一个 CSV 文件", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            // 读取内容
            string[] lines = System.IO.File.ReadAllLines(path);

            _currentCsvPath = path;

            _tableList.Clear();
            foreach (var line in lines)
            {
                // 用逗号分割一行（这里是最简单的方式，如果有引号/转义需要用更复杂的解析器）
                var cells = line.Split(',');
                _tableList.Add(new List<string>(cells));
            }

            // 更新行列数量
            _row    = _tableList.Count;
            _column = _tableList.Count > 0 ? _tableList[0].Count : 0;
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

            string   fullPath = System.IO.Path.GetFullPath(assetPath);
            string[] lines    = System.IO.File.ReadAllLines(fullPath);

            _currentCsvPath = fullPath;

            _tableList.Clear();
            foreach (var line in lines)
            {
                var cells = line.Split(',');
                _tableList.Add(new List<string>(cells));
            }

            _row    = _tableList.Count;
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
                    // 简单写法：用逗号拼接
                    // 如果 CSV 有特殊字符（逗号、换行、引号），需要加转义/引号处理
                    lines.Add(string.Join(",", row));
                }

                System.IO.File.WriteAllLines(_currentCsvPath, lines, System.Text.Encoding.UTF8);

                AssetDatabase.Refresh(); // 刷新 Project 视图
                Debug.Log($"CSV 保存成功：{_currentCsvPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存 CSV 时出错：{ex.Message}");
            }
        }

        public void Refresh() { }
    }
}