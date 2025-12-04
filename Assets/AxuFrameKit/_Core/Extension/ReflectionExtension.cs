using System;
using System.Linq;
using System.Reflection;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Extension
{
    public static class ReflectionExtension
    {
        /// <summary>
        /// 获取对象属性值
        /// </summary>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null) return null;
            PropertyInfo prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj, null);
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        public static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null) return null;
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// 检查类型是否拥有特定特性
        /// </summary>
        public static bool HasAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.IsDefined(typeof(T), inherit);
        }

        /// <summary>
        /// 获取类型的特定特性
        /// </summary>
        public static T GetAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
        }

        /// <summary>
        /// 检查某个对象是否是该类型的实例
        /// </summary>
        public static bool IsInstanceOf(this object obj, Type type)
        {
            if (obj == null) return false;
            return type.IsInstanceOfType(obj);
        }

        /// <summary>
        /// 判断类型是否继承自另一个类型
        /// </summary>
        public static bool InheritsFrom(this Type type, Type baseType)
        {
            if (type == null) return false;
            if (baseType == null) return type.IsInterface;
            if (baseType.IsInterface) return baseType.IsAssignableFrom(type);
            return type.IsSubclassOf(baseType);
        }

        /// <summary>
        /// 判断类型是否继承自泛型类型 (e.g. List<T>)
        /// </summary>
        public static bool InheritsFromGeneric(this Type type, Type genericTypeDefinition)
        {
            if (type == null || genericTypeDefinition == null) return false;
            if (!genericTypeDefinition.IsGenericTypeDefinition) return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
                return true;

            Type baseType = type.BaseType;
            if (baseType == null) return false;

            return baseType.InheritsFromGeneric(genericTypeDefinition);
        }
    }
}
