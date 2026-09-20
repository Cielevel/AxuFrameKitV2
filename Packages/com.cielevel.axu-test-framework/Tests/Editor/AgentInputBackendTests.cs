using System;
using System.Linq;
using AxuTestFramework.Agent.Tools;
using NUnit.Framework;

namespace AxuTestFramework.Tests.Editor
{
    /// <summary>
    /// [Axu/TestFramework] 输入后端正规化（backlog #5）：主 Agent 程序集对 InputSystem/UGUI 零硬引用，
    /// 两个可选守护程序集分别提供 Input 注入与 UGUI 命中/降级实现，经 <see cref="AgentInputBackends"/>
    /// 反射发现。本工程同时装有 com.unity.inputsystem 与 com.unity.ugui（见 AGENTS 依赖表），
    /// 两个后端都应可解析；其他工程缺包时对应断言按 Ignore 跳过（缺包不编译是特性）。
    /// </summary>
    [Category("Axu/TestFramework")]
    public class AgentInputBackendTests
    {
        [SetUp]
        public void SetUp()
        {
            AgentInputBackends.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            AgentInputBackends.Reset();
        }

        [Test]
        public void Agent程序集_不硬引用InputSystem与UGUI()
        {
            var references = typeof(AgentInput).Assembly
                .GetReferencedAssemblies()
                .Select(assembly => assembly.Name)
                .ToList();

            Assert.That(references, Does.Not.Contain("Unity.InputSystem"),
                "Agent 主程序集不应硬引用 InputSystem（应经可选守护程序集 AxuTestFramework.Agent.Input）");
            Assert.That(references, Does.Not.Contain("UnityEngine.UI"),
                "Agent 主程序集不应硬引用 uGUI（应经可选守护程序集 AxuTestFramework.Agent.UGUI）");
        }

        [Test]
        public void 可选后端类型_按程序集限定名_可解析()
        {
            Assert.That(Type.GetType(AgentInputBackends.InputSystemBackendTypeName), Is.Not.Null,
                "本工程装有 com.unity.inputsystem，应能解析 AxuTestFramework.Agent.Input 后端");
            Assert.That(Type.GetType(AgentInputBackends.UguiBackendTypeName), Is.Not.Null,
                "本工程装有 com.unity.ugui，应能解析 AxuTestFramework.Agent.UGUI 后端");
        }

        [Test]
        public void 后端发现_装有InputSystem_优先Input主路径()
        {
            if (Type.GetType(AgentInputBackends.InputSystemBackendTypeName) == null)
                Assert.Ignore("本工程未安装 com.unity.inputsystem，跳过主路径断言");

            Assert.That(AgentInputBackends.TapInjector, Is.Not.Null);
            Assert.That(AgentInputBackends.TapInjector.Name,
                Is.EqualTo(AgentInputBackends.InputSystemInjectorName));
        }

        [Test]
        public void 后端发现_Reset后重新解析_结果稳定()
        {
            var first = AgentInputBackends.TapInjector;
            AgentInputBackends.Reset();
            var second = AgentInputBackends.TapInjector;

            Assert.That(second?.Name, Is.EqualTo(first?.Name));
        }
    }
}
