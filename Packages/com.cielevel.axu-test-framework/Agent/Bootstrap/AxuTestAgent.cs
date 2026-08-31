using System;
using System.Collections.Generic;
using AxuTestFramework.Agent.Runner;
using AxuTestFramework.Core.Scenarios;
using UnityEngine;

namespace AxuTestFramework.Agent.Bootstrap
{
    /// <summary>
    /// Agent 静态门面（游戏内控制台 / 调试面板 / 自定义触发器的代码入口）。
    /// 激活类 API 全部以 AXU_TEST_AGENT 守护：define 关闭时为空操作/禁用结果，不产生任何激活行为。
    /// </summary>
    public static class AxuTestAgent
    {
        static AxuTestAgent()
        {
#if AXU_TEST_AGENT
            BuiltInCapabilities.RegisterAll();
#endif
        }

        /// <summary>Agent 是否已启用（AXU_TEST_AGENT define 开关）。</summary>
        public static bool IsEnabled
        {
            get
            {
#if AXU_TEST_AGENT
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>注册自定义能力（显式 API，IL2CPP 安全）；define 关闭时空操作。</summary>
        public static void RegisterCapability(IAxuTestCapability capability)
        {
#if AXU_TEST_AGENT
            BuiltInCapabilities.RegisterAll();
            AxuCapabilityRegistry.Register(capability);
#endif
        }

        /// <summary>
        /// 解析并启动一次 suite（或单 scenario 文件）运行，异步执行、结果落报告与退出码。
        /// define 关闭时返回 false（禁用结果），不启动任何运行。
        /// </summary>
        /// <param name="path">suite 或单 scenario JSON 路径。</param>
        /// <param name="configure">启动参数回调（可修改 ReportPath/ScenarioIds/QuitOnDone 等）。</param>
        /// <returns>true=启动请求已受理；false=未启用、参数无效或宿主正忙。</returns>
        public static bool RunSuite(string path, Action<ScenarioCliOptions> configure = null)
        {
#if AXU_TEST_AGENT
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("[AxuTestAgent] RunSuite 需要 suite/scenario JSON 路径");
                return false;
            }

            var options = new ScenarioCliOptions { SuitePath = path };
            configure?.Invoke(options);

            var launch = new ScenarioLaunchOptions
            {
                SuitePath = options.SuitePath,
                ScenarioIds = options.ScenarioIds != null
                    ? new List<string>(options.ScenarioIds)
                    : new List<string>(),
                ReportPath = options.ReportPath,
                ArtifactsDir = options.ArtifactsDir,
                QuitOnDone = options.QuitOnDone ?? false,
                WarnAsFail = options.WarnAsFail ?? false,
                Seed = options.Seed,
                OverlayHint = options.OverlayHint,
                Source = "editor-bridge", // API 触发归入 Editor/控制台桥语义
            };

            var host = AgentRunnerHost.EnsureHost();
            return host.StartWithLaunchOptions(launch);
#else
            return false; // define 未开启：返回禁用结果
#endif
        }
    }
}
