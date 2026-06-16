using System;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// Attribute to draw a button in the Inspector for a method.
    /// Optional <c>name</c> overrides the displayed button label; if omitted, the method name is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InsButtonAttribute : Attribute
    {
        /// <summary>Inspector 上显示的按钮名称。为空时使用方法名。</summary>
        public string Name { get; }

        /// <summary>
        /// 是否在调用前执行 Undo.RecordObject。
        /// 仅当该方法会修改序列化字段时才应设为 true。
        /// </summary>
        public bool RecordUndo { get; }

        /// <summary>按钮在 Inspector 中的显示顺序，值越小越靠前。</summary>
        public int Order { get; }

        public InsButtonAttribute(string name = null, bool recordUndo = false, int order = 0)
        {
            Name = name;
            RecordUndo = recordUndo;
            Order = order;
        }
    }
}
