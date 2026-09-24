using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 首次运行引导的目标清单与 suite JSON 供给：
    /// <see cref="SelfCheckSuiteJson"/> 是内嵌的"工具链自检"套件（单 echo 内联 scenario，仅延时+日志，
    /// 无 budgets、无 baseline、无场景依赖——任何工程导入即可跑，跑通与否只取决于框架自身），
    /// 经 <see cref="EnsureSelfCheckSuiteFile"/> 幂等落盘 Temp 后交给
    /// <see cref="ScenarioRunService.StartEditorPlayRun"/>；scene/perf 目标经 <see cref="BuildGoalSuiteJson"/>
    /// 现拼 JSON，由 <see cref="TryCreateGoalSuite"/> 按 <see cref="ScenarioJsonMenu"/> 同款规则落 Assets
    /// （AssetDatabase.CreateFolder 逐级建目录 + 重名加序号 + ImportAsset 入库）。
    /// <para>自检套件保持"纯工具链语义"：不带预算与基线，通过与否不依赖项目内容；
    /// 引导目标产生的套件同样不带预算（引导只证明"能跑"，调参/定预算是后续使用的事）。</para>
    /// </summary>
    public static class FirstRunSuiteCatalog
    {
        /// <summary>自检套件 id（看板经 <see cref="ScenarioRunStatusPresenter.ScopePrefix"/> 识别并标注"示例自检"）。</summary>
        public const string SelfCheckSuiteId = "axu-selfcheck-echo";

        /// <summary>自检套件文件（相对工程根；Temp 为 Unity 会话级目录，不进版本库，用完即弃）。</summary>
        public const string SelfCheckSuiteTempPath = "Temp/axu_selfcheck_echo.suite.json";

        /// <summary>goal 套件默认落盘目录（工程约定场景目录，与 JSON 模板菜单一致）。</summary>
        public const string DefaultGoalFolder = ScenarioJsonMenu.ScenariosFolder;

        const string GoalIdSelfCheck = "selfcheck";
        const string GoalIdCode = "code";
        const string GoalIdScene = "scene";
        const string GoalIdPerf = "perf";

        /// <summary>引导目标清单（顺序即界面展示顺序；id 勿改）。</summary>
        public static IReadOnlyList<FirstRunGoal> Goals { get; } = new List<FirstRunGoal>
        {
            new FirstRunGoal
            {
                Id = GoalIdSelfCheck,
                Title = "示例自检",
                Description = "验证测试工具链是否可用（示例，不依赖项目内容）",
                NeedsAgentDefine = true,
                NeedsBuildScenes = false,
            },
            new FirstRunGoal
            {
                Id = GoalIdCode,
                Title = "代码单元测试",
                Description = "运行 EditMode/PlayMode 单元测试，检查代码功能",
                NeedsAgentDefine = false,
                NeedsBuildScenes = false,
            },
            new FirstRunGoal
            {
                Id = GoalIdScene,
                Title = "场景冒烟",
                Description = "运行 smoke 场景测试，检查场景能否正常加载运行",
                NeedsAgentDefine = true,
                NeedsBuildScenes = true,
            },
            new FirstRunGoal
            {
                Id = GoalIdPerf,
                Title = "性能采样",
                Description = "采集当前场景性能数据，供多次运行对比趋势",
                NeedsAgentDefine = true,
                NeedsBuildScenes = false,
            },
        };

        /// <summary>
        /// 内嵌自检套件（axu-suite/1）：单 echo 内联 scenario，仅延时+日志，
        /// 无 budgets、无 baseline、无场景依赖；结构必须能过
        /// <see cref="ScenarioRunService.TryPrevalidateSuiteFile"/> 的真实校验（有单测守护）。
        /// </summary>
        public static string SelfCheckSuiteJson { get; } =
@"{
  ""schema"": ""axu-suite/1"",
  ""id"": ""axu-selfcheck-echo"",
  ""title"": ""工具链自检（AxuTestFramework 内置示例，不依赖项目内容）"",
  ""scenarios"": [
    {
      ""schema"": ""axu-scenario/1"",
      ""id"": ""axu.selfcheck.echo"",
      ""capability"": ""echo"",
      ""enabled"": true,
      ""title"": ""自检步骤：仅延时与日志输出"",
      ""params"": {
        ""message"": ""AxuTestFramework self-check"",
        ""delaySeconds"": 0.5,
        ""steps"": 2
      }
    }
  ],
  ""settings"": {
    ""stopOnFailure"": false,
    ""warnAsFail"": false,
    ""scenarioTimeoutSec"": 120
  }
}";

        /// <summary>scene 目标套件：smoke 能力遍历构建场景清单（source=buildSettings）。</summary>
        const string SceneGoalSuiteJson =
@"{
  ""schema"": ""axu-suite/1"",
  ""id"": ""axu-goal-scene-smoke"",
  ""title"": ""场景冒烟检查（遍历构建场景清单：能加载、无报错）"",
  ""scenarios"": [
    {
      ""schema"": ""axu-scenario/1"",
      ""id"": ""axu.goal.scene.smoke"",
      ""capability"": ""smoke"",
      ""enabled"": true,
      ""title"": ""构建场景冒烟矩阵"",
      ""params"": {
        ""source"": ""buildSettings"",
        ""stabilitySeconds"": 1,
        ""stabilityFrames"": 10,
        ""screenshot"": true
      }
    }
  ],
  ""settings"": {
    ""stopOnFailure"": false,
    ""warnAsFail"": false,
    ""scenarioTimeoutSec"": 600
  }
}";

        /// <summary>perf 目标套件：perf 能力采样当前场景（warmup 1s + measure 5s，无预算）。</summary>
        const string PerfGoalSuiteJson =
@"{
  ""schema"": ""axu-suite/1"",
  ""id"": ""axu-goal-perf"",
  ""title"": ""当前场景性能采样（warmup 1 秒 + 度量 5 秒，无预算，供多次运行对比）"",
  ""scenarios"": [
    {
      ""schema"": ""axu-scenario/1"",
      ""id"": ""axu.goal.perf"",
      ""capability"": ""perf"",
      ""enabled"": true,
      ""title"": ""当前场景性能采样（warmup 1s / measure 5s）"",
      ""params"": {
        ""warmupSeconds"": 1,
        ""measureSeconds"": 5,
        ""screenshot"": true
      }
    }
  ],
  ""settings"": {
    ""stopOnFailure"": false,
    ""warnAsFail"": false,
    ""scenarioTimeoutSec"": 120
  }
}";

        /// <summary>
        /// 按引导目标拼 suite JSON："scene" → smoke(source:buildSettings)；"perf" → perf 采样当前场景
        /// （warmup 1s + measure 5s，无预算）；其余（含 selfcheck/code——它们不需要套件文件）返回 null。
        /// </summary>
        public static string BuildGoalSuiteJson(string goalId)
        {
            switch ((goalId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case GoalIdScene:
                    return SceneGoalSuiteJson;
                case GoalIdPerf:
                    return PerfGoalSuiteJson;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 幂等写自检套件文件（<see cref="SelfCheckSuiteTempPath"/>，目录随建）：内容一致时不重写，
        /// 避免无谓的时间戳抖动；返回文件绝对路径。写失败时 <paramref name="error"/> 非 null 并返回 null。
        /// </summary>
        public static string EnsureSelfCheckSuiteFile(out string error)
        {
            error = null;
            var fullPath = AxuTestReportPaths.ToFullPath(SelfCheckSuiteTempPath);
            try
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory); // 已存在时为 no-op；Temp 理论上恒在，此处兜底

                if (File.Exists(fullPath) && File.ReadAllText(fullPath) == SelfCheckSuiteJson)
                    return fullPath; // 幂等：内容一致不重写

                File.WriteAllText(fullPath, SelfCheckSuiteJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return fullPath;
            }
            catch (Exception ex)
            {
                error = $"自检套件文件写入失败：{fullPath}；{ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// 把 scene/perf 目标的套件落盘到 Assets（<see cref="ScenarioJsonMenu"/> 同款规则：
        /// <c>AssetDatabase.CreateFolder</c> 逐级建目录 + 重名自动加序号 + ImportAsset 入库）。
        /// <paramref name="assetsFolder"/> 传 null/空时默认 <see cref="DefaultGoalFolder"/>。
        /// 成功返回 true 且 <paramref name="assetPath"/> 为 "Assets/..." 相对资产路径；
        /// 失败返回 false 且 <paramref name="error"/> 说明原因。
        /// </summary>
        public static bool TryCreateGoalSuite(string goalId, string assetsFolder, out string assetPath, out string error)
        {
            assetPath = null;
            error = null;

            var json = BuildGoalSuiteJson(goalId);
            if (json == null)
            {
                error = $"未知的引导目标 \"{goalId}\"（支持 scene / perf；示例自检与单元测试不需要创建套件文件）。";
                return false;
            }

            try
            {
                var folder = string.IsNullOrWhiteSpace(assetsFolder) ? DefaultGoalFolder : assetsFolder;
                EnsureFolder(folder);

                var unique = GetUniqueAssetPath(
                    $"{folder}/{(goalId ?? string.Empty).Trim().ToLowerInvariant()}.goal.suite.json");
                File.WriteAllText(
                    AxuTestReportPaths.ToFullPath(unique),
                    json + Environment.NewLine,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                AssetDatabase.ImportAsset(unique);
                Debug.Log($"[AxuTestFramework] 引导套件已创建: {unique}");
                assetPath = unique;
                return true;
            }
            catch (Exception ex)
            {
                error = $"套件文件创建失败：{ex.Message}";
                return false;
            }
        }

        /// <summary>逐级创建资产目录（仅经 AssetDatabase API；"Assets" 根恒存在，作递归终点）。</summary>
        static void EnsureFolder(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            var parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(assetFolderPath);
            EnsureFolder(parent);

            if (!AssetDatabase.IsValidFolder(assetFolderPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>目标文件已存在时在扩展名前追加 _1.._999 序号（引导可反复点击，不覆盖既有文件）。</summary>
        static string GetUniqueAssetPath(string assetPath)
        {
            if (!File.Exists(AxuTestReportPaths.ToFullPath(assetPath)))
                return assetPath;

            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileNameWithoutExtension(assetPath);
            var extension = Path.GetExtension(assetPath);

            for (var i = 1; i < 1000; i++)
            {
                var candidate = $"{directory}/{name}_{i}{extension}";
                if (!File.Exists(AxuTestReportPaths.ToFullPath(candidate)))
                    return candidate;
            }

            return assetPath;
        }
    }
}
