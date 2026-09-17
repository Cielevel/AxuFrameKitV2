using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.UtilityGroup;
using NUnit.Framework;
using UnityEditor;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Editor/SceneSets] SceneSetPresetStore：多场景组合预设的 JSON 落盘往返、
    /// 损坏自愈（保留 .corrupt 后空档重建，仅默认路径弹窗告知，临时注入路径不弹窗）、
    /// 存在性校验与同名覆盖。
    /// 全部走临时 path 注入（系统 Temp 下随机目录，SetUp 建 / TearDown 整目录删），
    /// 不打开/保存任何真实场景；"存在场景"用例直接复用工程内已有 SceneAsset
    /// （AssetDatabase.FindAssets 检索现成 .unity）——不经手写场景 YAML、不 ImportAsset、
    /// 不打开场景，选实现最简且不触发重新导入/重编译的方案。
    /// </summary>
    [Category("Axu/Editor/SceneSets")]
    public class SceneSetPresetStoreTests
    {
        private string _tempDir;
        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "AxuSceneSetPresetStoreTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _tempPath = Path.Combine(_tempDir, "presets.json");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 清理失败不影响测试结论（临时目录在系统 Temp 下，可留待系统清理）
            }
        }

        private static SceneSetPreset MakePreset(string name, params string[] paths)
        {
            return new SceneSetPreset { Name = name, ScenePaths = paths };
        }

        // ───────────────────── 用例 ─────────────────────

        [Test]
        public void 存取_两个预设往返_名称与路径数组逐项相等()
        {
            // Arrange：仅虚构路径即可，落盘往返不校验场景存在性
            var presets = new List<SceneSetPreset>
            {
                MakePreset("主城+关卡+UI", "Assets/A.unity", "Assets/B.unity", "Assets/UI.unity"),
                MakePreset("战斗", "Assets/Battle.unity")
            };

            // Act
            SceneSetPresetStore.SavePresets(presets, _tempPath);
            var loaded = SceneSetPresetStore.LoadPresets(_tempPath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Count, Is.EqualTo(2), "应完整往返 2 个预设");
            for (int i = 0; i < presets.Count; i++)
            {
                Assert.That(loaded[i].Name, Is.EqualTo(presets[i].Name));
                Assert.That(loaded[i].ScenePaths, Is.EqualTo(presets[i].ScenePaths),
                    "路径数组应逐项相等（含顺序，第一个即激活场景）");
            }
        }

        [Test]
        public void Load_文件损坏_返回空列表不抛异常并保留corrupt()
        {
            // Arrange：写入非法 JSON
            File.WriteAllText(_tempPath, "{oops");

            // Act & Assert：不抛异常，返回空列表（从空档重建）
            List<SceneSetPreset> loaded = null;
            Assert.DoesNotThrow(() => loaded = SceneSetPresetStore.LoadPresets(_tempPath));
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded, Is.Empty);

            // 损坏文件应被改名 .corrupt 保留（原文件移走）；临时注入路径不弹窗（弹窗仅限默认路径）
            Assert.That(File.Exists(_tempPath + ".corrupt"), Is.True, "损坏文件应保留为 .corrupt");
            Assert.That(File.Exists(_tempPath), Is.False, "原损坏文件应已被移走");
        }

        [Test]
        public void Load_文件不存在_返回空列表且不产生corrupt()
        {
            // Act
            var loaded = SceneSetPresetStore.LoadPresets(_tempPath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded, Is.Empty);
            Assert.That(File.Exists(_tempPath + ".corrupt"), Is.False, "文件本就不存在时不应产生 .corrupt");
        }

        [Test]
        public void Validate_路径不存在_正确报缺失()
        {
            // Arrange
            var preset = MakePreset("幽灵组合", "Assets/Definitely_Not_Exist.unity");

            // Act
            var missing = SceneSetPresetStore.ValidatePreset(preset);

            // Assert
            Assert.That(missing, Is.EqualTo(new[] { "Assets/Definitely_Not_Exist.unity" }));
        }

        [Test]
        public void Validate_混合路径_仅报缺失不误报存在场景()
        {
            // Arrange："存在"场景直接复用工程内已有 SceneAsset（选择理由见类头注释）
            var guid = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" }).FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                Assert.Ignore("工程内无 SceneAsset 资产，跳过存在性校验用例");
            var existingPath = AssetDatabase.GUIDToAssetPath(guid);

            var preset = MakePreset("混合组合", existingPath, "Assets/Definitely_Not_Exist.unity");

            // Act
            var missing = SceneSetPresetStore.ValidatePreset(preset);

            // Assert：存在的场景不应误报，缺失的必须报出
            Assert.That(missing, Is.EqualTo(new[] { "Assets/Definitely_Not_Exist.unity" }),
                "存在的场景不应误报，缺失的必须报出");
        }

        [Test]
        public void Save_覆盖同名Name_列表正确更新()
        {
            // Arrange：[更新为当前] 的底层等价逻辑 —— 读出列表、把同名 Name 的预设
            // 整体替换为新的路径数组（模拟 CaptureCurrentSetup 的产物）、再整表落盘
            var first = new List<SceneSetPreset>
            {
                MakePreset("组合A", "Assets/Old1.unity", "Assets/Old2.unity"),
                MakePreset("组合B", "Assets/B.unity")
            };
            SceneSetPresetStore.SavePresets(first, _tempPath);

            // Act
            var loaded = SceneSetPresetStore.LoadPresets(_tempPath);
            var index = loaded.FindIndex(p => p.Name == "组合A");
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "前置：组合A 应已存在");
            loaded[index] = MakePreset("组合A", "Assets/New1.unity", "Assets/New2.unity", "Assets/New3.unity");
            SceneSetPresetStore.SavePresets(loaded, _tempPath);

            // Assert
            var reloaded = SceneSetPresetStore.LoadPresets(_tempPath);
            Assert.That(reloaded.Count, Is.EqualTo(2), "覆盖同名不应新增条目");
            var a = reloaded.FirstOrDefault(p => p.Name == "组合A");
            Assert.That(a, Is.Not.Null);
            Assert.That(a.ScenePaths, Is.EqualTo(new[] { "Assets/New1.unity", "Assets/New2.unity", "Assets/New3.unity" }));
            Assert.That(reloaded.First(p => p.Name == "组合B").ScenePaths,
                Is.EqualTo(new[] { "Assets/B.unity" }), "无关预设不应受覆盖影响");
        }

        [Test]
        public void Capture_EditMode直接调用_不抛异常且返回有效对象()
        {
            // EditMode 下它返回当前打开场景集合（测试不打开/保存任何场景，
            // 也不假设工程当前打开了什么），只验证可安全调用、返回对象完整
            SceneSetPreset captured = null;
            Assert.DoesNotThrow(() => captured = SceneSetPresetStore.CaptureCurrentSetup());
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.ScenePaths, Is.Not.Null);
        }
    }
}
