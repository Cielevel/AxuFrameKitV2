using AxuFrameKit.Common.UI.Kit.DataBind;

namespace AxuFrameKit.Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView 数据构建器
    /// 提供便捷的方法来创建用于数据绑定的 UIDataUnit 数组
    /// 
    /// 使用示例：
    /// <code>
    ///     var data = new ListViewDataBuilder()
    ///         .SetData(UIData.Id("title", "物品名称"), UIData.Id("icon", sprite))
    ///         .SetData("count", 99)
    ///         .SetData(0, "索引0的数据")
    ///         .Build();
    /// </code>
    /// </summary>
    public class ListViewDataBuilder
    {
        private readonly System.Collections.Generic.List<IUIDataUnit> _units;

        public ListViewDataBuilder()
        {
            _units = new System.Collections.Generic.List<IUIDataUnit>();
        }

        /// <summary>
        /// 设置 ID 绑定的数据
        /// </summary>
        public ListViewDataBuilder Set<T>(string id, T value)
        {
            _units.Add(UIData.Id(id, value));
            return this;
        }

        /// <summary>
        /// 设置 Index 绑定的数据
        /// </summary>
        public ListViewDataBuilder Set<T>(int index, T value)
        {
            _units.Add(UIData.Index(index, value));
            return this;
        }

        /// <summary>
        /// 批量设置数据
        /// </summary>
        public ListViewDataBuilder SetData(params IUIDataUnit[] units)
        {
            if (units != null)
            {
                _units.AddRange(units);
            }
            return this;
        }

        /// <summary>
        /// 设置 ID 绑定的数据（Set 的别名）
        /// </summary>
        public ListViewDataBuilder SetData<T>(string id, T value)
        {
            return Set(id, value);
        }

        /// <summary>
        /// 设置 Index 绑定的数据（Set 的别名）
        /// </summary>
        public ListViewDataBuilder SetData<T>(int index, T value)
        {
            return Set(index, value);
        }

        /// <summary>
        /// 添加自定义数据单元
        /// </summary>
        public ListViewDataBuilder Add(IUIDataUnit unit)
        {
            _units.Add(unit);
            return this;
        }

        /// <summary>
        /// 构建数据单元数组
        /// </summary>
        public IUIDataUnit[] Build()
        {
            return _units.ToArray();
        }

        /// <summary>
        /// 隐式转换为数组
        /// </summary>
        public static implicit operator IUIDataUnit[](ListViewDataBuilder builder)
        {
            return builder.Build();
        }
    }

    /// <summary>
    /// 静态辅助类，提供更简洁的语法
    /// </summary>
    public static class ListViewData
    {
        /// <summary>
        /// 创建新的数据构建器
        /// </summary>
        public static ListViewDataBuilder Create() => new ListViewDataBuilder();

        /// <summary>
        /// 快速创建单条 ID 绑定数据
        /// </summary>
        public static IUIDataUnit[] Id<T>(string id, T value)
        {
            return new IUIDataUnit[] { UIData.Id(id, value) };
        }

        /// <summary>
        /// 快速创建单条 Index 绑定数据
        /// </summary>
        public static IUIDataUnit[] Index<T>(int index, T value)
        {
            return new IUIDataUnit[] { UIData.Index(index, value) };
        }

        /// <summary>
        /// 批量创建数据单元数组
        /// </summary>
        public static IUIDataUnit[] SetData(params IUIDataUnit[] units)
        {
            return units;
        }

        /// <summary>
        /// 快速创建单条 ID 绑定数据（Id 的别名）
        /// </summary>
        public static IUIDataUnit[] SetData<T>(string id, T value)
        {
            return Id(id, value);
        }

        /// <summary>
        /// 快速创建单条 Index 绑定数据（Index 的别名）
        /// </summary>
        public static IUIDataUnit[] SetData<T>(int index, T value)
        {
            return Index(index, value);
        }
    }
}
