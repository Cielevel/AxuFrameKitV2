using System;
using System.Collections.Generic;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// `-axu*` 命令行参数解析（Player/Editor 启动来源之一，纯 .NET、零引擎依赖）。
    /// 兼容单/双横线与大小写；取值型参数读取下一个 token；未知 -axu* 记入 IgnoredArgs。
    /// </summary>
    public static class ScenarioCli
    {
        /// <summary>解析参数表（如 System.Environment.GetCommandLineArgs()），恒返回非 null 结果。</summary>
        public static ScenarioCliOptions Parse(IList<string> args)
        {
            var options = new ScenarioCliOptions();
            if (args == null) return options;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (string.IsNullOrEmpty(arg)) continue;

                var token = arg.TrimStart('-').ToLowerInvariant();
                switch (token)
                {
                    case "axusuite":
                        options.SuitePath = NextValue(args, ref i);
                        break;
                    case "axuscenario":
                        var scenarioId = NextValue(args, ref i);
                        if (!string.IsNullOrWhiteSpace(scenarioId)) options.ScenarioIds.Add(scenarioId);
                        break;
                    case "axureport":
                        options.ReportPath = NextValue(args, ref i);
                        break;
                    case "axuartifacts":
                        options.ArtifactsDir = NextValue(args, ref i);
                        break;
                    case "axuquitondone":
                        options.QuitOnDone = true;
                        break;
                    case "axuwarnasfail":
                        options.WarnAsFail = true;
                        break;
                    case "axuseed":
                        var seedText = NextValue(args, ref i);
                        if (seedText != null && int.TryParse(seedText, out var seed)) options.Seed = seed;
                        break;
                    case "axuoverlay":
                        options.OverlayHint = true;
                        break;
                    case "axunooverlay":
                        options.OverlayHint = false;
                        break;
                    default:
                        if (token.StartsWith("axu", StringComparison.Ordinal)) options.IgnoredArgs.Add(arg);
                        break;
                }
            }

            return options;
        }

        /// <summary>读取当前 flag 的取值 token（无值返回 null，并把游标推进到取值上）。</summary>
        static string NextValue(IList<string> args, ref int index)
        {
            if (index + 1 < args.Count)
            {
                index++;
                return args[index];
            }
            return null;
        }
    }
}
