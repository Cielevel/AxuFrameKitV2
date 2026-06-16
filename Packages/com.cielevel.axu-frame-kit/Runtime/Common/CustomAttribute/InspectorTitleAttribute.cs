using System;

namespace AxuFrameKit.Common.CustomAttribute
{
    /// <summary>
    /// 在Inspector中绘制大标题，适用于CustomMonoBehaivour继承者
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class InspectorTitleAttribute : Attribute
    {
        public string Title { get; private set; }
        
        public int FontSize { get; private set; }
        
        public InspectorTitleAttribute(string title, int fontSize = 40)
        {
            Title = title;
            FontSize = fontSize;
        }
    }
}