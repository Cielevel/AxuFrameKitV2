using System.Collections.Generic;
using AxuTestFramework.Agent.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AxuTestFramework.Agent.UGUI
{
    /// <summary>
    /// UGUI 命中 / 降级点按后端（可选守护程序集：com.unity.ugui ≥1.0 时经 versionDefines
    /// 定义 <c>AXU_UGUI_PRESENT</c> + defineConstraints 编入；未装包时整个程序集不编译，
    /// 主 Agent 程序集零硬引用）。由 <see cref="AgentInputBackends"/> 反射发现。
    /// <para>命中信息（<see cref="HitTest"/>）用于 monkey 流水留痕；无 Input System 工程由
    /// <see cref="Click"/> 执行 <c>ExecuteEvents.ExecuteHierarchy&lt;IPointerClickHandler&gt;</c> 降级点按。
    /// raycast 缓冲复用，零持续分配。</para>
    /// </summary>
    public sealed class UguiInputBackend : IAgentUguiProbe
    {
        readonly List<RaycastResult> _raycastBuffer = new List<RaycastResult>(8);

        public string HitTest(Vector2 screenPosition)
        {
            var hitObject = FindHitObject(screenPosition);
            return hitObject != null ? hitObject.name : null;
        }

        public void Click(Vector2 screenPosition)
        {
            var hitObject = FindHitObject(screenPosition);
            if (hitObject == null) return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            ExecuteEvents.ExecuteHierarchy(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
        }

        GameObject FindHitObject(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return null;

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            _raycastBuffer.Clear();
            eventSystem.RaycastAll(pointerData, _raycastBuffer);
            return _raycastBuffer.Count > 0 ? _raycastBuffer[0].gameObject : null;
        }
    }
}
