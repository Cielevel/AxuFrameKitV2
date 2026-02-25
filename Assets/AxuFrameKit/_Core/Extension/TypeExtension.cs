using System;
using System.Linq;

namespace AxuFrameKit._Core.Extension
{
    public static class TypeExtension
    {
        /// <summary>
        /// 获取类型的默认值 (equivalent to default(T))
        /// </summary>
        public static object GetDefaultValue(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// 检查类型是否实现了某个泛型接口
        /// </summary>
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
        public static bool IsNullableType(this Type type)
        {
            if (type == null) return false;
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// 获取 Nullable&lt;T&gt; 的底层类型；非 Nullable 时返回自身
        /// </summary>
        public static Type GetNonNullableType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        /// <summary>
        /// 是否是静态类
        /// </summary>
        public static bool IsStaticClass(this Type type)
        {
            if (type == null) return false;
            return type.IsClass && type.IsAbstract && type.IsSealed;
        }
    }
}
