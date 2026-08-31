using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>单个测试情景定义（schema axu-scenario/1，见 DOC/ScenarioSchema.md §1）。</summary>
    public class ScenarioDef
    {
        /// <summary>自描述 schema，默认 axu-scenario/1。</summary>
        [JsonProperty("schema")] public string Schema = SchemaNames.Scenario;

        /// <summary>全小写点分 id（suite 内唯一）。</summary>
        [JsonProperty("id")] public string Id;

        /// <summary>能力 id（须为注册表中的 capability id）。</summary>
        [JsonProperty("capability")] public string Capability;

        /// <summary>展示用标题（可选）。</summary>
        [JsonProperty("title")] public string Title;

        /// <summary>是否启用，默认 true；false 时 suite 跳过。</summary>
        [JsonProperty("enabled")] public bool Enabled = true;

        /// <summary>能力参数（保留键见 DOC §4，其余由 capability 自行解释）。</summary>
        [JsonProperty("params")] public JObject Params = new JObject();

        /// <summary>预算规则；无 budgets 时结果标 unbudgeted=true。</summary>
        [JsonProperty("budgets")] public List<BudgetRule> Budgets = new List<BudgetRule>();

        /// <summary>基线文件路径（可选，相对 suite 文件）。</summary>
        [JsonProperty("baseline")] public string Baseline;

        /// <summary>校验定义完整性，返回错误描述列表（空列表=通过）。</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("id 不能为空");
            if (string.IsNullOrWhiteSpace(Capability)) errors.Add($"capability 不能为空（id={Id ?? "(null)"}）");

            if (Budgets != null)
            {
                for (var i = 0; i < Budgets.Count; i++)
                {
                    var rule = Budgets[i];
                    if (rule == null)
                    {
                        errors.Add($"budgets[{i}] 为空");
                        continue;
                    }
                    foreach (var error in rule.Validate()) errors.Add($"budgets[{i}]: {error}");
                }
            }

            return errors;
        }
    }
}
