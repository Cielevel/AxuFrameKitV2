using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.UI
{
    /// <summary>
    /// AxuKit 编辑器窗口族共享主题：主题注册表 + EditorPrefs 持久化 + 根元素类挂载 + token 表集中加载。
    /// 主题变量定义见 Editor/Common/UI/AxuKitTokens.uss 的 .theme-* 块；
    /// 所有 AxuKit 窗口共享同一主题选择（EditorPrefs 键 "AxuKit.Theme"）。
    /// token 表（AxuKitTokens.uss）统一经 <see cref="GetTokens"/>/<see cref="AttachTokens"/> 加载：
    /// 使用规范化工程相对路径（AssetDatabase.LoadAssetAtPath 对含 ".." 的路径恒返回 null）。
    /// </summary>
    internal static class AxuKitTheme
    {
        /// <summary>主题持久化键（AxuKit 全部编辑器窗口共享）</summary>
        public const string PrefsKey = "AxuKit.Theme";

        /// <summary>旧版 KitBox 单窗口键（首次迁移来源）</summary>
        public const string LegacyPrefsKey = "AxuKitBoxRevV2.Theme";

        /// <summary>
        /// 主题变量表 AxuKitTokens.uss 的规范化工程相对路径。
        /// 注意：不要用脚本目录拼 "../" 相对路径——AssetDatabase.LoadAssetAtPath 对含 ".." 的路径恒返回 null。
        /// </summary>
        public const string TokensPath = "Packages/com.cielevel.axu-frame-kit/Editor/Common/UI/AxuKitTokens.uss";

        /// <summary>
        /// 主题注册表：新增主题时在 AxuKitTokens.uss 加一个 .theme-* 变量块，再在此登记
        /// </summary>
        public static readonly (string DisplayName, string Class)[] Themes =
        {
            ("UE5 经典", "theme-ue5"),
            ("macOS Light", "theme-macos-light"),
            ("Tokyo Night", "theme-tokyo-night"),
            ("Dracula", "theme-dracula"),
            ("Nord", "theme-nord"),
        };

        private static StyleSheet _cachedTokens;
        private static bool _loadFailedWarned;

        /// <summary>
        /// 加载主题变量表 AxuKitTokens.uss（带缓存；成功后复用同一实例，失败时告警一次并返回 null，
        /// 后续调用会重试以便资产恢复后自愈）。
        /// </summary>
        public static StyleSheet GetTokens()
        {
            if (_cachedTokens == null)
            {
                _cachedTokens = AssetDatabase.LoadAssetAtPath<StyleSheet>(TokensPath);
                if (_cachedTokens == null && !_loadFailedWarned)
                {
                    _loadFailedWarned = true;
                    Debug.LogWarning(
                        $"[AxuKitTheme] 主题变量表加载失败：\"{TokensPath}\" 不存在或尚未导入，" +
                        "var(--kb-*) 变量将失效，AxuKit 窗口可能出现深底黑字不可见问题。");
                }
            }
            return _cachedTokens;
        }

        /// <summary>
        /// 把主题变量表挂到窗口根元素（防重复挂载）。返回是否成功挂载；失败已在 <see cref="GetTokens"/> 告警。
        /// </summary>
        public static bool AttachTokens(VisualElement root)
        {
            var tokens = GetTokens();
            if (tokens == null || root == null) return false;
            if (!root.styleSheets.Contains(tokens))
                root.styleSheets.Add(tokens);
            return true;
        }

        /// <summary>读取已保存主题类（含旧键迁移与脏数据兜底）</summary>
        public static string ResolveSavedClass()
        {
            var saved = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
                saved = EditorPrefs.GetString(LegacyPrefsKey, Themes[0].Class);
            return Array.Exists(Themes, t => t.Class == saved) ? saved : Themes[0].Class;
        }

        /// <summary>把主题类挂到窗口根元素（先清掉全部主题类再挂新类）并持久化</summary>
        public static void Apply(VisualElement root, string themeClass)
        {
            if (root == null || string.IsNullOrEmpty(themeClass)) return;
            if (!Array.Exists(Themes, t => t.Class == themeClass)) themeClass = Themes[0].Class;

            foreach (var theme in Themes)
                root.RemoveFromClassList(theme.Class);
            root.AddToClassList(themeClass);
            EditorPrefs.SetString(PrefsKey, themeClass);
        }
    }
}
