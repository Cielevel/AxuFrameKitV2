using System;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 可选输入后端发现器（backlog #5 正规化）：主 Agent 程序集不硬引用 InputSystem/UGUI，
    /// 两个可选守护程序集分别提供实现，本类按程序集限定名反射发现并缓存：
    /// ① <c>AxuTestFramework.Agent.Input</c>（com.unity.inputsystem ≥1.0 时编译）→ 主路径注入；
    /// ② <c>AxuTestFramework.Agent.UGUI</c>（com.unity.ugui ≥1.0 时编译）→ 命中检测 + 降级点按。
    /// 两者都缺时 <see cref="TapInjector"/>/<see cref="UguiProbe"/> 为 null（monkey 仅记录坐标）。
    /// 发现结果在进程内缓存（域重载会重置静态，自动重新解析）。
    /// </summary>
    public static class AgentInputBackends
    {
        /// <summary>Input System 可选后端程序集限定类型名（未装包/未编译时为 null）。</summary>
        public const string InputSystemBackendTypeName =
            "AxuTestFramework.Agent.Input.InputSystemTapInjector, AxuTestFramework.Agent.Input";

        /// <summary>UGUI 可选后端程序集限定类型名（未装包/未编译时为 null）。</summary>
        public const string UguiBackendTypeName =
            "AxuTestFramework.Agent.UGUI.UguiInputBackend, AxuTestFramework.Agent.UGUI";

        /// <summary>Input System 后端标识。</summary>
        public const string InputSystemInjectorName = "inputsystem";

        /// <summary>UGUI 后端标识（仅命中检测/降级点按，非注入器）。</summary>
        public const string UguiInjectorName = "ugui";

        static bool _resolved;
        static IAgentTapInjector _tapInjector;
        static IAgentUguiProbe _uguiProbe;

        /// <summary>主注入器（Input System；未装包时为 null）。</summary>
        public static IAgentTapInjector TapInjector
        {
            get
            {
                EnsureResolved();
                return _tapInjector;
            }
        }

        /// <summary>UGUI 命中/降级探针（未装包时为 null）。</summary>
        public static IAgentUguiProbe UguiProbe
        {
            get
            {
                EnsureResolved();
                return _uguiProbe;
            }
        }

        /// <summary>解析可选后端（幂等；异常吞掉降级为 null，绝不影响测试本体）。</summary>
        public static void EnsureResolved()
        {
            if (_resolved)
                return;
            _resolved = true;

            _tapInjector = CreateBackend(InputSystemBackendTypeName) as IAgentTapInjector;
            _uguiProbe = CreateBackend(UguiBackendTypeName) as IAgentUguiProbe;
        }

        /// <summary>重置发现缓存（测试/域状态复位用；下次访问重新解析）。</summary>
        public static void Reset()
        {
            _resolved = false;
            _tapInjector = null;
            _uguiProbe = null;
        }

        static object CreateBackend(string assemblyQualifiedTypeName)
        {
            try
            {
                var type = Type.GetType(assemblyQualifiedTypeName);
                return type == null ? null : Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                var shortName = assemblyQualifiedTypeName.Split(',')[0];
                Debug.LogWarning($"[AxuTestAgent] 可选输入后端加载失败（{shortName}）：{ex.Message}");
                return null;
            }
        }
    }
}
