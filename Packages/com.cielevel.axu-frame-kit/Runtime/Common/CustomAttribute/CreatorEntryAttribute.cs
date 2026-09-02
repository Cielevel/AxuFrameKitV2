using System;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// SO 资产创建窗口（Editor/Common/Window/ScriptableObjectCreatorWindow）的条目元数据。
    /// 标注在 ScriptableObject 子类上，决定该类型在创建窗口左栏中的分组与显示名：
    /// - EntryPath 最后一段 = 面板显示名（替代类名），前序段 = 分组路径。
    /// - 示例：[CreatorEntry("道具")] → 顶级显示名"道具"；
    ///         [CreatorEntry("配置/战斗/SkillData")] → 分组 配置/战斗，显示名 SkillData。
    /// 未标注的类型仍会被窗口收录（显示名回退为类名、归入"未分组"）。
    /// 注意：标注不继承（Inherited = false），标注在 SO 中间基类时派生类不会继承其分组/显示名。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CreatorEntryAttribute : Attribute
    {
        /// <summary>条目路径：'/' 分隔；最后一段为显示名，前序段为分组路径。</summary>
        public string EntryPath { get; }

        public CreatorEntryAttribute(string entryPath)
        {
            EntryPath = entryPath;
        }

        // ───────────────────── 静态解析助手 ─────────────────────

        /// <summary>
        /// 解析 EntryPath（纯字符串逻辑，供创建窗口与测试复用）：
        /// 按 '/' 切分、每段 Trim、丢弃空段。段数 &lt;= 1 时 groups 为空数组、
        /// displayName 为 Trim 后的整串（为空则 displayName = null）；
        /// 段数 &gt; 1 时最后一段为 displayName，其余为 groups。
        /// </summary>
        public static void Parse(string entryPath, out string[] groups, out string displayName)
        {
            if (string.IsNullOrEmpty(entryPath))
            {
                groups = Array.Empty<string>();
                displayName = null;
                return;
            }

            var segments = entryPath.Split('/');
            var cleaned = new string[segments.Length];
            var count = 0;
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (trimmed.Length > 0)
                    cleaned[count++] = trimmed;
            }

            if (count <= 1)
            {
                groups = Array.Empty<string>();
                displayName = count == 1 ? cleaned[0] : null;
                return;
            }

            groups = new string[count - 1];
            Array.Copy(cleaned, groups, count - 1);
            displayName = cleaned[count - 1];
        }
    }
}
