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
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("id 不能为空");
            if (Scenarios == null || Scenarios.Count == 0)
            {
                errors.Add("scenarios 不能为空");
                return errors;
            }

            // 内联 scenario 索引（Phase 1 引用只解析到同文件内联定义）
            var inlineIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var entry in Scenarios)
            {
                if (entry != null && entry.Inline != null && !string.IsNullOrEmpty(entry.Inline.Id))
                    inlineIds.Add(entry.Inline.Id);
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

                if (!inlineIds.Contains(entry.Ref))
                    errors.Add($"scenarios[{i}] 引用的 scenario \"{entry.Ref}\" 在 suite 内联定义中不存在");
            }

            return errors;
        }
    }
}
