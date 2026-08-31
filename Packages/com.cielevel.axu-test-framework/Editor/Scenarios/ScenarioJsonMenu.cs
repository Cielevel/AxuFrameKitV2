using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// Scenario / Suite JSON 模板创建菜单（方案 §3.2：JSON 为权威载体，<see cref="ScenariosFolder"/>
    /// 为工程约定目录）。模板结构与包内 <c>DOC/ScenarioSchema.md</c>（axu-scenario/1、axu-suite/1）一致；
    /// Scenario 模板用 Agent 内建 <c>echo</c> 能力（仅延时 + 日志，params 键 message/delaySeconds/steps
    /// 为与运行时侧的契约）。JSON 不支持注释，说明文字由 title 字段承载。
    /// <para>目录经 <c>AssetDatabase.CreateFolder</c> 逐级创建；文件名带 HHmmss 时间戳，重名自动加序号；
    /// 写入后 <c>AssetDatabase.ImportAsset</c> 入库并 <c>EditorUtility.RevealInFinder</c> 定位。
    /// JSON 正文按 DOC 约定写 UTF-8 无 BOM。</para>
    /// </summary>
    public static class ScenarioJsonMenu
    {
        /// <summary>创建 Scenario 模板菜单。</summary>
        public const string ScenarioMenuPath = "AxuTestFramework/Create Scenario JSON";

        /// <summary>创建 Suite 模板菜单。</summary>
        public const string SuiteMenuPath = "AxuTestFramework/Create Suite JSON";

        /// <summary>工程约定场景目录（方案 §3.2，Editor 侧扫描落点）。</summary>
        public const string ScenariosFolder = "Assets/Tests/Scenarios";

        private const string ScenarioFilePrefix = "scenario";
        private const string SuiteFilePrefix = "suite";

        [MenuItem(ScenarioMenuPath, false)]
        public static void CreateScenarioJson()
        {
            CreateTemplate(ScenarioFilePrefix, ScenarioTemplate);
        }

        [MenuItem(SuiteMenuPath, false)]
        public static void CreateSuiteJson()
        {
            CreateTemplate(SuiteFilePrefix, SuiteTemplate);
        }

        // ------------------------------------------------------------------ 模板正文（与 DOC/ScenarioSchema.md 同步维护）

        /// <summary>axu-scenario/1 最小可跑模板：echo 内建能力 + 空 budgets（结果将标 unbudgeted=true）。</summary>
        private const string ScenarioTemplate =
@"{
  ""schema"": ""axu-scenario/1"",
  ""id"": ""echo.demo"",
  ""capability"": ""echo"",
  ""enabled"": true,
  ""title"": ""echo 冒烟模板：echo 为 Agent 内建能力（仅延时+日志），params 支持 message / delaySeconds / steps；budgets 留空时结果标 unbudgeted=true，可按包内 DOC/ScenarioSchema.md 补充 metric/op/value 规则"",
  ""params"": {
    ""message"": ""hello from scenario"",
    ""delaySeconds"": 1.0,
    ""steps"": 3
  },
  ""budgets"": []
}";

        /// <summary>axu-suite/1 最小模板：字符串项引用既有场景 id + 内联完整 scenario + profiles/settings 示例。</summary>
        private const string SuiteTemplate =
@"{
  ""schema"": ""axu-suite/1"",
  ""id"": ""echo.demo.suite"",
  ""title"": ""echo 冒烟套件模板：scenarios 字符串项引用已存在的场景 id（echo.demo，可先经 AxuTestFramework/Create Scenario JSON 生成），对象项为内联完整 scenario；profiles 为按名引用的环境档（对 scenario.params 深合并覆盖），scenario 项带 profile 字段即可套用 fast 档；settings 三键为默认值"",
  ""scenarios"": [
    ""echo.demo"",
    {
      ""schema"": ""axu-scenario/1"",
      ""id"": ""echo.demo.inline"",
      ""capability"": ""echo"",
      ""enabled"": true,
      ""title"": ""内联完整对象形态示例：结构与 axu-scenario/1 相同，嵌在 suite 内直接可跑"",
      ""params"": {
        ""message"": ""hello from suite"",
        ""delaySeconds"": 1.0,
        ""steps"": 3
      },
      ""budgets"": []
    }
  ],
  ""profiles"": {
    ""fast"": { ""delaySeconds"": 0.2 }
  },
  ""settings"": {
    ""stopOnFailure"": false,
    ""warnAsFail"": false,
    ""scenarioTimeoutSec"": 600
  }
}";

        // ------------------------------------------------------------------ 落盘

        /// <summary>在 <see cref="ScenariosFolder"/> 下以 <c>{prefix}_{HHmmss}.json</c> 写入模板
        /// （重名自动加序号），ImportAsset 入库后 RevealInFinder 定位。</summary>
        private static void CreateTemplate(string filePrefix, string json)
        {
            EnsureFolder(ScenariosFolder);

            var assetPath = GetUniqueAssetPath(
                $"{ScenariosFolder}/{filePrefix}_{DateTime.Now:HHmmss}.json");

            var fullPath = ToProjectPath(assetPath);
            File.WriteAllText(fullPath, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            AssetDatabase.ImportAsset(assetPath);
            EditorUtility.RevealInFinder(fullPath);

            Debug.Log($"[AxuTestFramework] 模板已创建: {assetPath}");
        }

        /// <summary>逐级创建资产目录（仅经 AssetDatabase API；"Assets" 根恒存在，作递归终点）。</summary>
        private static void EnsureFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            var parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetFolderPath);
            EnsureFolder(parent);

            if (!AssetDatabase.IsValidFolder(assetFolderPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>目标文件已存在时在扩展名前追加 _1.._999 序号（HHmmss 只到秒级，同秒连点防覆盖）。</summary>
        private static string GetUniqueAssetPath(string assetPath)
        {
            if (!File.Exists(ToProjectPath(assetPath)))
                return assetPath;

            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileNameWithoutExtension(assetPath);
            var extension = Path.GetExtension(assetPath);

            for (var i = 1; i < 1000; i++)
            {
                var candidate = $"{directory}/{name}_{i}{extension}";
                if (!File.Exists(ToProjectPath(candidate)))
                    return candidate;
            }

            Debug.LogWarning($"[AxuTestFramework] 无法为 {assetPath} 找到空闲文件名（重名序号已用尽）。");
            return assetPath;
        }

        /// <summary>Assets/ 相对路径 → 工程绝对路径（工程根经 Application.dataPath 推导，不依赖进程 cwd）。</summary>
        private static string ToProjectPath(string assetRelativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
