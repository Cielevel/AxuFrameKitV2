using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>单条预算规则（schema budgets[] 条目，见 DOC/ScenarioSchema.md §1）。</summary>
    public class BudgetRule
    {
        /// <summary>指标键（如 fps.avg），须为本次运行实际产出的指标。</summary>
        [JsonProperty("metric")] public string Metric;

        /// <summary>操作符原串（">=" / "<=" / "==" / "range"）；解析见 <see cref="BudgetOpParser"/>。</summary>
        [JsonProperty("op")] public string Op;

        /// <summary>阈值（range 时不用）。</summary>
        [JsonProperty("value")] public double? Value;

        /// <summary>区间下限（range 专用）。</summary>
        [JsonProperty("valueMin")] public double? ValueMin;

        /// <summary>区间上限（range 专用）。</summary>
        [JsonProperty("valueMax")] public double? ValueMax;

        /// <summary>展示单位（ms/MB/fps…），不参与换算。</summary>
        [JsonProperty("unit")] public string Unit;

        /// <summary>违约判定级别，默认 Fail（序列化为 "fail"/"warn"）。</summary>
        [JsonProperty("severity")]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public BudgetSeverity Severity = BudgetSeverity.Fail;

        /// <summary>解析后的操作符（Op 非法时抛 ArgumentException；可先用 Validate 拦截）。</summary>
        public BudgetOp ParsedOp => BudgetOpParser.Parse(Op);

        /// <summary>校验规则完整性，返回错误描述列表（空列表=通过）。</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Metric)) errors.Add("metric 不能为空");

            var isRange = BudgetOpParser.TryParse(Op, out var op);
            if (!isRange)
            {
                errors.Add($"op \"{Op ?? "(null)"}\" 不合法（支持 \">=\" / \"<=\" / \"==\" / \"range\"）");
            }
            else if (op == BudgetOp.Range)
            {
                if (ValueMin == null || ValueMax == null) errors.Add("range 需要 valueMin 与 valueMax");
                else if (ValueMin.Value > ValueMax.Value) errors.Add($"valueMin ({ValueMin}) 不能大于 valueMax ({ValueMax})");
            }
            else if (Value == null)
            {
                errors.Add("缺少 value（仅 range 可省略）");
            }

            return errors;
        }
    }
}
