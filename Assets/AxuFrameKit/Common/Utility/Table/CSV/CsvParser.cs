using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace AxuFrameKit.Common.Utility.Table.CSV
{
    /// <summary>
    /// CSV解析配置选项
    /// </summary>
    [Serializable]
    public class CsvParseOptions
    {
        /// <summary>字段分隔符，null表示自动检测</summary>
        [field: SerializeField]
        public char? Delimiter { get; set; } = null;
        
        /// <summary>文本限定符，用于包含特殊字符的字段</summary>
        [field: SerializeField]
        public char TextQualifier { get; set; } = '"';
        
        /// <summary>是否包含标题行</summary>
        [field: SerializeField]
        public bool HasHeaderRow { get; set; } = true;

        /// <summary>是否包含Tag行</summary>
        [field: SerializeField]
        public bool HasTagsRow { get; set; } = false;
        
        /// <summary>是否忽略空行</summary>
        [field: SerializeField]
        public bool IgnoreEmptyRows { get; set; } = true;
        
        /// <summary>是否去除字段前后空格</summary>
        [field: SerializeField]
        public bool TrimFields { get; set; } = true;
        
        /// <summary>数组分隔符（用于单个字段中的数组数据）</summary>
        [field: SerializeField]
        public char ArraySeparator { get; set; } = '|';
        
        /// <summary>文本编码</summary>
        [field: SerializeField]
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        
        /// <summary>日期时间格式</summary>
        [field: SerializeField]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        
        /// <summary>数字格式化信息</summary>
        [field: SerializeField]
        public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// 字段映射特性，用于指定CSV列与类属性的映射关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CsvColumnAttribute : Attribute
    {
        public string ColumnName { get; }
        public bool IsOptional { get; set; } = false;
        public string DefaultValue { get; set; } = null;

        public CsvColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
    
    /// <summary>
    /// 解析一行数据后，调用指定方法进行对对应成员进行最终处理（也就是一个类在完成构建后）
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)] // TODO 提供异步方法调用？
    public class FinalParseAttribute : Attribute
    {
        /// <summery>空参数方法/传递入当前类实例</summery>
        public string ParseMethod { get; set; }
        
        /// <summary>处理顺序，以保证可能存在的FinalParse顺序依赖；降序调用</summary>
        public int Order { get; set; }

        public FinalParseAttribute(string parseMethod, int order = 0)
        {
            ParseMethod = parseMethod;
            Order       = order;
        }
    }

    /// <summary>
    /// 自定义的ContractResolver，使CsvColumn特性具有JsonProperty的功能；
    /// - 使用时需要创建setting，将这个类的实例赋值给JsonSerializerSettings.ContractResolver，然后反序列化时传递。
    /// </summary>
    public class CsvColumnContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
        
            // 检查是否有CsvColumn特性
            var csvColumnAttribute = member.GetCustomAttribute<CsvColumnAttribute>();
            if (csvColumnAttribute != null)
            {
                // 使用CsvColumn特性中的名称作为JSON属性名
                property.PropertyName = csvColumnAttribute.ColumnName;
            }
        
            return property;
        }
    }
    
    /// <summary>
    /// CSV解析结果
    /// </summary>
    public class CsvParseResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string[] Headers { get; set; }
        public string[][] Tags { get; set; }
        public bool IsSuccess => Errors.Count == 0;
    }

    /// <summary>
    /// 先进的CSV解析器
    /// 支持复杂CSV格式、自动类型转换、灵活的映射规则
    /// </summary>
    public class CsvParser
    {
        private readonly CsvParseOptions _options;

        public CsvParser(CsvParseOptions options = null)
        {
            _options = options ?? new CsvParseOptions();
        }

        /// <summary>
        /// 解析CSV文本到指定类型的列表
        /// </summary>
        public CsvParseResult<T> Parse<T>(string csvContent) where T : new() // TODO 异步处理（以便于异步调用FinalParse）
        {
            var result = new CsvParseResult<T>();
            
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.Errors.Add("CSV内容为空");
                return result;
            }

            try
            {
                // 解析CSV为原始数据
                var rawData = ParseCsvToRawData(csvContent);
                if (rawData.Count == 0)
                {
                    result.Warnings.Add("CSV文件没有数据行");
                    return result;
                }

                // 获取标题行
                string[] headers = null;
                int dataStartIndex = 0;

                if (_options.HasHeaderRow && rawData.Count > 0)
                {
                    headers        = rawData[0];
                    dataStartIndex = 1;
                    result.Headers = headers;
                }

                // 获取tag行
                if (_options.HasTagsRow && rawData.Count > 0)
                {
                    var tags = rawData[1];
                    dataStartIndex = 2;
                    result.Tags    = tags.Select(e => e.Split(',')).ToArray();
                }

                // 获取映射信息
                var mappingInfo = GetMappingInfo<T>(headers);
                var finalParseMembers = GetFinalParseMembers<T>();

                // 转换数据
                for (int i = dataStartIndex; i < rawData.Count; i++)
                {
                    try
                    {
                        var obj = ConvertRowToObject<T>(rawData[i], mappingInfo, headers); // CRITICAL 如果有Header，要以Header的顺序为基础，而非mapping为基础
                        result.Data.Add(obj);
                        ConvertFinalParse(obj, finalParseMembers);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"第{i + 1}行转换失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"解析CSV时发生错误: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 解析CSV为原始字符串数组数据
        /// </summary>
        private List<string[]> ParseCsvToRawData(string csvContent)
        {
            var result = new List<string[]>();
            var delimiter = _options.Delimiter ?? DetectDelimiter(csvContent);

            using var reader = new StringReader(csvContent);
            while (reader.ReadLine() is { } line)
            {
                if (_options.IgnoreEmptyRows && string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = ParseCsvLine(line, delimiter, _options.TextQualifier);
                if (_options.TrimFields)
                    fields = fields.Select(f => f?.Trim()).ToArray();
                    
                result.Add(fields);
            }

            return result;
        }

        /// <summary>
        /// 解析单行CSV数据，正确处理引号和嵌套逗号
        /// </summary>
        private string[] ParseCsvLine(string line, char delimiter, char textQualifier)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == textQualifier)
                {
                    if (!insideQuotes)
                        insideQuotes = true; // 开始引号
                    else
                    {
                        // 检查是否是转义引号（两个连续的引号）
                        if (i + 1 < line.Length && line[i + 1] == textQualifier)
                        {
                            currentField.Append(textQualifier);
                            i++; // 跳过下一个引号
                        }
                        else
                        {
                            // 结束引号
                            insideQuotes = false;
                        }
                    }
                }
                else if (c == delimiter && !insideQuotes)
                {
                    // 字段分隔符
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    // 普通字符
                    currentField.Append(c);
                }
            }

            // 添加最后一个字段
            fields.Add(currentField.ToString());

            return fields.ToArray();
        }

        /// <summary>
        /// 自动检测CSV分隔符
        /// </summary>
        private char DetectDelimiter(string csvContent)
        {
            var possibleDelimiters = new[] { ',', ';', '\t', '|' };
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length == 0) return ',';

            // 取前几行进行分析
            var sampleLines = lines.Take(System.Math.Min(5, lines.Length)).ToArray();
            
            var delimiterCounts = possibleDelimiters.ToDictionary(d => d, d => 0);

            foreach (var line in sampleLines)
            {
                foreach (var delimiter in possibleDelimiters)
                {
                    delimiterCounts[delimiter] += line.Count(c => c == delimiter);
                }
            }

            // 返回出现频率最高且一致的分隔符
            return delimiterCounts.OrderByDescending(kv => kv.Value).First().Key;
        }

        /// <summary>
        /// 获取类型的映射信息
        /// </summary>
        private Dictionary<string, MemberInfo> GetMappingInfo<T>(string[] headers)
        {
            var mapping = new Dictionary<string, MemberInfo>();
            var type = typeof(T);

            // 获取所有公共字段和属性
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);

            var members = fields.Concat(properties.Cast<MemberInfo>());

            foreach (var member in members)
            {
                var csvAttribute = member.GetCustomAttribute<CsvColumnAttribute>();
                
                if (csvAttribute != null)
                {
                    // 使用特性指定的映射
                    if (!string.IsNullOrEmpty(csvAttribute.ColumnName))
                    {
                        mapping[csvAttribute.ColumnName] = member;
                    }
                }
                else if (headers != null)
                {
                    // 尝试按名称匹配
                    var matchingHeader = headers.FirstOrDefault(h => 
                        string.Equals(h, member.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingHeader != null)
                        mapping[matchingHeader] = member;
                }
            }

            return mapping;
        }
        
        private List<MemberInfo> GetFinalParseMembers<T>()
        {
            var type = typeof(T);
            return type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                       .Where(m => m.GetCustomAttribute<FinalParseAttribute>() != null)
                       .OrderByDescending(m => m.GetCustomAttribute<FinalParseAttribute>().Order)
                       .ToList();
        }

        /// <summary>
        /// 将CSV行转换为对象
        /// </summary>
        private T ConvertRowToObject<T>(string[] row, Dictionary<string, MemberInfo> mapping, string[] headers) where T : new()
        {
            var obj = new T();
            headers ??= _options.HasHeaderRow ? mapping.Keys.ToArray() : Enumerable.Range(0, row.Length).Select(i => i.ToString()).ToArray();

            for (int i = 0; i < System.Math.Min(row.Length, headers.Length); i++)
            {
                var header = headers[i];
                if (mapping.TryGetValue(header, out var member) == false) 
                    continue;

                var value = row[i];

                try
                {
                    object convertedValue = ConvertValue(value, GetMemberType(member), member);
                    SetMemberValue(obj, member, convertedValue);
                }
                catch (Exception ex)
                {
                    // 检查是否为可选字段
                    var csvAttribute = member.GetCustomAttribute<CsvColumnAttribute>();
                    if (csvAttribute?.IsOptional == true)
                    {
                        // 使用默认值
                        if (csvAttribute.DefaultValue != null)
                        {
                            var defaultValue = ConvertValue(csvAttribute.DefaultValue, GetMemberType(member), member);
                            SetMemberValue(obj, member, defaultValue);
                        }
                    }
                    else
                    {
                        throw new Exception($"转换字段'{header}'失败: {ex.Message}");
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// 转换字符串值到指定类型
        /// </summary>
        private object ConvertValue(string value, Type targetType, MemberInfo member)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    return Activator.CreateInstance(targetType);
                }
                return null;
            }

            // 处理可空类型
            var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
            if (nullableUnderlyingType != null)
            {
                targetType = nullableUnderlyingType;
            }

            // 数组类型处理
            if (targetType.IsArray)
            {
                return ConvertToArray(value, targetType.GetElementType());
            }

            // 枚举类型处理
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true);
            }

            // 基础类型转换
            return targetType.Name switch
            {
                nameof(Boolean) => ConvertToBoolean(value),
                nameof(DateTime) => DateTime.ParseExact(value, _options.DateTimeFormat, _options.CultureInfo),
                nameof(Single) => float.Parse(value, _options.CultureInfo),
                nameof(Double) => double.Parse(value, _options.CultureInfo),
                nameof(Decimal) => decimal.Parse(value, _options.CultureInfo),
                _ => Convert.ChangeType(value, targetType, _options.CultureInfo)
            };
        }

        /// <summary>
        /// 转换字符串到数组
        /// </summary>
        private object ConvertToArray(string value, Type elementType)
        {
            var parts = value.Split(_options.ArraySeparator);
            var array = Array.CreateInstance(elementType, parts.Length);
            
            for (int i = 0; i < parts.Length; i++)
            {
                var convertedElement = ConvertValue(parts[i].Trim(), elementType, null);
                array.SetValue(convertedElement, i);
            }

            return array;
        }

        /// <summary>
        /// 转换布尔值，支持多种格式
        /// </summary>
        private bool ConvertToBoolean(string value)
        {
            value = value.ToLower().Trim();
            return value switch
            {
                "true" or "1" or "yes" or "y" or "是" or "对" or "真" => true,
                "false" or "0" or "no" or "n" or "否" or "错" or "假" => false,
                _ => throw new FormatException($"无法将'{value}'转换为布尔值")
            };
        }
        
        private void ConvertFinalParse<T>(T obj, List<MemberInfo> finalParseMembers)
        {
            foreach (var memberInfo in finalParseMembers)
            {
                var finalParseAttr = memberInfo.GetCustomAttribute<FinalParseAttribute>();
                
                if (finalParseAttr == null || string.IsNullOrEmpty(finalParseAttr.ParseMethod)) 
                    continue;
                
                var type   = obj.GetType();
                var method = type.GetMethod(finalParseAttr.ParseMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                
                if (method == null) 
                    continue;
                
                method.Invoke(obj, method.GetParameters().Length == 0 ? null : new object[] { obj });
            }
        }

        /// <summary>
        /// 获取成员类型
        /// </summary>
        private Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => throw new ArgumentException("不支持的成员类型")
            };
        }

        /// <summary>
        /// 设置成员值
        /// </summary>
        private void SetMemberValue(object obj, MemberInfo member, object value)
        {
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(obj, value);
                    break;
                case PropertyInfo property:
                    property.SetValue(obj, value);
                    break;
                default:
                    throw new ArgumentException("不支持的成员类型");
            }
        }
    }
}