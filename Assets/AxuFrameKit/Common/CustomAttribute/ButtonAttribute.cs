using System;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// Attribute to draw a button in the Inspector for a method.
    /// Optional <c>name</c> overrides the displayed button label; if omitted, the method name is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class InsButtonAttribute : Attribute
    {
        /// <summary>
        /// Display name of the button. If null or empty, the method name will be used.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new ButtonAttribute.
        /// </summary>
        /// <param name="name">Optional button label.</param>
        public InsButtonAttribute(string name = null)
        {
            Name = name;
        }
    }
}
