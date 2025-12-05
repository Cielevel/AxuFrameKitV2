namespace AxuFrameKit.Common.UI.Kit.DataBind
{
    using Component;
    
    public static class UIData
    {
        // 自动推断 T，不需要手动写 <string> 或 <Sprite>
        public static UIDataUnit<T> Id<T>(string id, T value)
        {
            return new UIDataUnit<T>(id, value);
        }

        public static UIDataUnit<T> Index<T>(int index, T value)
        {
            return new UIDataUnit<T>(index, value);
        }
    }
    
    // 这是一个非泛型的接口，任何 DataUnit<T> 都要实现它
    public interface IUIDataUnit
    {
        string TargetId { get; }
        int TargetIndex { get; }
        
        /// <summary>
        /// 尝试将数据应用到组件上
        /// </summary>
        void TryApply(UIBindComponent component);
    }
    
    // 泛型结构体，避免GC（Struct）
    public readonly struct UIDataUnit<T> : IUIDataUnit
    {
        public bool UseIndex { get; }
        
        public string TargetId { get; }
        public int TargetIndex { get; }
        
        private readonly T _value;

        // 构造 ID 类型的数据
        public UIDataUnit(string id, T value)
        {
            TargetId    = id;
            _value      = value;
            TargetIndex = -1;
            UseIndex    = false;
        }

        // 构造 Index 类型的数据
        public UIDataUnit(int index, T value)
        {
            TargetId    = null;
            TargetIndex = index;
            _value      = value;
            UseIndex    = true;
        }

        public void TryApply(UIBindComponent component)
        {
            // 核心：这里进行类型检查。
            // 只有当组件是泛型组件，且泛型类型 T 与本数据的 T 一致时才执行。
            if (component is UIBindComponentGeneric<T> typedComponent)
            {
                typedComponent.SetData(_value);
            }
            else
            {
                // 可选：在这里加日志，提示类型不匹配（例如把 String 发给了 Image）
                // Debug.LogWarning($"类型不匹配: 组件 {component.name} 期望 {typeof(T)} 但并未实现对应泛型类。");
            }
        }
    }
}