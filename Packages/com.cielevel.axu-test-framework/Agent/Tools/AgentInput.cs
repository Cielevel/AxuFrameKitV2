using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if AXU_INPUTSYSTEM_PRESENT
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 统一输入注入门面（monkey 能力的输入底座，方案 §4.7 / P0 决策）：
    /// <list type="bullet">
    /// <item>装有 Input System（versionDefines 定义 <c>AXU_INPUTSYSTEM_PRESENT</c>）时主路径走
    ///   <c>InputSystem.QueueStateEvent</c>：向 <c>Mouse.current</c> 注入 move → down → up 三事件
    ///   （<c>MouseState.WithPosition/WithButton</c>）；无鼠标设备（触屏形态）退 <c>Touchscreen.current</c>
    ///   注入 <c>TouchState</c> 的 Began → Moved → Ended。事件在下一次 Input System update 时生效
    ///   （Player/编辑器常规帧循环即触发）。</item>
    /// <item>未装 Input System（或两形态设备均缺）降级：对 UGUI raycast 命中对象执行
    ///   <c>ExecuteEvents.ExecuteHierarchy&lt;IPointerClickHandler&gt;</c>；未命中仅记录坐标。
    ///   降级发生时经 <see cref="InfoFindingSink"/> 出一次性 info 提示（供 runner 转 Finding）。</item>
    /// </list>
    /// 坐标均使用屏幕像素坐标（左下原点，y 向上）；UGUI 命中检测仅用于流水留痕与降级执行，
    /// 不影响 Input System 主路径（命中与否由引擎事件自行分派）。
    /// </summary>
    public static class AgentInput
    {
        static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>(8);

        static bool _fallbackNoticeEmitted;

        /// <summary>
        /// 一次性 info 提示回调（降级 UGUI 点按时触发一次，参数为提示文案）。
        /// runner 在运行窗口内设置、结束时置 null（静态回调注意成对清理）。
        /// setter 被设置（runner 开始新一次运行）时复位一次性标志，保证每次运行的首个降级 info 都能产出。
        /// </summary>
        public static Action<string> InfoFindingSink
        {
            get => _infoFindingSink;
            set
            {
                _infoFindingSink = value;
                if (value != null) _fallbackNoticeEmitted = false;
            }
        }

        static Action<string> _infoFindingSink;

        /// <summary>Input System 主路径是否可用（程序集经 versionDefines 编入且存在鼠标/触屏设备）。</summary>
        public static bool InputSystemTapAvailable
        {
            get
            {
#if AXU_INPUTSYSTEM_PRESENT
                return Mouse.current != null || Touchscreen.current != null;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 在 <paramref name="screenPosition"/>（屏幕像素坐标）注入一次点按，
        /// 返回 UGUI raycast 命中的对象名（未命中为 null）。不抛异常。
        /// </summary>
        public static string InjectTap(Vector2 screenPosition)
        {
            var hitName = FindUguiHitName(screenPosition);

#if AXU_INPUTSYSTEM_PRESENT
            if (QueueInputSystemTap(screenPosition)) return hitName;
#endif
            ExecuteUguiFallbackTap(screenPosition, hitName);
            return hitName;
        }

#if AXU_INPUTSYSTEM_PRESENT
        /// <summary>
        /// Input System 主路径：move → down → up 三事件（鼠标），或 Began → Moved → Ended（触屏）。
        /// 设备缺失返回 false（调用方降级）。
        /// </summary>
        static bool QueueInputSystemTap(Vector2 screenPosition)
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
                const int touchId = 1; // 固定 touchId：合成单指序列，避免多指状态污染
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
#endif

        /// <summary>降级路径：对命中对象（沿父链）执行 pointerClick；同时保证一次性 info 提示发出（含未命中情形）。</summary>
        static void ExecuteUguiFallbackTap(Vector2 screenPosition, string hitName)
        {
            NotifyFallbackOnce();

            if (hitName == null) return; // 未命中：仅记录坐标（调用方流水已含 pos）

            var hitObject = FindUguiHitObject(screenPosition);
            if (hitObject == null) return;

            var eventSystem = EventSystem.current;
            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            ExecuteEvents.ExecuteHierarchy(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>UGUI raycast 命中对象名（EventSystem 缺失/未命中返回 null；缓冲复用零持续分配）。</summary>
        static string FindUguiHitName(Vector2 screenPosition)
        {
            var hitObject = FindUguiHitObject(screenPosition);
            return hitObject != null ? hitObject.name : null;
        }

        static GameObject FindUguiHitObject(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return null;

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            RaycastBuffer.Clear();
            eventSystem.RaycastAll(pointerData, RaycastBuffer);
            return RaycastBuffer.Count > 0 ? RaycastBuffer[0].gameObject : null;
        }

        /// <summary>降级提示只发一次（内容含坐标注入语义说明），经 sink 或 Debug.Log 留痕。</summary>
        static void NotifyFallbackOnce()
        {
            if (_fallbackNoticeEmitted) return;
            _fallbackNoticeEmitted = true;

            const string message =
                "InputSystem 未装或无鼠标/触屏设备，降级 UGUI 点按（ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>）；" +
                "未命中 UGUI 元素的位置仅记录坐标，不产生输入事件";
            var sink = InfoFindingSink;
            if (sink != null) sink(message);
            else Debug.Log("[AxuTestAgent] " + message);
        }
    }
}
