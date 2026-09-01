namespace AxuTestFramework.Core
{
    /// <summary>
    /// 场景测试数据契约的 schema 名称常量（定义见包 DOC/ScenarioSchema.md）。
    /// </summary>
    public static class SchemaNames
    {
        /// <summary>单个测试情景定义（axu-scenario/1）。</summary>
        public const string Scenario = "axu-scenario/1";

        /// <summary>批量运行计划（axu-suite/1）。</summary>
        public const string Suite = "axu-suite/1";

        /// <summary>场景测试运行报告（axu-scenario-report/1）。</summary>
        public const string ScenarioReport = "axu-scenario-report/1";

        /// <summary>趋势 CSV 存储（axu-trend/1，见 Core/Trend/TrendStore.cs）。</summary>
        public const string Trend = "axu-trend/1";
    }
}
