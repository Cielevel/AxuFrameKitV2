using System;
using System.Collections.Generic;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// suite/scenario JSON 加载与扇出解析：
    /// LoadSuite/LoadScenario 做 schema 校验 + Validate；Resolve 完成 profile 深合并与 id 过滤。
    /// </summary>
    public static class SuiteLoader
    {
        /// <summary>加载 suite JSON（schema=axu-suite/1）；schema 不符抛 AxuSchemaException，校验失败抛 InvalidOperationException。</summary>
        public static SuiteDef LoadSuite(string path)
        {
            var suite = AxuJson.FromFile<SuiteDef>(path, SchemaNames.Suite);
            var errors = suite.Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException($"suite 校验失败：{path}；{string.Join("；", errors.ToArray())}");
            return suite;
        }

        /// <summary>加载独立 scenario JSON（schema=axu-scenario/1）；异常语义同 LoadSuite。</summary>
        public static ScenarioDef LoadScenario(string path)
        {
            var scenario = AxuJson.FromFile<ScenarioDef>(path, SchemaNames.Scenario);
            var errors = scenario.Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException($"scenario 校验失败：{path}；{string.Join("；", errors.ToArray())}");
            return scenario;
        }

        /// <summary>
        /// 把 suite 解析为可执行列表：内联 scenario 直接展开；ref 引用同文件内联定义；
        /// ref+profile 用 suite.profiles[profile] 深合并覆盖 params（profile 优先），id 追加 "@profile"；
        /// enabled=false 跳过；selectedIds 非空时仅保留 SourceId 或 Id 命中的条目。
        /// suiteDirectory：suite/scenario 文件所在目录，写入每条 ResolvedScenario.SuiteDirectory
        /// （baseline 相对路径的解析基准；单 scenario 文件路径同样传其目录）。
        /// externalById：进程内注册的代码态 scenario（gamehook，方案 §4.8）：同 id 优先于 suite 内联/ref
        /// 定义（覆盖语义），且可为纯 ref 条目供定义（补充语义）；null = 无进程内注册。
        /// </summary>
        public static List<ResolvedScenario> Resolve(SuiteDef suite, IList<string> selectedIds = null,
            string suiteDirectory = null, IDictionary<string, ScenarioDef> externalById = null)
        {
            var results = new List<ResolvedScenario>();
            if (suite == null || suite.Scenarios == null) return results;

            // 内联 scenario 索引（首个同 id 内联定义生效）
            var inlineById = new Dictionary<string, ScenarioDef>(StringComparer.Ordinal);
            foreach (var entry in suite.Scenarios)
            {
                if (entry != null && entry.Inline != null && !string.IsNullOrEmpty(entry.Inline.Id)
                    && !inlineById.ContainsKey(entry.Inline.Id))
                {
                    inlineById.Add(entry.Inline.Id, entry.Inline);
                }
            }

            foreach (var entry in suite.Scenarios)
            {
                if (entry == null) continue;

                ScenarioDef baseDef;
                string sourceId;
                if (entry.Inline != null)
                {
                    baseDef = entry.Inline;
                    sourceId = baseDef.Id;
                }
                else
                {
                    if (string.IsNullOrEmpty(entry.Ref)) continue; // 已由 Validate 拦截
                    sourceId = entry.Ref;
                    if (externalById != null && externalById.TryGetValue(entry.Ref, out var hookDef) && hookDef != null)
                        baseDef = hookDef; // gamehook 补充：ref 命中进程内注册定义
                    else if (inlineById.TryGetValue(entry.Ref, out baseDef))
                        { /* suite 内联定义 */ }
                    else
                        throw new InvalidOperationException($"suite \"{suite.Id}\" 引用的 scenario \"{entry.Ref}\" 未在内联定义/进程内注册中找到");
                }

                // gamehook 覆盖：进程内注册的同 id 定义优先于 suite 内联定义（后者覆盖语义）
                if (externalById != null && externalById.TryGetValue(sourceId, out var registered) && registered != null)
                    baseDef = registered;

                if (baseDef == null || !baseDef.Enabled) continue; // enabled=false 跳过（DOC §1）

                var hasProfile = !string.IsNullOrEmpty(entry.Profile);
                var mergedParams = baseDef.Params != null ? (JObject)baseDef.Params.DeepClone() : new JObject();
                if (hasProfile
                    && suite.Profiles != null
                    && suite.Profiles.TryGetValue(entry.Profile, out var overlay)
                    && overlay != null)
                {
                    AxuJson.DeepMerge(mergedParams, overlay);
                }

                var resolved = new ResolvedScenario
                {
                    Id = hasProfile ? $"{sourceId}@{entry.Profile}" : sourceId,
                    Capability = baseDef.Capability,
                    Params = mergedParams,
                    Budgets = baseDef.Budgets != null ? new List<BudgetRule>(baseDef.Budgets) : new List<BudgetRule>(),
                    // 扇出实例（@profile）不继承源场景的基线——不同 profile 是不同负载，
                    // 拿源场景基线对比会把正常差异误判成回归；派生实例默认无基线
                    Baseline = hasProfile ? null : baseDef.Baseline,
                    SuiteDirectory = suiteDirectory,
                    SourceId = sourceId,
                    CodeSetup = baseDef.CodeSetup,
                    CodePrepare = baseDef.CodePrepare,
                };

                if (selectedIds != null && selectedIds.Count > 0)
                {
                    var matched = false;
                    foreach (var selected in selectedIds)
                    {
                        if (selected == resolved.Id || selected == sourceId)
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (!matched) continue;
                }

                results.Add(resolved);
            }

            return results;
        }
    }
}
