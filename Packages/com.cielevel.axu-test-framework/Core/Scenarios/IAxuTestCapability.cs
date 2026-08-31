using System;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// 测试能力统一接口（扩展点核心）：入口层只面向注册表编程，游戏工程可实现并注册自定义能力。
    /// </summary>
    public interface IAxuTestCapability
    {
        /// <summary>能力 id（scenario.capability 引用，如 "perf" / "echo"）。</summary>
        string Id { get; }

        /// <summary>展示名（看板/Overlay 用）。</summary>
        string DisplayName { get; }

        /// <summary>该能力的参数 POCO 类型（scenario.params 反序列化目标）。</summary>
        Type ParamType { get; }

        /// <summary>创建运行器（每次执行新建，宿主以协程驱动）。</summary>
        IScenarioRunner CreateRunner(ScenarioContext context);
    }
}
