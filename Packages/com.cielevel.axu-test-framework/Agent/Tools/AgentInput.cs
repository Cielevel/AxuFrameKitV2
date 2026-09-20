using System;
using UnityEngine;

namespace AxuTestFramework.Agent.Tools
{
    /// <summary>
    /// 统一输入注入门面（monkey 能力的输入底座，方案 §4.7 / P0 决策）：
    /// <list type="bullet">
    /// <item>装有 Input System（可选守护程序集 <c>AxuTestFramework.Agent.Input</c>）时主路径走
    ///   <c>InputSystem.QueueStateEvent</c>：向 <c>Mouse.current</c> 注入 move → down → up 三事件
    ///   （<c>MouseState.WithPosition/WithButton</c>）；无鼠标设备（触屏形态）退 <c>Touchscreen.current</c>
    ///   注入 <c>TouchState</c> 的 Began → Moved → Ended。事件在下一次 Input System update 时生效
    ///   （Player/编辑器常规帧循环即触发）。</item>
    /// <item>未装 Input System（或两形态设备均缺）降级：对 UGUI raycast 命中对象执行
    ///   <c>ExecuteEvents.ExecuteHierarchy&lt;IPointerClickHandler&gt;</c>（可选守护程序集
    ///   <c>AxuTestFramework.Agent.UGUI</c>）；未命中仅记录坐标。
    ///   降级发生时经 <see cref="InfoFindingSink"/> 出一次性 info 提示（供 runner 转 Finding）。</item>
    /// <item>两个可选后端都缺（干净工程）时仅记录坐标，不产生输入事件，并出一次性 info 提示。</item>
    /// </list>
    /// 后端经 <see cref="AgentInputBackends"/> 反射发现——主 Agent 程序集对 InputSystem/UGUI 零硬引用
    /// （backlog #5 正规化）。坐标均使用屏幕像素坐标（左下原点，y 向上）；UGUI 命中检测仅用于流水
    /// 留痕与降级执行，不影响 Input System 主路径（命中与否由引擎事件自行分派）。
    /// </summary>
    public static class AgentInput
    {
        static bool _noticeEmitted;

        static Action<string> _infoFindingSink;

        /// <summary>
        /// 一次性 info 提示回调（降级/不可用提示触发一次，参数为提示文案）。
        /// runner 在运行窗口内设置、结束时置 null（静态回调注意成对清理）。
        /// setter 被设置（runner 开始新一次运行）时复位一次性标志，保证每次运行的首个提示都能产出。
        /// </summary>
        public static Action<string> InfoFindingSink
        {
            get => _infoFindingSink;
            set
            {
                _infoFindingSink = value;
                if (value != null) _noticeEmitted = false;
            }
        }

        /// <summary>Input System 主路径是否可用（可选后端已编译且存在鼠标/触屏设备）。</summary>
        public static bool InputSystemTapAvailable
        {
            get
            {
                var injector = AgentInputBackends.TapInjector;
                return injector != null && injector.Available &&
                       injector.Name == AgentInputBackends.InputSystemInjectorName;
            }
        }

        /// <summary>
        /// 在 <paramref name="screenPosition"/>（屏幕像素坐标）注入一次点按，
        /// 返回 UGUI raycast 命中的对象名（未命中/无 UGUI 后端为 null）。不抛异常。
        /// </summary>
        public static string InjectTap(Vector2 screenPosition)
        {
            var injector = AgentInputBackends.TapInjector;
            var probe = AgentInputBackends.UguiProbe;
            var hitName = probe != null ? probe.HitTest(screenPosition) : null;

            // 主路径：Input System 注入成功即返回（hit 信息仅流水留痕，不改变引擎事件分派）
            if (injector != null && injector.Name == AgentInputBackends.InputSystemInjectorName &&
                injector.TryInject(screenPosition))
            {
                return hitName;
            }

            // 降级 1：UGUI 后端存在（未装 InputSystem / 无设备）→ 一次性提示 + 精确点按（未命中仅留痕）
            if (probe != null)
            {
                NotifyOnce(
                    "InputSystem 未装或无鼠标/触屏设备，降级 UGUI 点按（ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>）；" +
                    "未命中 UGUI 元素的位置仅记录坐标，不产生输入事件");
                probe.Click(screenPosition);
                return hitName;
            }

            // 降级 2：两个可选后端都缺（干净工程）→ 仅记录坐标
            if (injector == null)
            {
                NotifyOnce(
                    "InputSystem 与 UGUI 后端均不可用（未安装 com.unity.inputsystem / com.unity.ugui）：" +
                    "本次仅记录坐标，不产生输入事件");
                return hitName;
            }

            // 边界：装有 InputSystem 但无鼠标/触屏设备，且无 UGUI 后端
            NotifyOnce("InputSystem 已装但无鼠标/触屏设备，且 UGUI 后端不可用：本次仅记录坐标，不产生输入事件");
            return hitName;
        }

        /// <summary>提示只发一次（内容含坐标注入/降级语义），经 sink 或 Debug.Log 留痕。</summary>
        static void NotifyOnce(string message)
        {
            if (_noticeEmitted)
                return;
            _noticeEmitted = true;

            var sink = _infoFindingSink;
            if (sink != null) sink(message);
            else Debug.Log("[AxuTestAgent] " + message);
        }
    }
}
