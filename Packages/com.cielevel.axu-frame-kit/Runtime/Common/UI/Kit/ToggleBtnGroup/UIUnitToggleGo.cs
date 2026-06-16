using UnityEngine;
using UnityEngine.UI;

namespace AxuFrameKit.Common.UI.Kit.ToggleBtnGroup
{
    public class UIUnitToggleGo : UIToggleBaseUnit
    {
        public override void SetState(bool state) => gameObject.SetActive(state);
    }
}