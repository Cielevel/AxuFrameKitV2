using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 解析完成、可直接执行的 scenario（suite 扇出/合并之后、运行之前的数据形态）。
    /// </summary>
    public class ResolvedScenario
    {
        /// <summary>扇出后的最终 id（profile 扇出为 "{ref}@{profile}"，否则与 SourceId 相同）。</summary>
        public string Id;

        /// <summary>能力 id。</summary>
        public string Capability;

        /// <summary>合并 profile 后的参数 JObject（深拷贝，profile 优先）。</summary>
        public JObject Params;

        /// <summary>预算规则（无预算时为空列表 → 结果标 unbudgeted）。</summary>
        public List<BudgetRule> Budgets = new List<BudgetRule>();

        /// <summary>基线文件路径（继承自源 scenario，可选）。</summary>
        public string Baseline;

        /// <summary>源 id（不含 @profile 后缀，供过滤与追溯）。</summary>
        public string SourceId;
    }
}
