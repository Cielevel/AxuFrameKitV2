using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Editor/MissingScript] MissingScriptScanner：Missing Script 扫描检出 / 清扫落盘 /
    /// 范围过滤 / 会话缓存。
    /// 正例资产 = 常驻夹具方案（第四轮，弃一切运行时构造）：旧方案"空 GO 挂 GoAutoDestroy →
    /// SaveAsPrefabAsset → SerializedObject 置空 m_Script → 存档"已被实测证伪——
    /// Unity 6000.0.23f1 引擎拒绝保存含缺失脚本的 prefab（错误原文
    /// "You are trying to save a Prefab with a missing script. This is not allowed."），
    /// 更早轮次的手写/注入 YAML 路径也被导入器剥离槽位，运行时构造全部走不通。
    /// <para>常驻夹具（由编排方另行制作并提交入库，测试不负责生成）：
    /// Tests/Editor/Fixtures/MissingScriptFixture.prefab——在 Unity 外以文本方式把 prefab 内
    /// 一个 MonoBehaviour（原为 GoAutoDestroy）的 m_Script GUID 替换为无效 GUID，即真实工程
    /// "脚本被删后残留"的 stale-GUID 形态，重导入后为 fake-null 缺失脚本槽位；
    /// 根物体 MissingScriptFixtureRoot：Transform + 1 个缺失脚本槽位，无其他组件。</para>
    /// <para>夹具只读、测试不直接使用本体：每个用例先 File.Copy 到
    /// Assets/Temp_MissingSweeperTest/ 临时目录再 ImportAsset 后使用（复制资产重导入会生成
    /// 新 meta，属正常）。理由：夹具需入库常驻复用，不能被用例改写/删除；清扫等写操作只落在
    /// 副本上，Assets/ 临时目录逐用例由 TearDown 连同资产/元文件删除；Packages/ 下的夹具本体
    /// 也永远不会被扫描器扫到（扫描器对非 Assets 输入整体过滤，见范围过滤用例）。</para>
    /// 副本导入后做 <see cref="PrefabHasMissingSlot"/> 二分断言（与扫描器同一 fake-null 判定，
    /// 区分夹具问题与扫描器问题）。落盘断言以 stale GUID 为标记（stale-GUID 形态下 m_Script
    /// 不会是 {fileID: 0}，勿以 fileID-0 作缺失标记）。
    /// 正常对照 prefab 用空 GameObject 直接 SaveAsPrefabAsset；干净场景用手写最小 .unity YAML
    /// （NewScene(Additive) 在自动化 Editor 中被"未保存未命名场景"前置校验拦截，
    /// 见 CreateCleanSceneAsset 注释，已实跑 Pass）。
    /// 已知噪音：加载含缺失脚本槽位的资产时 Unity 会输出 Missing Script 告警，属预期，
    /// fixture 内 ignoreFailingMessages 放行（仅本 fixture 生效，TearDown 恢复）。
    /// </summary>
    [Category("Axu/Editor/MissingScript")]
    public class MissingScriptScannerTests
    {
        private const string TempRoot = "Assets/Temp_MissingSweeperTest";
        private const string OtherFolder = TempRoot + "/Other";
        // 副本与夹具同名：Unity 6 强制 prefab 根物体名=资产文件名，同名可保证根名恒等于 FixtureRootName
        private const string MissingPrefabPath = TempRoot + "/MissingScriptFixture.prefab";
        private const string CleanPrefabPath = TempRoot + "/CleanPrefab.prefab";
        private const string CleanScenePath = TempRoot + "/CleanScene.unity";

        /// <summary>
        /// 常驻夹具（工程相对路径，位于包内 Tests 目录，入库只读；测试内禁止改写该文件）。
        /// </summary>
        private const string FixtureSourcePath =
            "Packages/com.cielevel.axu-frame-kit/Tests/Editor/Fixtures/MissingScriptFixture.prefab";

        /// <summary>夹具根物体名（缺失脚本槽位所在 GO；Unity 6 强制 prefab 根名=资产文件名，故为 MissingScriptFixture）。</summary>
        private const string FixtureRootName = "MissingScriptFixture";

        [SetUp]
        public void SetUp()
        {
            // 缓存是扫描器的静态会话状态，逐用例清空保证隔离
            MissingScriptScanner.ClearCache();

            // 含缺失脚本槽位的资产在导入/加载时会产生预期内的 Missing Script 日志，放行避免误判
            LogAssert.ignoreFailingMessages = true;

            if (!AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.CreateFolder("Assets", "Temp_MissingSweeperTest");
            if (!AssetDatabase.IsValidFolder(OtherFolder))
                AssetDatabase.CreateFolder(TempRoot, "Other");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            MissingScriptScanner.ClearCache();

            // 临时目录连同资产/元文件一并删除，再刷新保持状态干净
            if (AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.DeleteAsset(TempRoot);
            AssetDatabase.Refresh();
        }

        // ───────────────────── 用例 ─────────────────────

        [Test]
        public void Scan_含缺失脚本槽位的Prefab_检出一条可移除Entry_正常Prefab不误报()
        {
            // Arrange（装载时已做二分诊断：副本导入后必须存在 fake-null 槽位，否则在此先行报夹具问题）
            CreateTempPrefabFromFixture();
            CreateCleanPrefab();

            // Act
            var reports = MissingScriptScanner.Scan(new[] { TempRoot }, true, false, out var summary);

            // Assert：先断言扫描覆盖面（区分"目标未被收集/被缓存跳过"与"检出失败"）
            Assert.That(summary.ScannedCount, Is.EqualTo(2), "两个 prefab 均应被 FindAssets 收集并实际扫描");
            Assert.That(summary.CacheHitCount, Is.EqualTo(0), "首扫不应命中缓存");

            // Assert：缺脚本副本恰好 1 条 Entry（根物体、归属本资产、可移除）
            var missing = reports.FirstOrDefault(r => r.AssetPath == MissingPrefabPath);
            Assert.That(missing, Is.Not.Null,
                "缺脚本副本应被检出（装载已诊断存在槽位；若此断言失败需查扫描器路径）");
            Assert.That(missing.AssetType, Is.EqualTo(MissingScriptScanner.TypePrefab));
            Assert.That(missing.Entries.Count, Is.EqualTo(1), "夹具副本仅含 1 个缺失脚本组件（根物体 Transform + 1 个缺失槽位）");

            var entry = missing.Entries[0];
            Assert.That(entry.Removable, Is.True, "根物体上的缺失组件应归属本资产（可移除）");
            Assert.That(entry.GameObjectPath, Is.EqualTo(FixtureRootName), "根物体层级路径应为夹具约定的根物体名");
            Assert.That(entry.ComponentIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(entry.OwningAssetPath, Is.EqualTo(MissingPrefabPath), "可移除条目归属当前资产");

            // Assert：正常 prefab 不误报
            Assert.That(reports.Any(r => r.AssetPath == CleanPrefabPath), Is.False, "正常 prefab 不应被检出");
        }

        [Test]
        public void RemoveMissingScripts_清扫可移除缺失组件_重扫归零且落盘staleGUID被清除()
        {
            // Arrange（装载时已做二分诊断，保证前置的"检出 1 个"不是夹具空转）
            CreateTempPrefabFromFixture();
            var before = MissingScriptScanner.Scan(new[] { TempRoot }, true, false, out _);
            Assert.That(before.Count, Is.EqualTo(1), "前置：应检出 1 个缺脚本资产");

            // 前置（落盘证据）：从夹具本体提取 m_Script 的 stale GUID，并确认其在工程内无对应资产（确系无效 GUID）
            var staleGuid = ExtractStaleScriptGuid(File.ReadAllText(ToAbsolutePath(FixtureSourcePath)));
            Assert.That(staleGuid, Is.Not.Null.And.Not.Empty,
                "前置：常驻夹具应含 m_Script 的 stale GUID（夹具制作未生效或形态不符约定）");
            Assert.That(AssetDatabase.GUIDToAssetPath(staleGuid), Is.Empty,
                "前置：夹具 m_Script GUID 应在工程内无对应资产（stale-GUID 形态约定）");

            // 前置（落盘证据）：清扫前副本文件应含夹具同款 stale-GUID 标记
            var textBefore = File.ReadAllText(ToAbsolutePath(MissingPrefabPath));
            Assert.That(textBefore.Contains(staleGuid), Is.True,
                "前置：缺脚本副本落盘文件应含夹具的 stale-GUID 标记");

            // Act（清缓存确保重扫反映真实落盘状态）
            MissingScriptScanner.ClearCache();
            var removeSummary = MissingScriptScanner.RemoveMissingScripts(before);
            var after = MissingScriptScanner.Scan(new[] { TempRoot }, true, false, out _);

            // Assert：统计与重扫结果
            Assert.That(removeSummary.ProcessedAssetCount, Is.EqualTo(1), "应处理 1 个资产");
            Assert.That(removeSummary.RemovedCount, Is.EqualTo(1), "应移除 1 处缺失组件");
            Assert.That(removeSummary.SkippedCount, Is.EqualTo(0));
            Assert.That(after.Any(r => r.AssetPath == MissingPrefabPath), Is.False, "清扫后重扫不应再检出");

            // Assert：清扫后副本已无缺失组件（SaveAsPrefabAsset 落盘合法的前提），
            // 落盘文件不再含夹具的 stale-GUID 标记（缺失组件已随清扫移除）
            Assert.That(PrefabHasMissingSlot(MissingPrefabPath), Is.False,
                "清扫后副本不应再含缺失脚本槽位（SaveAsPrefabAsset 落盘合法的前提）");
            var textAfter = File.ReadAllText(ToAbsolutePath(MissingPrefabPath));
            Assert.That(textAfter.Contains(staleGuid), Is.False,
                "落盘文件不应再含夹具的 stale-GUID 标记（缺失组件已随清扫移除）");
        }

        [Test]
        public void Scan_范围不含目标资产_结果为空()
        {
            // Arrange：副本位于 TempRoot（Assets 子目录 A），扫描范围为其子目录 Other（B，不含任何资产）
            CreateTempPrefabFromFixture();

            // Act：子目录 Other 不含任何资产
            var reports = MissingScriptScanner.Scan(new[] { OtherFolder }, true, true, out var summary);

            // Assert
            Assert.That(reports, Is.Empty, "不含副本的目录扫描不应有结果");
            Assert.That(summary.ScannedCount, Is.EqualTo(0));

            // Act2：Assets 外输入（Packages/）应被过滤，不产生任何扫描；
            // 注意常驻夹具本体就在 Packages/ 下，同样依赖此过滤保证永不被扫到
            var packagesScan = MissingScriptScanner.Scan(new[] { "Packages" }, true, true, out var packagesSummary);

            // Assert2
            Assert.That(packagesScan, Is.Empty, "Packages/ 输入应被过滤不扫描");
            Assert.That(packagesSummary.ScannedCount, Is.EqualTo(0), "Packages/ 输入不应实际扫描任何资产");
        }

        [Test]
        public void Scan_干净场景资产_零误报()
        {
            // 取舍说明：场景"正例"（含缺失脚本的 .unity YAML）组装不稳定，按方案仅做干净场景负例；
            // 场景与 Prefab 共用同一 WalkHierarchy / DetectMissingComponents 检测核心，正例由 prefab 用例覆盖。
            // 本用例覆盖"未打开场景 → Additive 打开 → 遍历 → 关闭（不保存）"的扫描路径，
            // 场景以手写最小 .unity YAML 构造（NewScene 被自动化 Editor 拦截，取舍见 CreateCleanSceneAsset 注释）。
            // Arrange
            CreateCleanSceneAsset();

            // Act
            var reports = MissingScriptScanner.Scan(new[] { TempRoot }, false, true, out var summary);

            // Assert
            Assert.That(reports, Is.Empty, "干净场景不应被检出");
            Assert.That(summary.ScannedCount, Is.EqualTo(1), "场景资产应实际扫描");
        }

        [Test]
        public void Scan_同Hash且上次干净_第二次命中缓存跳过()
        {
            // Arrange
            CreateCleanPrefab();
            MissingScriptScanner.Scan(new[] { TempRoot }, true, false, out var firstSummary);
            Assert.That(firstSummary.ScannedCount, Is.EqualTo(1), "第一次应实际扫描");
            Assert.That(firstSummary.CacheHitCount, Is.EqualTo(0));

            // Act：资产未变（hash 相同）且上次结论为"干净"
            var second = MissingScriptScanner.Scan(new[] { TempRoot }, true, false, out var secondSummary);

            // Assert
            Assert.That(secondSummary.CacheHitCount, Is.EqualTo(1), "hash 未变且上次干净应命中缓存");
            Assert.That(secondSummary.ScannedCount, Is.EqualTo(0), "命中缓存不应实际扫描");
            Assert.That(second, Is.Empty);
        }

        // ───────────────────── 夹具装载与二分诊断：缺脚本 prefab 副本 ─────────────────────

        /// <summary>
        /// 装载常驻夹具副本（各正例用例的缺脚本资产来源）：
        /// 1) File.Copy 夹具（<see cref="FixtureSourcePath"/>，入库只读）→ 临时目录覆盖写副本；
        /// 2) AssetDatabase.ImportAsset 让 Unity 以自身管线导入副本（复制重导入生成新 meta 属正常）；
        /// 3) 加载断言（副本可加载为 GameObject 资产）；
        /// 4) <see cref="PrefabHasMissingSlot"/> 二分断言（与扫描器同一 fake-null 判定，
        ///    区分夹具问题与扫描器问题）。
        /// 全部写操作只落在 Assets/ 临时目录的副本上，夹具本体禁止改写。
        /// </summary>
        private static void CreateTempPrefabFromFixture()
        {
            var sourceAbs = ToAbsolutePath(FixtureSourcePath);
            Assert.That(File.Exists(sourceAbs), Is.True,
                "常驻夹具缺失: " + FixtureSourcePath + "（由编排方制作并提交入库，测试不负责生成）");

            var destAbs = ToAbsolutePath(MissingPrefabPath);
            File.Copy(sourceAbs, destAbs, true);
            AssetDatabase.ImportAsset(MissingPrefabPath);

            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(MissingPrefabPath);
            Assert.That(loaded, Is.Not.Null, "夹具诊断：缺失脚本夹具副本导入后加载失败");

            Assert.That(PrefabHasMissingSlot(MissingPrefabPath), Is.True,
                "常驻夹具缺失槽位在副本导入后未存活——Unity 6000.0.23f1 对 stale-GUID 导入做剥离，检测正例无法自动化覆盖");
        }

        /// <summary>
        /// 从 prefab YAML 文本提取 m_Script 引用的 GUID（stale-GUID 落盘标记）。
        /// 夹具约定：prefab 内除该 m_Script 外无其他 GUID 引用（无外部资源引用字段），
        /// 故该 GUID 可作为清扫前后落盘内容的标记使用。
        /// </summary>
        private static string ExtractStaleScriptGuid(string prefabYaml)
        {
            var match = Regex.Match(prefabYaml, @"m_Script:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})");
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// 二分诊断核心：加载 prefab 内容并按与扫描器完全相同的判定检查缺失槽位是否存在
        /// （同一检测逻辑，故"夹具检查通过 + Scan 未检出"才可能指向扫描器）。
        /// 判定口径：GetComponents 结果中的 null 条目（Unity 6000.0.23f1 实测为真 null，非 fake-null）。
        /// </summary>
        private static bool PrefabHasMissingSlot(string path)
        {
            try
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var comps = root.GetComponents<Component>();
                    return comps.Any(c => c == null);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MissingScriptScannerTests] 槽位检查加载失败（按未生成处理）: " + ex.Message);
                return false;
            }
        }

        // ───────────────────── 测试资产构造：对照资产 ─────────────────────

        /// <summary>正常对照 prefab：空 GameObject 直接 SaveAsPrefabAsset（无缺失脚本）。</summary>
        private static void CreateCleanPrefab()
        {
            var go = new GameObject("CleanRoot");
            PrefabUtility.SaveAsPrefabAsset(go, CleanPrefabPath);
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// 干净场景资产：手写最小 .unity YAML（GameObject + Transform + SceneRoots）+ ImportAsset。
        /// 取舍说明（第二轮已定、实跑 Pass，本轮保留）：
        /// - 原 NewScene(EmptyScene, Additive) 在自动化 Editor 中被前置校验拦截
        ///   （"Cannot create a new scene additively with an untitled scene unsaved"，当前为未保存未命名场景）；
        /// - NewSceneMode.Single 虽无该拦截，但会替换测试会话当前打开的场景且 TearDown 不恢复，副作用更大；
        /// - 故与 prefab 同款"手写最小 YAML + ImportAsset"：缺省管理器块（RenderSettings 等）
        ///   由 Unity 场景加载器按默认值补齐。
        /// 构造后做二分诊断：场景必须能 Additive 打开且恰好 1 个根物体，
        /// 否则负例"通过"可能只是"场景没加载"而非"干净"。
        /// </summary>
        private static void CreateCleanSceneAsset()
        {
            var yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &2000000001
GameObject:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Component:
  - component: {fileID: 2000000002}
  m_Layer: 0
  m_Name: CleanSceneRoot
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2000000002
Transform:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2000000001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!287349306 &2000000003
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots: [{fileID: 2000000001}]
";
            File.WriteAllText(ToAbsolutePath(CleanScenePath), yaml);
            AssetDatabase.ImportAsset(CleanScenePath);

            var loaded = AssetDatabase.LoadAssetAtPath<SceneAsset>(CleanScenePath);
            Assert.That(loaded, Is.Not.Null, "夹具诊断：手写场景 YAML 导入失败");

            // 二分诊断：场景必须能 Additive 打开且恰好 1 个根物体
            var scene = EditorSceneManager.OpenScene(CleanScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Length, Is.EqualTo(1), "夹具诊断：手写场景应含 1 个根物体");
                Assert.That(roots[0].name, Is.EqualTo("CleanSceneRoot"), "夹具诊断：场景根物体名应为 CleanSceneRoot");
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>工程相对路径 → 绝对路径（基于 Application.dataPath 上级，不依赖进程工作目录）。</summary>
        private static string ToAbsolutePath(string projectRelativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
