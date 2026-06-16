using System;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// 在Inspector中绘制描述，适用于CustomMonoBehaivour继承者
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InspectorIntroAttribute : Attribute
    {
        public string Description { get; private set; }

        public InspectorIntroAttribute(string description)
        {
            Description = description;
        }
    }
}