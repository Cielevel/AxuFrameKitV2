using UnityEngine;

namespace AxuFrameKit.Common.UI.Kit.DataBind.Component
{
    public interface IUIBindComponent
    {
        void SetID(string id);
        void SetIndex(int index);
    }
    
    public abstract class UIBindComponent : MonoBehaviour, IUIBindComponent
    {
        [SerializeField] private string _id;
        [SerializeField] private int _index;

        // 公开访问器
        public string ID => _id;
        public int Index => _index;

        void IUIBindComponent.SetID(string id) => _id = id;

        void IUIBindComponent.SetIndex(int index) => _index = index;
    }
    
    // UIBindComponentGeneric<T> 保持不变
    public abstract class UIBindComponentGeneric<T> : UIBindComponent
    {
        public abstract void SetData(T data);
    }
}