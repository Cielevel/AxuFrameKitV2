using System.Collections.Generic;
using AxuFrameKit.Common.Editor.Common;
using AxuFrameKit.Common.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace AxuFrameKit.Common.Editor.KitBox.CommonGroup
{
    public static class InnerSymbols
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
    
    public class SymbolsEditPage : IKitPage
    {
        public string Title => "定义符号宏";
        
        private string _currentSymbol = "";
        
        public void Show()
        {
            UpperBox();
        }
        
        public void Show(Rect position) { }

        private void UpperBox()
        {
            GUI.backgroundColor = Color.blue;

            TitleDrawerEditor.DrawTitle("定义符号宏", Color.white);
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginHorizontal("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawInfo();
            DrawButton();
            EditorGUILayout.EndHorizontal();
            
            return;
            
            void DrawInfo()
            {
                GUI.backgroundColor = Color.black;
                EditorGUILayout.BeginVertical("box", GUILayout.Width(500), GUILayout.Height(300));
                
                GUILayout.Label("当前符号宏", new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize  = 15,
                    normal    = { textColor = Color.white }
                }, GUILayout.ExpandWidth(true), GUILayout.Height(30));

                if (string.IsNullOrEmpty(_currentSymbol) == false)
                {
                    GUILayout.Label("当前选择的符号宏： " + _currentSymbol, new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize  = 14,
                        normal    = { textColor = Color.white }
                    }, GUILayout.ExpandWidth(true), GUILayout.Height(30));
                    
                    if (InnerSymbols.SymbolDesc.TryGetValue(_currentSymbol, out var desc))
                    {
                        GUILayout.Label("符号宏描述： " + desc, new GUIStyle(EditorStyles.label)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            fontSize  = 12,
                            normal    = { textColor = Color.white }
                        }, GUILayout.ExpandWidth(true), GUILayout.Height(30));
                    }

                    if (ScriptDefineSymbolSetter.IsSymbolAdded(_currentSymbol) == false)
                    {
                        if (GUILayout.Button($"增添：{_currentSymbol}"))
                            ScriptDefineSymbolSetter.AddSymbol(_currentSymbol);
                    }
                    else
                    {
                        if (GUILayout.Button($"移除：{_currentSymbol}"))
                            ScriptDefineSymbolSetter.RemoveSymbol(_currentSymbol);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            void DrawButton()
            {
                EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
                
                foreach (var symbol in InnerSymbols.AllSymbols)
                    if (GUILayout.Button(symbol))
                        _currentSymbol = symbol;
                
                EditorGUILayout.EndVertical();
            }
        }
        
        public void Refresh() { }
    }
}