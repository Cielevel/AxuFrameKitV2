using System;
using System.Linq;

namespace AxuFrameKit._Core.Extension
{
    public static class TypeExtension
    {
        /// <summary>
        /// 获取类型的默认值 (equivalent to default(T))
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>类型的默认值，若为引用类型则返回 null</returns>
        public static object GetDefaultValue(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// 检查类型是否实现了某个泛型接口
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <param name="interfaceType">要匹配的接口类型（可以是泛型定义）</param>
        /// <returns>如果实现了该接口则返回 true</returns>
        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            if (type == null || interfaceType == null) return false;

            if (interfaceType.IsGenericTypeDefinition)
            {
                return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
            }
            return interfaceType.IsAssignableFrom(type);
        }

        /// <summary>
        /// 是否是数字类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果是数值类型（含 Nullable）则返回 true</returns>
        public static bool IsNumeric(this Type type)
        {
            if (type == null) return false;

            type = Nullable.GetUnderlyingType(type) ?? type;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断类型是否为 Nullable&lt;T&gt;
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果是 Nullable 则返回 true</returns>
        public static bool IsNullableType(this Type type)
        {
            if (type == null) return false;
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// 获取 Nullable&lt;T&gt; 的底层类型；非 Nullable 时返回自身
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>基础类型</returns>
        public static Type GetNonNullableType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// 是否是静态类
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果是静态类则返回 true</returns>
        public static bool IsStaticClass(this Type type)
        {
            if (type == null) return false;
            return type.IsClass && type.IsAbstract && type.IsSealed;
        }
    }
}
