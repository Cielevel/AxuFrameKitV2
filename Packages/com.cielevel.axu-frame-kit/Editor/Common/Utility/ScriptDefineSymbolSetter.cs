using System.Linq;
using AxuFrameKit._Core.Extension;
using UnityEditor;
using UnityEditor.Build;

namespace AxuFrameKit.Common.Editor.Common
{
    public static class ScriptDefineSymbolSetter
    {
        private static BuildTargetGroup BuildTarget => EditorUserBuildSettings.selectedBuildTargetGroup;
        
        private static NamedBuildTarget CurrentBuildTarget =>
            NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        public static bool IsSymbolAdded(string symbol)
        {
            if (symbol.IsNullOrEmpty())
                return false;

            var symbols = PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget).Split(";");
            
            return symbols.Contains(symbol);
        }
        
        public static void AddSymbol(string symbol)
        {
            if (PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget)
                              .Contains(symbol) == false)
            {
                bool result = EditorUtility.DisplayDialog("宏定义增添操作",
                                                          $"将要向平台（{BuildTarget}）增添（{symbol}）宏定义，该过程将重新编译，是否确定？", "确定",
                                                          "取消");

                if (result == false)
                    return;
            }
            else
            {
                EditorUtility.DisplayDialog("宏定义已添加", $"（{symbol}）的宏定义已经添加了。", "确定");
                return;
            }
            
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget);
            var newSymbols = currentSymbols + ";" + symbol;
            PlayerSettings.SetScriptingDefineSymbols(CurrentBuildTarget, newSymbols);
            EditorUtility.DisplayDialog("成功", $"已向（{BuildTarget}）添加了（{symbol}）宏定义，将进行编译。", "确定");
        }

        public static void RemoveSymbol(string symbol)
        {
            // 先查询是否有该宏定义
            if (PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget).Contains(symbol))
            {
                if (EditorUtility.DisplayDialog("宏定义移除操作",
                                                $"将要移除当前平台（{BuildTarget}）的（{symbol}）宏定义，该过程将重新编译，是否确定？", "确定",
                                                "取消") == false)
                    return;
            }
            else
            {
                EditorUtility.DisplayDialog("宏定义未找到", $"未找到（{symbol}）的宏定义，无法移除。", "确定");
                return;
            }
            
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget);
            var strs = currentSymbols.Split(';').ToList();
            int index = strs.IndexOf(symbol);
            if (index != -1)
            {
                strs.RemoveAt(index);
                var newSymbols = string.Join(";", strs);
                PlayerSettings.SetScriptingDefineSymbols(CurrentBuildTarget, newSymbols);
                EditorUtility.DisplayDialog("成功", $"已从（{BuildTarget}）移除了（{symbol}）宏定义，将进行编译。", "确定");
            }
        }
    }
}