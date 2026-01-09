using System;
using Newtonsoft.Json;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    /// <summary>
    /// 自定义JSON转换器，用于将数字0和1转换为布尔值false和true
    /// </summary>
    public class NumberToBooleanConverter : JsonConverter
    {
        /// <summary>
        /// 确定此实例是否可以转换指定的对象类型
        /// </summary>
        /// <param name="objectType">要转换的对象的类型</param>
        /// <returns>如果此实例可以转换指定类型，则为true；否则为false</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool) || objectType == typeof(bool?);
        }

        /// <summary>
        /// 读取JSON表示并返回一个布尔对象
        /// </summary>
        /// <param name="reader">JSON读取器</param>
        /// <param name="objectType">目标对象类型</param>
        /// <param name="existingValue">正在读取的对象的现有值</param>
        /// <param name="serializer">调用的序列化器实例</param>
        /// <returns>转换后的布尔对象</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType == typeof(bool?))
                    return null;
                return false;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToInt32(reader.Value) != 0;
            }

            if (reader.TokenType == JsonToken.Boolean)
            {
                return reader.Value;
            }

            if (reader.TokenType == JsonToken.String)
            {
                string stringValue = reader.Value.ToString();
                
                // 尝试解析字符串为数字
                if (int.TryParse(stringValue, out int intValue))
                {
                    return intValue != 0;
                }
                
                // 尝试解析字符串为布尔值
                if (bool.TryParse(stringValue, out bool boolValue))
                {
                    return boolValue;
                }
            }

            throw new JsonSerializationException($"无法将 {reader.TokenType} 转换为布尔值");
        }

        /// <summary>
        /// 将布尔值写入JSON表示
        /// </summary>
        /// <param name="writer">JSON写入器</param>
        /// <param name="value">要写入的值</param>
        /// <param name="serializer">调用的序列化器实例</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
