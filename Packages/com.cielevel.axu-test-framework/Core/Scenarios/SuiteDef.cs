using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>批量运行计划（schema axu-suite/1，见 DOC/ScenarioSchema.md §2）。</summary>
    public class SuiteDef
    {
        /// <summary>自描述 schema，默认 axu-suite/1。</summary>
        [JsonProperty("schema")] public string Schema = SchemaNames.Suite;

        /// <summary>suite id（全小写点分）。</summary>
        [JsonProperty("id")] public string Id;

        /// <summary>展示用标题（可选）。</summary>
        [JsonProperty("title")] public string Title;

        /// <summary>scenario 条目：字符串引用 / ref+profile 扇出 / 完整内联对象。</summary>
        [JsonProperty("scenarios")] public List<SuiteScenarioRef> Scenarios = new List<SuiteScenarioRef>();

        /// <summary>环境档：对 scenario.params 的深合并覆盖（profile 优先）。</summary>
        [JsonProperty("profiles")] public Dictionary<string, JObject> Profiles = new Dictionary<string, JObject>();

        /// <summary>运行设置。</summary>
        [JsonProperty("settings")] public SuiteSettings Settings = new SuiteSettings();

        /// <summary>校验 id、scenarios 非空且各 ref 均能解析到本文件内联定义，返回错误描述列表（空列表=通过）。</summary>
        public List<string> Validate()
        {
            return Validate(externallyProvidedIds: null);
        }

        /// <summary>
        /// 校验并额外认可 <paramref name="externallyProvidedIds"/>（进程内注册的代码态 scenario id）满足 ref；
        /// 未解析的 ref 仍作为错误返回。
        /// </summary>
        public List<string> Validate(IEnumerable<string> externallyProvidedIds)
        {
            var errors = new List<string>();
            var unresolvedRefs = new List<string>();
            Validate(externallyProvidedIds, errors, unresolvedRefs);
            errors.AddRange(unresolvedRefs);
            return errors;
        }

        /// <summary>
        /// 校验：结构问题记入 <paramref name="errors"/>（任何情况都必须拦下），
        /// ref 未能在内联定义或 <paramref name="externallyProvidedIds"/> 中解析的记入
        /// <paramref name="unresolvedRefs"/>——代码态 scenario 由运行时注册（gamehook），
        /// 静态发现路径需要与"真的写错了引用"区分开，故两类问题分开回传。
        /// </summary>
        public void Validate(IEnumerable<string> externallyProvidedIds, List<string> errors, List<string> unresolvedRefs)
        {
            if (errors == null) throw new System.ArgumentNullException(nameof(errors));
            if (unresolvedRefs == null) throw new System.ArgumentNullException(nameof(unresolvedRefs));

            if (string.IsNullOrWhiteSpace(Id)) errors.Add("id 不能为空");
            if (Scenarios == null || Scenarios.Count == 0)
            {
                errors.Add("scenarios 不能为空");
                return;
            }

            // 可满足 ref 的 id 集合：本文件内联定义 + 调用方告知的外部（进程内注册）定义
            var knownIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var entry in Scenarios)
            {
                if (entry != null && entry.Inline != null && !string.IsNullOrEmpty(entry.Inline.Id))
                    knownIds.Add(entry.Inline.Id);
            }
            if (externallyProvidedIds != null)
            {
                foreach (var id in externallyProvidedIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        knownIds.Add(id);
                }
            }

            for (var i = 0; i < Scenarios.Count; i++)
            {
                var entry = Scenarios[i];
                if (entry == null)
                {
                    errors.Add($"scenarios[{i}] 为空");
                    continue;
                }

                if (entry.Inline != null)
                {
                    foreach (var error in entry.Inline.Validate())
                        errors.Add($"scenarios[{i}]（内联 {entry.Inline.Id ?? "(null)"}）: {error}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Ref))
                {
                    errors.Add($"scenarios[{i}] 缺少 ref（或完整内联 scenario 对象）");
                    continue;
                }

                if (!knownIds.Contains(entry.Ref))
                {
                    unresolvedRefs.Add(
                        $"scenarios[{i}] 引用的 scenario \"{entry.Ref}\" 既未在 suite 内联定义中、也不在已注册的代码态 scenario 清单中");
                }
            }
        }
    }
}
