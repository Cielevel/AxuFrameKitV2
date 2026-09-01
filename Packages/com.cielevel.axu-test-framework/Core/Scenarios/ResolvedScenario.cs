using System;
using System.Collections;
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

        /// <summary>基线文件路径（继承自源 scenario，可选；相对 <see cref="SuiteDirectory"/> 解析）。</summary>
        public string Baseline;

        /// <summary>suite/scenario 文件所在目录（scenario.baseline 相对路径的解析基准；宿主在解析时填充）。</summary>
        public string SuiteDirectory;

        /// <summary>源 id（不含 @profile 后缀，供过滤与追溯）。</summary>
        public string SourceId;

        /// <summary>代码态 setup 委托（FromCode 路径透传；宿主在能力分发前同步调用，可空）。</summary>
        public Action<ScenarioContext> CodeSetup;

        /// <summary>代码态准备协程工厂（FromCodeRoutine 路径透传；宿主拼在 Runner 协程前执行，可空）。</summary>
        public Func<IEnumerator> CodePrepare;
    }
}
