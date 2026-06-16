using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    /// <summary>
    /// 只关心表现，不关心逻辑
    /// </summary>
    public abstract class UIToggleBaseUnit : MonoBehaviour
    {
        public abstract void SetState(bool state);
    }
}