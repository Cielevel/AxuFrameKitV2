using System;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 预算比较操作符（schema 串形："&gt;=" / "&lt;=" / "==" / "range"，见 DOC/ScenarioSchema.md §1）。
    /// </summary>
    public enum BudgetOp
    {
        /// <summary>&gt;=：实际值须不小于阈值。</summary>
        GreaterOrEqual,

        /// <summary>&lt;=：实际值须不大于阈值。</summary>
        LessOrEqual,

        /// <summary>==：实际值须等于阈值。</summary>
        Equal,

        /// <summary>区间判定（valueMin..valueMax，含端点）。</summary>
        Range,
    }

    /// <summary>BudgetOp 与 schema 操作符字符串的互转。</summary>
    public static class BudgetOpParser
    {
        /// <summary>把 schema 操作符串解析为枚举；失败抛 ArgumentException。</summary>
        public static BudgetOp Parse(string text)
        {
            if (TryParse(text, out var op)) return op;
            throw new ArgumentException(
                $"未知的预算操作符 \"{text ?? "(null)"}\"，仅支持 \">=\" / \"<=\" / \"==\" / \"range\"。", nameof(text));
        }

        /// <summary>尝试解析 schema 操作符串（自动去首尾空白）。</summary>
        public static bool TryParse(string text, out BudgetOp op)
        {
            switch (text != null ? text.Trim() : null)
            {
                case ">=": op = BudgetOp.GreaterOrEqual; return true;
                case "<=": op = BudgetOp.LessOrEqual; return true;
                case "==": op = BudgetOp.Equal; return true;
                case "range": op = BudgetOp.Range; return true;
                default: op = BudgetOp.GreaterOrEqual; return false;
            }
        }
    }
}
