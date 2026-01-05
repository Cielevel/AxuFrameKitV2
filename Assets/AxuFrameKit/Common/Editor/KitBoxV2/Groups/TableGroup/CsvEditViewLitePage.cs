using System;
using System.Collections.Generic;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEngine;

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
                EditorGUILayout.HelpBox("暂无数据。请加载 CSV 文件或手动添加行列。", MessageType.Info);
            }
            else
            {
                DrawTableArea();
            }
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
            
            // 第三行：缩放控制
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("表格缩放", GUILayout.Width(60));
            _cellScale = EditorGUILayout.Slider(_cellScale, 0.8f, 1.5f);
            
            if (!string.IsNullOrEmpty(_currentCsvPath))
            {
                GUILayout.Space(10);
                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                EditorGUILayout.LabelField($"📄 {System.IO.Path.GetFileName(_currentCsvPath)}", pathStyle, GUILayout.Width(200));
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTableArea()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            const float baseHeight = 22f;
            float cellHeight = baseHeight * _cellScale;
            
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
            
            // 计算表格尺寸
            Rect tableRect = EditorGUILayout.GetControlRect(GUILayout.Height((_tableList.Count + 1) * cellHeight));
            float totalWidth = tableRect.width;
            float cellWidth = _tableList[0].Count > 0 ? totalWidth / _tableList[0].Count : 0;
            
            // 绘制表格内容
            for (int i = 0; i < _tableList.Count; i++)
            {
                Rect rowRect = new Rect(tableRect.x, tableRect.y + i * cellHeight, totalWidth, cellHeight);
                
                // 交替行背景色
                if (i % 2 == 1)
                {
                    Color bgColor = new Color(0.25f, 0.25f, 0.25f, 0.3f);
                    EditorGUI.DrawRect(rowRect, bgColor);
                }
                
                for (int j = 0; j < _tableList[0].Count; j++)
                {
                    Color originalBg = GUI.backgroundColor;
                    GUI.backgroundColor = i == 0 ? new Color(0.3f, 0.5f, 0.3f) : Color.white;
                    
                    Rect cellRect = new Rect(rowRect.x + j * cellWidth, rowRect.y, cellWidth - 2, cellHeight - 2);
                    
                    // 清理引号
                    string cellValue = _tableList[i][j];
                    if (cellValue.Length > 0 && cellValue[0] == '"')
                        cellValue = cellValue.Substring(1);
                    if (cellValue.Length > 0 && cellValue[cellValue.Length - 1] == '"')
                        cellValue = cellValue.Substring(0, cellValue.Length - 1);
                    
                    _tableList[i][j] = EditorGUI.TextField(cellRect, cellValue, i == 0 ? titleStyle : contentStyle);
                    
                    GUI.backgroundColor = originalBg;
                }
            }
            
            // 绘制网格线
            DrawGridLines(tableRect, cellHeight, cellWidth);
            
            EditorGUILayout.EndScrollView();
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
        
        public void Refresh()
        {
            _tableList.Clear();
            _currentCsvPath = "";
            _row = 0;
            _column = 0;
        }
    }
}
