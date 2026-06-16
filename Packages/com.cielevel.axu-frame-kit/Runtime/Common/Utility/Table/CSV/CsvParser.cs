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

        /// <summary>是否包含Tag行（Tag行紧跟在Header行之后）</summary>
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
    /// 解析一行数据后，调用指定方法进行对对应成员进行最终处理（也就是一个类在完成构建后）。
    /// <para>
    /// 约定：ParseMethod 指向的方法必须是无参方法，或仅接受当前实例自身（类型为 T）作为唯一参数的方法。
    /// 其他签名将在运行时报错并跳过该调用。
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)] // TODO 提供异步方法调用？
    public class FinalParseAttribute : Attribute
    {
        /// <summary>空参数方法 / 传递入当前类实例的单参数方法</summary>
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
    /// 自定义的ContractResolver，使CsvColumn特性具有JsonProperty的功能。
    /// 使用时需要创建 JsonSerializerSettings 并将本类实例赋值给 ContractResolver，再传入反序列化方法。
    /// </summary>
    public class CsvColumnContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            var csvColumnAttribute = member.GetCustomAttribute<CsvColumnAttribute>();
            if (csvColumnAttribute != null)
                property.PropertyName = csvColumnAttribute.ColumnName;

            return property;
        }
    }

    /// <summary>
    /// CSV解析结果
    /// </summary>
    public class CsvParseResult<T>
    {
        public List<T>     Data     { get; set; } = new List<T>();
        public List<string> Errors  { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string[]    Headers  { get; set; }
        public string[][]  Tags     { get; set; }
        public bool IsSuccess => Errors.Count == 0;
    }

    /// <summary>
    /// 先进的CSV解析器。
    /// 支持复杂CSV格式、自动类型转换、灵活的映射规则。
    /// <para>
    /// 反射结果（成员映射、FinalParse 成员列表）按类型静态缓存，同类型重复解析时无额外反射开销。
    /// </para>
    /// </summary>
    public class CsvParser
    {
        private readonly CsvParseOptions _options;

        // ---- 静态反射缓存 ----
        // key = typeof(T)，在同一进程生命周期内，相同类型的映射信息只计算一次。
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> MappingCache= new Dictionary<Type, Dictionary<string, MemberInfo>>();

        private static readonly Dictionary<Type, List<MemberInfo>> FinalParseMemberCache= new Dictionary<Type, List<MemberInfo>>();

        private static readonly object CacheLock = new object();

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
                var rawData = ParseCsvToRawData(csvContent);
                if (rawData.Count == 0)
                {
                    result.Warnings.Add("CSV文件没有数据行");
                    return result;
                }

                string[] headers  = null;
                int dataStartIndex = 0;

                if (_options.HasHeaderRow)
                {
                    headers            = rawData[0];
                    dataStartIndex     = 1;
                    result.Headers     = headers;
                }

                // Tag行必须紧跟在Header行之后（或作为第一行，当没有Header时）
                if (_options.HasTagsRow)
                {
                    int tagRowIndex = dataStartIndex; // Header存在时为1，否则为0
                    if (tagRowIndex < rawData.Count)
                    {
                        var tags    = rawData[tagRowIndex];
                        result.Tags = tags.Select(e => e.Split(',')).ToArray();
                        dataStartIndex = tagRowIndex + 1;
                    }
                    else
                    {
                        result.Warnings.Add("配置了 HasTagsRow 但文件中不存在对应行，已跳过。");
                    }
                }

                var mappingInfo      = GetMappingInfo<T>(headers);
                var finalParseMembers = GetFinalParseMembers<T>();

                for (int i = dataStartIndex; i < rawData.Count; i++)
                {
                    try
                    {
                        var obj = ConvertRowToObject<T>(rawData[i], mappingInfo, headers);
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
            var result    = new List<string[]>();
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
            var fields       = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == textQualifier)
                {
                    if (!insideQuotes)
                    {
                        insideQuotes = true;
                    }
                    else
                    {
                        // 检查是否是转义引号（两个连续的引号）
                        if (i + 1 < line.Length && line[i + 1] == textQualifier)
                        {
                            currentField.Append(textQualifier);
                            i++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                }
                else if (c == delimiter && !insideQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

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

            var sampleLines     = lines.Take(System.Math.Min(5, lines.Length)).ToArray();
            var delimiterCounts = possibleDelimiters.ToDictionary(d => d, _ => 0);

            foreach (var line in sampleLines)
            foreach (var delimiter in possibleDelimiters)
                delimiterCounts[delimiter] += line.Count(c => c == delimiter);

            return delimiterCounts.OrderByDescending(kv => kv.Value).First().Key;
        }

        /// <summary>
        /// 获取类型的映射信息（带静态缓存，相同类型只反射一次）
        /// </summary>
        private Dictionary<string, MemberInfo> GetMappingInfo<T>(string[] headers)
        {
            var type = typeof(T);

            // 缓存命中时直接返回
            // 注意：缓存的 mapping 以成员名/ColumnName 为 key，与具体 headers 顺序无关，
            // 实际列定位由 ConvertRowToObject 通过 headers 数组下标完成。
            lock (CacheLock)
            {
                if (MappingCache.TryGetValue(type, out var cached))
                    return cached;
            }

            var mapping    = new Dictionary<string, MemberInfo>();
            var fields     = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanWrite);
            var members    = fields.Concat(properties.Cast<MemberInfo>());

            foreach (var member in members)
            {
                var csvAttribute = member.GetCustomAttribute<CsvColumnAttribute>();

                if (csvAttribute != null)
                {
                    if (!string.IsNullOrEmpty(csvAttribute.ColumnName))
                        mapping[csvAttribute.ColumnName] = member;
                }
                else if (headers != null)
                {
                    var matchingHeader = headers.FirstOrDefault(h =>
                        string.Equals(h, member.Name, StringComparison.OrdinalIgnoreCase));

                    if (matchingHeader != null)
                        mapping[matchingHeader] = member;
                }
            }

            lock (CacheLock)
            {
                // 二次检查，防止并发时重复写入
                if (!MappingCache.ContainsKey(type))
                    MappingCache[type] = mapping;
            }

            return mapping;
        }

        /// <summary>
        /// 获取带 FinalParse 特性的成员列表（带静态缓存）
        /// </summary>
        private List<MemberInfo> GetFinalParseMembers<T>()
        {
            var type = typeof(T);

            lock (CacheLock)
            {
                if (FinalParseMemberCache.TryGetValue(type, out var cached))
                    return cached;
            }

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<FinalParseAttribute>() != null)
                .OrderByDescending(m => m.GetCustomAttribute<FinalParseAttribute>().Order)
                .ToList();

            lock (CacheLock)
            {
                if (!FinalParseMemberCache.ContainsKey(type))
                    FinalParseMemberCache[type] = members;
            }

            return members;
        }

        /// <summary>
        /// 将CSV行转换为对象
        /// </summary>
        private T ConvertRowToObject<T>(string[] row, Dictionary<string, MemberInfo> mapping, string[] headers) where T : new()
        {
            var obj = new T();
            headers ??= _options.HasHeaderRow
                ? mapping.Keys.ToArray()
                : Enumerable.Range(0, row.Length).Select(i => i.ToString()).ToArray();

            for (int i = 0; i < System.Math.Min(row.Length, headers.Length); i++)
            {
                var header = headers[i];
                if (!mapping.TryGetValue(header, out var member))
                    continue;

                var value = row[i];

                try
                {
                    object convertedValue = ConvertValue(value, GetMemberType(member), member);
                    SetMemberValue(obj, member, convertedValue);
                }
                catch (Exception ex)
                {
                    var csvAttribute = member.GetCustomAttribute<CsvColumnAttribute>();
                    if (csvAttribute?.IsOptional == true)
                    {
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
                    return Activator.CreateInstance(targetType);
                return null;
            }

            // 处理可空类型
            var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
            if (nullableUnderlyingType != null)
                targetType = nullableUnderlyingType;

            // 数组类型处理
            if (targetType.IsArray)
                return ConvertToArray(value, targetType.GetElementType());

            // 枚举类型处理
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            // 基础类型转换
            return targetType.Name switch
            {
                nameof(Boolean)  => ConvertToBoolean(value),
                nameof(DateTime) => DateTime.ParseExact(value, _options.DateTimeFormat, _options.CultureInfo),
                nameof(Single)   => float.Parse(value, _options.CultureInfo),
                nameof(Double)   => double.Parse(value, _options.CultureInfo),
                nameof(Decimal)  => decimal.Parse(value, _options.CultureInfo),
                _                => Convert.ChangeType(value, targetType, _options.CultureInfo)
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

        /// <summary>
        /// 执行 FinalParse 标记的方法。
        /// 仅支持无参方法，或接受当前对象自身作为唯一参数的方法。
        /// 不符合约定的签名将记录错误并跳过，不会抛出异常中断整体解析。
        /// </summary>
        private void ConvertFinalParse<T>(T obj, List<MemberInfo> finalParseMembers)
        {
            foreach (var memberInfo in finalParseMembers)
            {
                var finalParseAttr = memberInfo.GetCustomAttribute<FinalParseAttribute>();

                if (finalParseAttr == null || string.IsNullOrEmpty(finalParseAttr.ParseMethod))
                    continue;

                var type   = obj.GetType();
                var method = type.GetMethod(
                    finalParseAttr.ParseMethod,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );

                if (method == null)
                {
                    Debug.LogWarning($"[CsvParser] FinalParse: 在类型 '{type.Name}' 中找不到方法 '{finalParseAttr.ParseMethod}'，已跳过。");
                    continue;
                }

                var parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    method.Invoke(obj, null);
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(type))
                {
                    method.Invoke(obj, new object[] { obj });
                }
                else
                {
                    // 签名不符合约定，记录错误但继续解析其他行
                    Debug.LogError(
                        $"[CsvParser] FinalParse: 方法 '{type.Name}.{finalParseAttr.ParseMethod}' 的签名不符合约定。" +
                        $"仅支持无参方法，或接受 '{type.Name}' 实例作为唯一参数的方法。已跳过。"
                    );
                }
            }
        }

        /// <summary>
        /// 获取成员类型
        /// </summary>
        private Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field       => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _                     => throw new ArgumentException("不支持的成员类型")
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