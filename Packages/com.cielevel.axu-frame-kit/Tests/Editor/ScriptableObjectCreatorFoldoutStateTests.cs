using System.Collections.Generic;
using AxuFrameKit.Common.Editor.Window;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] ScriptableObjectCreatorFoldoutState：EditorPrefs 折叠状态
    /// （收起集合语义）的无记录回退、读写往返、空集清空与损坏 JSON 容错。
    /// 使用带 GUID 后缀的唯一测试键隔离，SetUp/TearDown 双保险删除键，不污染真实窗口的 prefs。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class ScriptableObjectCreatorFoldoutStateTests
    {
        /// <summary>唯一测试键（字段化便于 TearDown 引用；每个测试实例各自随机，天然隔离）。</summary>
        private readonly string _prefsKey = "AxuTest.SOCreatorFoldoutState." + System.Guid.NewGuid().ToString("N");

        [SetUp]
        [TearDown]
        public void DeleteTestPrefsKey()
        {
            EditorPrefs.DeleteKey(_prefsKey);
        }

        [Test]
        public void Load_无记录_返回空集合()
        {
            // Act：键从未写入（SetUp 已兜底删除）
            var result = ScriptableObjectCreatorFoldoutState.Load(_prefsKey);

            // Assert
            Assert.That(result, Is.Not.Null, "返回集合本身不应为 null");
            Assert.That(result, Is.Empty, "无记录时应返回空集合（未记录路径默认展开）");
        }

        [Test]
        public void Save_Load_往返一致()
        {
            // Arrange：含中文段与 "/" 连接的真实形态路径
            var collapsed = new HashSet<string>
            {
                "配置/战斗",
                "未分组/AxuFrameKit.Runtime",
                "阿修工具/中文分组名",
            };

            // Act
            ScriptableObjectCreatorFoldoutState.Save(_prefsKey, collapsed);
            var loaded = ScriptableObjectCreatorFoldoutState.Load(_prefsKey);

            // Assert
            Assert.That(loaded, Is.EquivalentTo(collapsed), "往返后收起集合应一致");
        }

        [Test]
        public void Save_空集合_Load_返回空集合()
        {
            // Act：空集合应清空记录（恢复全展开），而非残留旧数据
            ScriptableObjectCreatorFoldoutState.Save(_prefsKey, new HashSet<string>());
            var loaded = ScriptableObjectCreatorFoldoutState.Load(_prefsKey);

            // Assert
            Assert.That(loaded, Is.Empty);
        }

        [Test]
        public void Load_损坏JSON_回退空集合并告警()
        {
            // Arrange：写入非法 JSON
            EditorPrefs.SetString(_prefsKey, "{not json");

            // 告警文案含不稳定的异常信息后缀，故用正则按消息前缀匹配（LogAssert.Expect 需在日志产生前声明）
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape("[ScriptableObjectCreatorFoldoutState]")));

            // Act
            var result = ScriptableObjectCreatorFoldoutState.Load(_prefsKey);

            // Assert：损坏数据回退空集合，不抛出
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty, "损坏 JSON 应回退为空集合");
        }

        [Test]
        public void Save_null入参_按空集合处理()
        {
            // Act：null 收起集合应视同清空记录
            ScriptableObjectCreatorFoldoutState.Save(_prefsKey, null);
            var loaded = ScriptableObjectCreatorFoldoutState.Load(_prefsKey);

            // Assert
            Assert.That(loaded, Is.Empty);
        }
    }
}
