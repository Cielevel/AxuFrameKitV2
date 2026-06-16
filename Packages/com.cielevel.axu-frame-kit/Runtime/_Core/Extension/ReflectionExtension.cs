using System;
using System.Linq;
using System.Reflection;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Extension
{
    public static class ReflectionExtension
    {
        /// <summary>
        /// 获取目标对象的属性值
        /// </summary>
        /// <param name="obj">目标对象实例</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>属性的值，若未找到或对象为 null 则返回 null</returns>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null) return null;
            PropertyInfo prop = obj.GetType().GetProperty(propertyName);
            return prop?.GetValue(obj, null);
        }

        /// <summary>
        /// 获取目标对象的字段值（支持私有字段）
        /// </summary>
        /// <param name="obj">目标对象实例</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns>字段的值，若未找到或对象为 null 则返回 null</returns>
        public static object GetFieldValue(this object obj, string fieldName)
        {
            if (obj == null) return null;
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// 检查类型是否拥有指定类型的特性 (Attribute)
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">要检查的类型</param>
        /// <param name="inherit">是否检查继承链上的特性</param>
        /// <returns>如果存在该特性则返回 true</returns>
        public static bool HasAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.IsDefined(typeof(T), inherit);
        }

        /// <summary>
        /// 获取类型的特定特性实例
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">目标类型</param>
        /// <param name="inherit">是否检查继承链上的特性</param>
        /// <returns>特性实例，若不存在则返回 null</returns>
        public static T GetAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return (T)type.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
        }

        /// <summary>
        /// 检查某个对象是否是该类型的实例（支持接口、基类判断）
        /// </summary>
        /// <param name="obj">对象实例</param>
        /// <param name="type">目标类型</param>
        /// <returns>如果是该类型或其派生类/实现类的实例则返回 true</returns>
        public static bool IsInstanceOf(this object obj, Type type)
        {
            if (obj == null) return false;
            return type.IsInstanceOfType(obj);
        }

        /// <summary>
        /// 判断类型是否继承自另一个类型，支持接口继承检查
        /// </summary>
        /// <param name="type">派生类类型</param>
        /// <param name="baseType">基类或接口类型</param>
        /// <returns>如果是子类或实现了该接口则返回 true</returns>
        public static bool InheritsFrom(this Type type, Type baseType)
        {
            if (type == null) return false;
            if (baseType == null) return type.IsInterface;
            if (baseType.IsInterface) return baseType.IsAssignableFrom(type);
            return type.IsSubclassOf(baseType);
        }

        /// <summary>
        /// 判断类型是否继承自泛型类型定义 (e.g. List&lt;T&gt;)
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <param name="genericTypeDefinition">泛型定义类型（必须是 IsGenericTypeDefinition 为 true）</param>
        /// <returns>如果是该泛型定义的实例或派生类则返回 true</returns>
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
