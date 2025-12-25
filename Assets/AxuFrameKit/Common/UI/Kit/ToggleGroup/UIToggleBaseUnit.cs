using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ToggleGroup
{
    /// <summary>
    /// 只关心表现层，不关心逻辑
    /// </summary>
    public abstract class UIToggleBaseUnit : MonoBehaviour
    {
        public abstract void SetState(bool state);
    }
}