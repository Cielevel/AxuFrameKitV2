using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor.Compilation;

namespace AxuTestFramework.Editor
{
    /// <summary>
    /// 测试程序集枚举器：axu_test_run 的"强制安全过滤"数据源。
    /// <para>识别规则（按 asmdef 文件声明判定，而非运行期引用闭包——Editor 下 Unity 会把 TestRunner 引用
    /// 注入所有程序集，运行期信号无法区分）：asmdef 的 references 含 UnityEngine.TestRunner /
    /// UnityEditor.TestRunner，或 defineConstraints 含 UNITY_INCLUDE_TESTS；无 asmdef 的
    /// Assembly-CSharp* 按本工程"测试必须进 asmdef"的惯例不算测试程序集。</para>
    /// <para>硬性安全规则：Unity.Pipeline.Tests*（pipeline 包自带测试）被无条件剔除——其 SecurityTokenTests
    /// 会清空 pipeline 服务的鉴权 token（SessionState），导致本地 API 全部 401（M0 报告"token 中毒"事故）。
    /// axu_test_run 绝不发出未按此清单收敛程序集范围的裸跑。</para>
    /// </summary>
    public static class AxuTestAssemblyScanner
    {
        /// <summary>无条件排除的程序集名前缀（忽略大小写）。</summary>
        public const string ForbiddenAssemblyPrefix = "Unity.Pipeline.Tests";

        /// <summary>判定测试程序集时识别的 TestRunner 引用名（asmdef name 形式）。</summary>
        private static readonly string[] TestRunnerReferenceNames =
        {
            "UnityEngine.TestRunner",
            "UnityEditor.TestRunner"
        };

        /// <summary>判定测试程序集时识别的定义约束（包测试程序集的守护约束）。</summary>
        private const string IncludeTestsConstraint = "UNITY_INCLUDE_TESTS";

        /// <summary>
        /// 枚举当前工程可安全运行的测试程序集短名（已剔除 <see cref="ForbiddenAssemblyPrefix"/> 命中的程序集），
        /// 按名称排序；out 参数返回被剔除的清单（调用方可写入响应作审计线索）。
        /// </summary>
        public static List<string> GetSafeTestAssemblies(out List<string> excluded)
        {
            var safe = new List<string>();
            excluded = new List<string>();

            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
            {
                var name = assembly.name;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!IsTestAssembly(assembly))
                    continue;

                if (name.StartsWith(ForbiddenAssemblyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    excluded.Add(name);
                    continue;
                }

                safe.Add(name);
            }

            safe.Sort(StringComparer.OrdinalIgnoreCase);
            excluded.Sort(StringComparer.OrdinalIgnoreCase);
            return safe;
        }

        /// <summary>
        /// 是否测试程序集：按该程序集 asmdef 文件的声明判定（references 含 TestRunner 或
        /// defineConstraints 含 UNITY_INCLUDE_TESTS）；无 asmdef 的程序集返回 false。
        /// 注意 GetAssemblyDefinitionFilePathFromScriptPath 返回相对工程根的路径，
        /// 而进程 cwd 不保证是工程根（从 Hub 启动时），必须先锚定工程根。
        /// </summary>
        private static bool IsTestAssembly(Assembly assembly)
        {
            if (assembly.sourceFiles == null || assembly.sourceFiles.Length == 0)
                return false;

            var relativePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(assembly.sourceFiles[0]);
            if (string.IsNullOrEmpty(relativePath))
                return false;

            var asmdefPath = AxuTestReportPaths.ToFullPath(relativePath);
            if (!File.Exists(asmdefPath))
                return false;

            return IsTestAssemblyByAsmdef(asmdefPath);
        }

        /// <summary>解析 asmdef JSON：references 含 TestRunner 或 defineConstraints 含 UNITY_INCLUDE_TESTS。</summary>
        private static bool IsTestAssemblyByAsmdef(string asmdefPath)
        {
            try
            {
                // 注意：反序列化到 Dictionary<string, object> 时嵌套数组会变成 JArray/JValue 而非 string，
                // 必须用强类型 DTO 承接
                var definition = JsonConvert.DeserializeObject<AsmdefFile>(File.ReadAllText(asmdefPath));
                if (definition == null)
                    return false;

                if (definition.References != null)
                {
                    foreach (var name in definition.References)
                    {
                        foreach (var testRunnerName in TestRunnerReferenceNames)
                        {
                            if (string.Equals(name, testRunnerName, StringComparison.Ordinal))
                                return true;
                        }
                    }
                }

                if (definition.DefineConstraints != null)
                {
                    foreach (var constraint in definition.DefineConstraints)
                    {
                        if (string.Equals(constraint, IncludeTestsConstraint, StringComparison.Ordinal))
                            return true;
                    }
                }
            }
            catch (Exception)
            {
                // asmdef 读取失败按非测试程序集处理
            }
            return false;
        }

        /// <summary>asmdef 文件的最小解析模型（只取判定所需两个字段；字段名与 asmdef JSON 键一致）。</summary>
        private sealed class AsmdefFile
        {
            /// <summary>asmdef references 数组。</summary>
            public string[] References { get; set; }

            /// <summary>asmdef defineConstraints 数组。</summary>
            public string[] DefineConstraints { get; set; }
        }
    }
}
