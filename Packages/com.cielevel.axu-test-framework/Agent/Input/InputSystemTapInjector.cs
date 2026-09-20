using AxuTestFramework.Agent.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AxuTestFramework.Agent.Input
{
    /// <summary>
    /// Input System 点按注入后端（可选守护程序集：com.unity.inputsystem ≥1.0 时经 versionDefines
    /// 定义 <c>AXU_INPUTSYSTEM_PRESENT</c> + defineConstraints 编入；未装包时整个程序集不编译，
    /// 主 Agent 程序集零硬引用）。由 <see cref="AgentInputBackends"/> 反射发现。
    /// <para>注入 move → down → up 三事件（鼠标，<c>MouseState.WithPosition/WithButton</c>）；
    /// 无鼠标设备（触屏形态）退 <c>Touchscreen.current</c> 注入 <c>TouchState</c> 的
    /// Began → Moved → Ended（固定 touchId=1，避免多指状态污染）。事件在下一次 Input System update 生效。</para>
    /// </summary>
    public sealed class InputSystemTapInjector : IAgentTapInjector
    {
        public string Name => AgentInputBackends.InputSystemInjectorName;

        public bool Available => Mouse.current != null || Touchscreen.current != null;

        public bool TryInject(Vector2 screenPosition)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                InputSystem.QueueStateEvent(mouse,
                    new MouseState { position = screenPosition });
                InputSystem.QueueStateEvent(mouse,
                    new MouseState { position = screenPosition }.WithButton(MouseButton.Left, true));
                InputSystem.QueueStateEvent(mouse,
                    new MouseState { position = screenPosition }.WithButton(MouseButton.Left, false));
                return true;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                const int touchId = 1;
                InputSystem.QueueStateEvent(touchscreen,
                    new TouchState { touchId = touchId, phase = UnityEngine.InputSystem.TouchPhase.Began, position = screenPosition });
                InputSystem.QueueStateEvent(touchscreen,
                    new TouchState { touchId = touchId, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = screenPosition, delta = Vector2.zero });
                InputSystem.QueueStateEvent(touchscreen,
                    new TouchState { touchId = touchId, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = screenPosition });
                return true;
            }

            return false;
        }
    }
}
