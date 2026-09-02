using System;
using System.Linq;
using AxuFrameKit.Common.CustomAttribute;
using AxuFrameKit.Common.Editor.Window;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] ScriptableObjectCreatorScanner：SO 子类扫描/过滤规则
    /// 与 CreatorEntryAttribute 条目路径解析（fixture 类型为嵌套 internal class，
    /// 单测经 assemblies 参数注入本测试程序集进行扫描；默认扫描会整层跳过 *.Tests 程序集，
    /// 排除类规则的"排除证据"仅在注入扫描下才有意义）。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class ScriptableObjectCreatorScannerTests
    {
        // ───────────────────── fixture 类型 ─────────────────────

        /// <summary>无标注普通 SO（应被收录，显示名回退类名，按程序集归入未分组）。</summary>
        internal class ScanFakePlainSo : ScriptableObject { }

        /// <summary>顶级显示名标注（无分组）。</summary>
        [CreatorEntry("道具")]
        internal class ScanFakeNamedSo : ScriptableObject { }

        /// <summary>分组路径标注（配置/战斗，显示名取最后一段）。</summary>
        [CreatorEntry("配置/战斗/ScanFakeSkillSo")]
        internal class ScanFakeSkillSo : ScriptableObject { }

        /// <summary>抽象类（应被排除）。</summary>
        internal abstract class ScanFakeAbstractSo : ScriptableObject { }

        /// <summary>框架 CustomScriptableObject 派生（仅框架开关下保留）。</summary>
        internal class ScanFakeFrameworkSo : CustomScriptableObject { }

        /// <summary>EditorWindow 派生（继承自 ScriptableObject，应被排除）。</summary>
        internal class ScanFakeWindowSo : EditorWindow { }

        /// <summary>Editor 派生（Inspector 绘制器分支，与 EditorWindow 互不派生，应被排除；须全限定，命名空间含 .Editor 段）。</summary>
        internal class ScanFakeEditorSo : UnityEditor.Editor { }

#pragma warning disable 612, 618
        /// <summary>Obsolete 标注（应被排除）。</summary>
        [Obsolete]
        internal class ScanFakeObsoleteSo : ScriptableObject { }
#pragma warning restore 612, 618

        /// <summary>泛型类型定义（应被排除）。</summary>
        internal class ScanFakeGenericSo<T> : ScriptableObject { }

        // ───────────────────── 用例 ─────────────────────

        [Test]
        public void Scan_普通程序集SO子类_被收录()
        {
            // Arrange：注入本测试程序集，绕过默认的测试程序集整层排除
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };
            var asmName = typeof(ScriptableObjectCreatorScannerTests).Assembly.GetName().Name;

            // Act
            var result = ScriptableObjectCreatorScanner.Scan(false, asm);

            // Assert：三个具体 ScanFake（非抽象、非泛型定义）均应被收录
            foreach (var fakeName in new[] { nameof(ScanFakePlainSo), nameof(ScanFakeNamedSo), nameof(ScanFakeSkillSo) })
            {
                var entry = result.FirstOrDefault(e => e.Type.Name == fakeName);
                Assert.That(entry, Is.Not.Null, $"应收录 {fakeName}");
                Assert.That(entry.Type.IsAbstract, Is.False, $"{fakeName} 不应是抽象类");
                Assert.That(entry.Type.IsGenericTypeDefinition, Is.False, $"{fakeName} 不应是泛型定义");
            }

            // 无标注类型 DisplayName 回退类名，并按所属程序集归入"未分组"
            var plain = result.First(e => e.Type.Name == nameof(ScanFakePlainSo));
            Assert.That(plain.DisplayName, Is.EqualTo(nameof(ScanFakePlainSo)), "无标注时 DisplayName 应等于类名");
            Assert.That(plain.GroupPath, Is.EqualTo(new[] { "未分组", asmName }),
                "无标注时 GroupPath 应为 未分组/程序集名 两段");
            Assert.That(plain.IsUngrouped, Is.True, "无标注时 IsUngrouped 应为 true");
        }

        [Test]
        public void Scan_抽象与Unity系统类型_被排除()
        {
            // 注入扫描下才能观察到排除规则本身（默认扫描会整层跳过测试程序集）
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };

            // Act
            var result = ScriptableObjectCreatorScanner.Scan(false, asm);

            // Assert
            Assert.That(result.Any(e => e.Type.Name == nameof(ScanFakeAbstractSo)), Is.False, "抽象类不应被收录");
            Assert.That(result.Any(e => e.Type == typeof(ScriptableObject)), Is.False, "ScriptableObject 自身不应被收录");
            Assert.That(result.Any(e => e.Type.Name == nameof(ScanFakeWindowSo)), Is.False,
                "EditorWindow 派生不应被收录（其资产无创建意义）");
#pragma warning disable 612, 618
            Assert.That(result.Any(e => e.Type.Name == nameof(ScanFakeObsoleteSo)), Is.False, "Obsolete 类型不应被收录");
#pragma warning restore 612, 618
            // 泛型定义的 Type.Name 带 `1 后缀，须用 typeof 开放泛型精确比对
            Assert.That(result.Any(e => e.Type == typeof(ScanFakeGenericSo<>)), Is.False, "泛型类型定义不应被收录");
            Assert.That(result.Any(e => e.Type.Name == nameof(UnityFixtures.ScanTests.ScanFakeUnityNsSo)), Is.False,
                "命名空间命中排除前缀的类型不应被收录");
            Assert.That(result.Any(e => e.Type.Name == nameof(ScanFakeEditorSo)), Is.False,
                "Editor 派生不应被收录（其资产无创建意义）");

            // 全局不变式：任何条目都不得是 EditorWindow / Editor 派生（防工具类回归混入）
            Assert.That(result.All(e => !typeof(EditorWindow).IsAssignableFrom(e.Type)
                                        && !typeof(UnityEditor.Editor).IsAssignableFrom(e.Type)), Is.True,
                "扫描结果不应包含任何 EditorWindow / Editor 派生类");

            // 所有条目的命名空间都不得以引擎/系统前缀开头（无命名空间视为保留）
            var prefixes = new[] { "Unity", "TMPro", "Microsoft", "System", "I18N" };
            foreach (var entry in result)
            {
                var ns = entry.Type.Namespace;
                if (string.IsNullOrEmpty(ns)) continue;
                foreach (var prefix in prefixes)
                {
                    Assert.That(ns.StartsWith(prefix, StringComparison.Ordinal), Is.False,
                        $"{entry.Type.FullName} 的命名空间命中排除前缀 {prefix}");
                }
            }
        }

        [Test]
        public void Scan_测试程序集_默认排除()
        {
            // Act：默认扫描（遍历 AppDomain），应整层跳过 *.Tests 程序集
            var result = ScriptableObjectCreatorScanner.Scan(false);

            // Assert：fixture 家族不得混入真实窗口的类型列表
            Assert.That(result.Any(e => e.Type.Name.StartsWith("ScanFake", StringComparison.Ordinal)), Is.False,
                "默认扫描不应收录任何 ScanFake* fixture（测试程序集应被整层跳过）");
            Assert.That(result.Any(e => e.Type.Name == nameof(UnityFixtures.ScanTests.ScanFakeUnityNsSo)), Is.False,
                "默认扫描不应收录 UnityNs fixture");
            Assert.That(result.All(e => e.Type.Assembly != typeof(ScriptableObjectCreatorScannerTests).Assembly),
                Is.True, "默认扫描结果不应包含来自测试程序集的任何类型");

            // Assert：Unity 官方程序集（程序集名以 Unity 开头，如 Unity.InputSystem / UnityEditor.TestRunner）
            // 应被整层跳过，其内部 SO 不再浮现在"未分组"区
            Assert.That(result.All(e => !e.Type.Assembly.GetName().Name.StartsWith("Unity", StringComparison.Ordinal)),
                Is.True, "默认扫描结果不应包含来自 Unity 官方程序集的任何类型");
        }

        [Test]
        public void Scan_仅框架开关_只留CustomScriptableObject派生()
        {
            // Arrange：注入扫描，两个开关形态同源可比
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };

            // Act
            var full = ScriptableObjectCreatorScanner.Scan(false, asm);
            var frameworkOnly = ScriptableObjectCreatorScanner.Scan(true, asm);

            // Assert
            Assert.That(frameworkOnly.Any(e => e.Type.Name == nameof(ScanFakeFrameworkSo)), Is.True,
                "frameworkOnly=true 应收录 CustomScriptableObject 派生");
            Assert.That(frameworkOnly.Any(e => e.Type.Name == nameof(ScanFakePlainSo)), Is.False,
                "frameworkOnly=true 不应收录普通 SO");
            Assert.That(frameworkOnly.All(f => full.Any(a => a.Type == f.Type)), Is.True,
                "仅框架结果应为全量结果的子集");
        }

        [Test]
        public void CreatorEntry_Parse_多段路径_分组与显示名正确()
        {
            // 多段：最后一段为显示名，其余为分组
            CreatorEntryAttribute.Parse("配置/战斗/SkillData", out var groups, out var displayName);
            Assert.That(groups, Is.EqualTo(new[] { "配置", "战斗" }));
            Assert.That(displayName, Is.EqualTo("SkillData"));

            // 单段：无分组，整段为显示名
            CreatorEntryAttribute.Parse("道具", out groups, out displayName);
            Assert.That(groups, Is.Empty);
            Assert.That(displayName, Is.EqualTo("道具"));

            // 每段 Trim、空段丢弃
            CreatorEntryAttribute.Parse(" a / b / 名 ", out groups, out displayName);
            Assert.That(groups, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(displayName, Is.EqualTo("名"));

            // 空串/null：groups 空、displayName 为 null
            CreatorEntryAttribute.Parse(string.Empty, out groups, out displayName);
            Assert.That(groups, Is.Empty);
            Assert.That(displayName, Is.Null);

            CreatorEntryAttribute.Parse(null, out groups, out displayName);
            Assert.That(groups, Is.Empty);
            Assert.That(displayName, Is.Null);
        }

        [Test]
        public void Scan_CreatorEntry标注_显示名与分组生效()
        {
            // Arrange
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };

            // Act
            var result = ScriptableObjectCreatorScanner.Scan(false, asm);

            // Assert：顶级显示名标注（单段重命名，无分组段，仍属已标注）
            var named = result.FirstOrDefault(e => e.Type.Name == nameof(ScanFakeNamedSo));
            Assert.That(named, Is.Not.Null, "应收录 ScanFakeNamedSo");
            Assert.That(named.DisplayName, Is.EqualTo("道具"), "显示名应取标注值而非类名");
            Assert.That(named.GroupPath, Is.Empty, "单段标注不应产生分组");
            Assert.That(named.IsUngrouped, Is.False, "单段标注（顶级重命名）IsUngrouped 应为 false");

            // Assert：多段分组标注
            var skill = result.FirstOrDefault(e => e.Type.Name == nameof(ScanFakeSkillSo));
            Assert.That(skill, Is.Not.Null, "应收录 ScanFakeSkillSo");
            Assert.That(skill.GroupPath, Is.EqualTo(new[] { "配置", "战斗" }));
            Assert.That(skill.DisplayName, Is.EqualTo("ScanFakeSkillSo"), "显示名应取路径最后一段");
        }

        [Test]
        public void Scan_标注分组与未分组混排_标注在前未分组最后()
        {
            // Arrange
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };

            // Act
            var result = ScriptableObjectCreatorScanner.Scan(false, asm);

            // Assert：有标注分组的 ScanFakeSkillSo 应排在未分组的 ScanFakePlainSo 之前
            var skillIndex = result.FindIndex(e => e.Type.Name == nameof(ScanFakeSkillSo));
            var plainIndex = result.FindIndex(e => e.Type.Name == nameof(ScanFakePlainSo));
            Assert.That(skillIndex, Is.GreaterThanOrEqualTo(0), "ScanFakeSkillSo 应被收录");
            Assert.That(plainIndex, Is.GreaterThanOrEqualTo(0), "ScanFakePlainSo 应被收录");
            Assert.That(skillIndex, Is.LessThan(plainIndex),
                "有标注分组的 ScanFakeSkillSo 应排在未分组的 ScanFakePlainSo 之前");

            // 全局不变式：未分组条目整体排在已标注条目之后（未分组内部按"未分组/程序集名"聚簇）
            var firstUngrouped = result.FindIndex(e => e.IsUngrouped);
            var lastGrouped = result.FindLastIndex(e => !e.IsUngrouped);
            if (firstUngrouped >= 0 && lastGrouped >= 0)
                Assert.That(firstUngrouped, Is.GreaterThan(lastGrouped),
                    "未分组条目应整体排在已标注条目之后");
        }

        [Test]
        public void Filter_关键字_匹配显示名或类名()
        {
            // Arrange：source 取注入扫描结果，与被测集合同源
            var asm = new[] { typeof(ScriptableObjectCreatorScannerTests).Assembly };
            var source = ScriptableObjectCreatorScanner.Scan(false, asm);

            // Act & Assert：按显示名命中
            var byDisplayName = ScriptableObjectCreatorScanner.Filter(source, "道具");
            Assert.That(byDisplayName.Any(e => e.Type.Name == nameof(ScanFakeNamedSo)), Is.True,
                "关键字'道具'应命中显示名");

            // Act & Assert：按类名命中（大小写不敏感）
            var byTypeName = ScriptableObjectCreatorScanner.Filter(source, "FakeSkill");
            Assert.That(byTypeName.Any(e => e.Type.Name == nameof(ScanFakeSkillSo)), Is.True,
                "关键字'FakeSkill'应命中类名");

            // 空关键字返回全量
            Assert.That(ScriptableObjectCreatorScanner.Filter(source, string.Empty).Count,
                Is.EqualTo(source.Count), "空关键字应返回全量");

            // 纯空白关键字等同空串，返回全量
            Assert.That(ScriptableObjectCreatorScanner.Filter(source, "   ").Count,
                Is.EqualTo(source.Count), "纯空白关键字应返回全量");
        }
    }
}

// 命名空间命中排除前缀的 fixture（应被扫描器排除，故置于主测试命名空间之外；
// 程序集仍为本测试程序集，注入扫描可见、默认扫描整层跳过）
namespace UnityFixtures.ScanTests
{
    internal class ScanFakeUnityNsSo : ScriptableObject { }
}
