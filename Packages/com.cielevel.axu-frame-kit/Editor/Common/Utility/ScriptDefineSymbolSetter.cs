using System.Collections.Generic;
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

        /// <summary>
        /// 读取当前平台宏定义列表的唯一口径：按 ';' 拆分并去掉空项。
        /// Add/Remove/Has 三条路径共用，杜绝 string.Contains 子串误命中（如 DEBUG 命中 DEBUG_X），
        /// 重组时也不会在空宏列表场景拼出前导/多余分号。
        /// </summary>
        private static List<string> GetSymbolList()
        {
            return PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget)
                .Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        public static bool IsSymbolAdded(string symbol)
        {
            if (symbol.IsNullOrEmpty())
                return false;

            // 拆分后逐项精确比较，而非对原始字符串做子串匹配
            return GetSymbolList().Contains(symbol);
        }

        public static void AddSymbol(string symbol)
        {
            if (IsSymbolAdded(symbol))
            {
                EditorUtility.DisplayDialog("宏定义已添加", $"（{symbol}）的宏定义已经添加了。", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("宏定义增添操作",
                                            $"将要向平台（{BuildTarget}）增添（{symbol}）宏定义，该过程将重新编译，是否确定？", "确定",
                                            "取消") == false)
                return;

            // 基于拆分后的列表重组字符串，空列表时不会产生前导分号
            var symbols = GetSymbolList();
            symbols.Add(symbol);
            PlayerSettings.SetScriptingDefineSymbols(CurrentBuildTarget, string.Join(";", symbols));
            EditorUtility.DisplayDialog("成功", $"已向（{BuildTarget}）添加了（{symbol}）宏定义，将进行编译。", "确定");
        }

        public static void RemoveSymbol(string symbol)
        {
            if (IsSymbolAdded(symbol) == false)
            {
                EditorUtility.DisplayDialog("宏定义未找到", $"未找到（{symbol}）的宏定义，无法移除。", "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("宏定义移除操作",
                                            $"将要移除当前平台（{BuildTarget}）的（{symbol}）宏定义，该过程将重新编译，是否确定？", "确定",
                                            "取消") == false)
                return;

            // 与守卫判定一致：精确移除所有同名项（原实现仅移除第一个），重组时不会残留空项
            var symbols = GetSymbolList();
            symbols.RemoveAll(s => s == symbol);
            PlayerSettings.SetScriptingDefineSymbols(CurrentBuildTarget, string.Join(";", symbols));
            EditorUtility.DisplayDialog("成功", $"已从（{BuildTarget}）移除了（{symbol}）宏定义，将进行编译。", "确定");
        }
    }
}
