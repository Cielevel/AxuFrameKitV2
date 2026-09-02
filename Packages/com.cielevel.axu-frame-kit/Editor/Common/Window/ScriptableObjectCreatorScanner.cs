using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Editor.Window
{
    /// <summary>
    /// ScriptableObjectCreatorWindow 的类型扫描器：
    /// 默认遍历全部已加载程序集（跳过 *.Tests 测试程序集，可经 assemblies 参数注入指定集合），
    /// 收集可创建资产的 ScriptableObject 子类，读取 <see cref="CreatorEntryAttribute"/> 填充分组路径与显示名，
    /// 未标注类型按所属程序集归入"未分组"。纯反射逻辑，不含 UI；窗口与单元测试共用。
    /// </summary>
    public static class ScriptableObjectCreatorScanner
    {
        // ───────────────────── 条目模型 ─────────────────────

        /// <summary>扫描结果条目：类型 + 面板显示名 + 分组路径 + 是否未标注。</summary>
        public sealed class Entry
        {
            public Type Type;
            public string DisplayName;
            public string[] GroupPath;

            /// <summary>无 CreatorEntry 标注（或标注解析为空）：已按"未分组/程序集名"归入 GroupPath。</summary>
            public bool IsUngrouped;
        }

        // ───────────────────── 常量与排除表 ─────────────────────

        /// <summary>未标注类型在分组路径中的顶级段名（与程序集名组成两段路径，窗口端经通用嵌套 Foldout 渲染）。</summary>
        private const string UngroupedGroupName = "未分组";

        /// <summary>引擎/系统命名空间前缀，扫描时排除（Unity、TMPro、Microsoft、System、I18N）。</summary>
        private static readonly string[] ExcludedNamespacePrefixes =
        {
            "Unity",
            "TMPro",
            "Microsoft",
            "System",
            "I18N",
        };

        // ───────────────────── 扫描 ─────────────────────

        /// <summary>
        /// 扫描可创建资产的 ScriptableObject 子类。
        /// assemblies 为 null 时遍历全部已加载程序集，并跳过测试程序集（程序集名含 ".Tests"，
        /// 防止单测 fixture 污染真实窗口的类型列表）与 Unity 官方程序集（程序集名以 "Unity" 开头，
        /// 其中部分类型命名空间无 Unity 前缀、命名空间规则拦不住）；显式传入时按传入集合迭代、
        /// 不做上述程序集级过滤（单元测试经此注入目标程序集）。
        /// 规则：非抽象 class、ScriptableObject 子类、非泛型类型定义、非 Obsolete、
        /// 非 EditorWindow / Editor 派生（二者均继承自 ScriptableObject，但其资产无创建意义）、
        /// 命名空间不以排除前缀开头（无命名空间的类型视为保留）。
        /// frameworkOnly = true 时仅保留 <see cref="CustomScriptableObject"/> 的派生类。
        /// 分组：有 CreatorEntry 标注且解析出有效内容的按解析结果；未标注（或解析为空）的按所属程序集
        /// 归入"未分组/程序集名"两段路径（<see cref="Entry.IsUngrouped"/> = true）。
        /// 排序：已标注的在前（按分组连接串 + 显示名，OrdinalIgnoreCase），未分组条目整体排最后
        /// （其分组串使同程序集自然聚簇、程序集按字母序）。
        /// </summary>
        public static List<Entry> Scan(bool frameworkOnly, Assembly[] assemblies = null)
        {
            var result = new List<Entry>();

            // assemblies 为 null → 遍历全部已加载程序集（跳过测试程序集与 Unity 官方程序集）；
            // 显式传入 → 原样迭代（单测注入）
            var sources = assemblies ?? AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in sources)
            {
                if (assemblies == null && IsTestAssembly(assembly)) continue;
                if (assemblies == null && IsUnityAssembly(assembly)) continue;
                if (assembly.IsDynamic) continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 部分类型加载失败时，退回到成功加载的那部分
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception)
                {
                    continue; // 无法枚举的程序集直接跳过
                }

                foreach (var type in types)
                {
                    if (!type.IsClass || type.IsAbstract) continue;
                    if (!type.IsSubclassOf(typeof(ScriptableObject))) continue;
                    // EditorWindow 与 Editor 是 ScriptableObject 下两条独立分支，需分别排除。
                    // 注意：本文件命名空间含 .Editor 段，简单名 Editor 会被解析为祖先命名空间，必须全限定
                    if (typeof(EditorWindow).IsAssignableFrom(type)) continue;
                    if (typeof(UnityEditor.Editor).IsAssignableFrom(type)) continue;
                    if (type.IsGenericTypeDefinition) continue;
                    if (type.GetCustomAttribute<ObsoleteAttribute>(false) != null) continue;
                    if (IsExcludedNamespace(type.Namespace)) continue;
                    if (frameworkOnly && !type.IsSubclassOf(typeof(CustomScriptableObject))) continue;

                    var entry = new Entry
                    {
                        Type = type,
                        DisplayName = type.Name,
                        GroupPath = Array.Empty<string>(),
                        IsUngrouped = true,
                    };

                    var creatorAttr = type.GetCustomAttribute<CreatorEntryAttribute>(false);
                    if (creatorAttr != null)
                    {
                        CreatorEntryAttribute.Parse(creatorAttr.EntryPath,
                            out var groupPath, out var displayName);
                        // 标注解析出有效内容（显示名或分组段）才视为已标注；空串/纯空白路径视同未标注
                        if (!string.IsNullOrEmpty(displayName) ||
                            (groupPath != null && groupPath.Length > 0))
                        {
                            entry.GroupPath = groupPath ?? Array.Empty<string>();
                            // 标注路径只有分组段时回退类名，保证显示名恒非空
                            entry.DisplayName = string.IsNullOrEmpty(displayName) ? type.Name : displayName;
                            entry.IsUngrouped = false;
                        }
                    }

                    // 未标注类型按所属程序集归入"未分组"，窗口端经通用嵌套 Foldout 自然渲染
                    if (entry.IsUngrouped)
                        entry.GroupPath = new[] { UngroupedGroupName, type.Assembly.GetName().Name };

                    result.Add(entry);
                }
            }

            result.Sort(CompareEntries);
            return result;
        }

        // ───────────────────── 过滤 ─────────────────────

        /// <summary>
        /// 按关键字过滤条目：OrdinalIgnoreCase 包含匹配显示名 / 类名 / FullName / 命名空间。
        /// 关键字为空或空白时原样返回列表。
        /// </summary>
        public static List<Entry> Filter(List<Entry> source, string search)
        {
            if (source == null) return source;
            if (string.IsNullOrWhiteSpace(search)) return source;

            var keyword = search.Trim();
            return source.Where(entry =>
                    Contains(entry.DisplayName, keyword) ||
                    Contains(entry.Type?.Name, keyword) ||
                    Contains(entry.Type?.FullName, keyword) ||
                    Contains(entry.Type?.Namespace, keyword))
                .ToList();
        }

        private static bool Contains(string text, string keyword)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ───────────────────── 内部工具 ─────────────────────

        private static bool IsExcludedNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return false; // 无命名空间的类型视为保留
            foreach (var prefix in ExcludedNamespacePrefixes)
            {
                if (ns.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>程序集名含 ".Tests"（不分大小写）即视为测试程序集（覆盖 *.Tests / *.Tests.Editor 全家族）。</summary>
        private static bool IsTestAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(".Tests", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 程序集名以 "Unity" 开头即视为 Unity 官方程序集（UnityEngine.*Module / UnityEditor.* /
        /// Unity.* 包程序集如 Unity.InputSystem、Unity.RenderPipelines.* 等）。
        /// 其中部分类型命名空间不含 Unity 前缀，命名空间排除规则拦不住，须在程序集层跳过。
        /// </summary>
        private static bool IsUnityAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return !string.IsNullOrEmpty(name) &&
                   name.StartsWith("Unity", StringComparison.Ordinal);
        }

        private static int CompareEntries(Entry a, Entry b)
        {
            // 有 CreatorEntry 分组的在前；未分组条目（"未分组/程序集名"）整体排最后，
            // 组内按分组串排序使同程序集聚簇、程序集名字母序
            if (a.IsUngrouped != b.IsUngrouped)
                return a.IsUngrouped ? 1 : -1;

            var result = string.Compare(JoinGroupPath(a), JoinGroupPath(b), StringComparison.OrdinalIgnoreCase);
            if (result != 0)
                return result;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static string JoinGroupPath(Entry entry)
        {
            return entry.GroupPath == null ? string.Empty : string.Join("/", entry.GroupPath);
        }
    }
}
