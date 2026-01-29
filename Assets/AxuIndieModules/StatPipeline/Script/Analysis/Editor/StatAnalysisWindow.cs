using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AxuIndieModules.StatPipeline
{
    public class StatAnalysisWindow : EditorWindow
    {
        private Vector2 _logScroll;
        private Vector2 _detailScroll;
        private int _selectedIndex = -1;
        private GUIStyle _selectedStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _richTextStyle;

        // 导航历史堆栈
        private Stack<StatDebugger.LogRecord> _navHistory = new Stack<StatDebugger.LogRecord>();

        [MenuItem("AxuIndieModules/StatPipeline/Stat Analysis Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatAnalysisWindow>("Stat Analysis");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            StatDebugger.OnLogAdded += Repaint;
        }

        private void OnDisable()
        {
            StatDebugger.OnLogAdded -= Repaint;
        }

        private void OnGUI()
        {
            InitStyles();

            // Toolbar
            DrawToolbar();

            // Split View
            EditorGUILayout.BeginHorizontal();

            // Left: Log List
            DrawLogList(GUILayout.Width(position.width * 0.3f));

            // Right: Details
            DrawDetails();

            EditorGUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_selectedStyle == null)
            {
                _selectedStyle = new GUIStyle(EditorStyles.label);
                _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.24f, 0.48f, 0.9f));
                _selectedStyle.normal.textColor = Color.white;

                _itemStyle = new GUIStyle(EditorStyles.label);
                _itemStyle.padding = new RectOffset(5, 5, 2, 2);

                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.fontSize = 12;

                _richTextStyle = new GUIStyle(EditorStyles.label);
                _richTextStyle.richText = true;
                _richTextStyle.wordWrap = true;
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Clear Logs", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    StatDebugger.ClearLogs();
                    _selectedIndex = -1;
                    _navHistory.Clear();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Repaint();
                }
            }
        }

        private void DrawLogList(GUILayoutOption width)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, width))
            {
                _logScroll = EditorGUILayout.BeginScrollView(_logScroll);
                var logs = StatDebugger.Logs;
                for (int i = logs.Count - 1; i >= 0; i--)
                {
                    var log = logs[i];
                    Rect rect = EditorGUILayout.BeginHorizontal();

                    bool isSelected = _selectedIndex == i;
                    GUIStyle style = isSelected ? _selectedStyle : _itemStyle;

                    if (isSelected && _navHistory.Count == 0)
                        _navHistory.Push(log);

                    if (GUI.Button(rect, "", GUIStyle.none))
                    {
                        _selectedIndex = i;
                        _navHistory.Clear();
                        _navHistory.Push(log);
                        GUI.FocusControl(null);
                    }

                    EditorGUILayout.LabelField($"[{log.Timestamp}] {log.StatID}", style);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawDetails()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedIndex < 0 || _selectedIndex >= StatDebugger.Logs.Count || _navHistory.Count == 0)
                {
                    EditorGUILayout.LabelField("Select a log to view details", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                // 绘制面包屑导航
                DrawNavigation();

                var currentLog = _navHistory.Peek();
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                // Header Info
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"Stat: {currentLog.StatName} ({currentLog.StatID})", _headerStyle);
                    EditorGUILayout.LabelField($"Base Value: {currentLog.BaseValue:F4}");
                    EditorGUILayout.LabelField($"Final Calculated: {currentLog.FinalValue:F4}");
                    EditorGUILayout.LabelField($"Actual Result: {currentLog.RealValue:F4}");
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Calculation Steps:", EditorStyles.boldLabel);

                foreach (var stage in currentLog.Stages)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"Stage: {stage.Name}", EditorStyles.boldLabel);
                        if (stage.Modifiers.Count == 0)
                        {
                            EditorGUILayout.LabelField("  <i>(No Modifiers)</i>", _richTextStyle);
                        }
                        else
                        {
                            foreach (var mod in stage.Modifiers)
                            {
                                DrawModifierDetail(mod, currentLog);
                            }
                        }
                    }
                    GUILayout.Space(5);
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Copy Raw Log to Clipboard"))
                {
                    GUIUtility.systemCopyBuffer = currentLog.FullLog;
                    Debug.Log("Log copied to clipboard.");
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawNavigation()
        {
            if (_navHistory.Count <= 1) return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("<< Back", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _navHistory.Pop();
                    _detailScroll = Vector2.zero;
                }

                GUILayout.Space(10);
                string path = "";
                var reversed = _navHistory.ToArray();
                System.Array.Reverse(reversed);
                for (int i = 0; i < reversed.Length; i++)
                {
                    path += reversed[i].StatID + (i == reversed.Length - 1 ? "" : " > ");
                }
                EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            }
        }

        private void DrawModifierDetail(StatDebugger.ModifierLog mod, StatDebugger.LogRecord currentLog)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.BeginHorizontal();
                string idInfo = !string.IsNullOrEmpty(mod.ID) ? $" ({mod.ID})" : "";
                EditorGUILayout.LabelField($"[<color=#{mod.Color}>{mod.Type}</color>] <b>{mod.Name}</b>{idInfo}", _richTextStyle);

                if (!string.IsNullOrEmpty(mod.SourceStatID) && currentLog.DependencySnapshots.ContainsKey(mod.SourceStatID))
                {
                    if (GUILayout.Button("Dive", GUILayout.Width(45)))
                    {
                        _navHistory.Push(currentLog.DependencySnapshots[mod.SourceStatID]);
                        _detailScroll = Vector2.zero;
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Weight: {mod.Weight}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"  <color=#888888>Calc:</color> {mod.Calculator} | <color=#888888>Stat:</color> {mod.StatInfo}", _richTextStyle);

                Rect progressRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, 1, $"{mod.PrevValue:F2} -> {mod.NextValue:F2}");
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
