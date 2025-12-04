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
    }
}
