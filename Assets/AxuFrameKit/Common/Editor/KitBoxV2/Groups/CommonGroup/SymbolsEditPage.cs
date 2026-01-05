using System.Collections.Generic;
using AxuFrameKit.Common.Editor.Common;
using AxuFrameKit.Common.Editor.KitBoxV2.Core;
using UnityEditor;
using UnityEngine;


namespace AxuFrameKit.Common.Editor.KitBoxV2.Groups.CommonGroup
{
    /// <summary>
    /// 内置符号宏定义
    /// </summary>
    public static class InnerSymbolsV2
    {
        private const string DebugX = "DEBUG_X";
        private const string YooAssetX = "YOO_ASSET_X";
        private const string AddressableX = "ADDRESSABLE_X";

        public static string[] AllSymbols => new[]
        {
            DebugX,
            YooAssetX,
            AddressableX,
        };

        public static Dictionary<string, string> SymbolDesc => new()
        {
            { DebugX, "启用工具内的调试Debug扩展。" },
            { YooAssetX, "启用针对YooAsset的封装扩展。" },
            { AddressableX, "启用对Addressable的封装扩展。" },
        };
    }

    /// <summary>
    /// 定义符号宏编辑页面
    /// </summary>
    public class SymbolsEditPage : IKitPageV2
    {
        public string Title => "定义符号宏";

        private IKitBoxV2 _kitBox;
        private string _currentSymbol = "";
        private Vector2 _scrollPos;

        public void Init(IKitBoxV2 kitBox)
        {
            _kitBox = kitBox;
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧：符号列表
            DrawSymbolList();

            // 右侧：符号详情
            DrawSymbolDetail();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSymbolList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200), GUILayout.ExpandHeight(true));

            KitBoxStyles.DrawSectionHeader("可用符号宏");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var symbol in InnerSymbolsV2.AllSymbols)
            {
                Color originalBg = GUI.backgroundColor;
                bool isSelected = _currentSymbol == symbol;
                bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(symbol);

                // 设置按钮颜色
                if (isSelected)
                    GUI.backgroundColor = KitBoxStyles.SelectedColor;
                else if (isAdded)
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.3f, 1f);

                EditorGUILayout.BeginHorizontal();

                // 状态指示
                string statusIcon = isAdded ? "✓" : "○";
                EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));

                if (GUILayout.Button(symbol, GUILayout.Height(25)))
                {
                    _currentSymbol = symbol;
                }

                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = originalBg;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSymbolDetail()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_currentSymbol))
            {
                // 未选择符号
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUIStyle hintStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                EditorGUILayout.LabelField("← 请从左侧选择一个符号宏", hintStyle);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
            else
            {
                KitBoxStyles.DrawSectionHeader("符号详情");

                // 符号名称
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    normal = { textColor = new Color(0.3f, 0.7f, 0.9f) }
                };
                EditorGUILayout.LabelField(_currentSymbol, nameStyle, GUILayout.Height(30));

                GUILayout.Space(10);

                // 符号描述
                if (InnerSymbolsV2.SymbolDesc.TryGetValue(_currentSymbol, out var desc))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUIStyle descStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 13,
                        wordWrap = true,
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                    };
                    EditorGUILayout.LabelField(desc, descStyle, GUILayout.Height(40));
                    EditorGUILayout.EndVertical();
                }

                GUILayout.Space(20);

                // 状态与操作
                bool isAdded = ScriptDefineSymbolSetter.IsSymbolAdded(_currentSymbol);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("当前状态：", GUILayout.Width(70));

                GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = isAdded ? Color.green : new Color(0.8f, 0.4f, 0.4f) }
                };
                EditorGUILayout.LabelField(isAdded ? "✓ 已启用" : "○ 未启用", statusStyle);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(20);

                // 操作按钮
                Color originalBg = GUI.backgroundColor;

                if (!isAdded)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f);
                    if (GUILayout.Button($"➕ 添加 {_currentSymbol}", GUILayout.Height(35)))
                    {
                        ScriptDefineSymbolSetter.AddSymbol(_currentSymbol);
                    }
                }
                else
                {
                    GUI.backgroundColor = new Color(0.7f, 0.3f, 0.3f);
                    if (GUILayout.Button($"➖ 移除 {_currentSymbol}", GUILayout.Height(35)))
                    {
                        ScriptDefineSymbolSetter.RemoveSymbol(_currentSymbol);
                    }
                }

                GUI.backgroundColor = originalBg;

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }

        public void Refresh() { }
    }
}
