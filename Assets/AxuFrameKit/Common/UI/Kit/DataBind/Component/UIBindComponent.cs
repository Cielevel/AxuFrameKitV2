using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    public abstract class UIBindComponent : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private int _index;

        // 公开访问器
        public string ID => _id;
        public int Index => _index;
    }
    
    // UIBindComponentGeneric<T> 保持不变
    public abstract class UIBindComponentGeneric<T> : UIBindComponent
    {
        public abstract void SetData(T data);
    }
}