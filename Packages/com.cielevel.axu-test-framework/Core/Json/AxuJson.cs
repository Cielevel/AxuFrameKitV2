using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Json
{
    /// <summary>
    /// Newtonsoft.Json 薄封装（Core 唯一允许的外部依赖）：schema 自校验读写 + 深合并。
    /// 未知附加字段忽略（向前兼容）；schema 不匹配抛 <see cref="AxuSchemaException"/>。
    /// </summary>
    public static class AxuJson
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
        };

        /// <summary>统一序列化器（POCO 均带显式 JsonProperty）。</summary>
        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(Settings);

        /// <summary>从文件读取并校验 schema；<paramref name="expectedSchema"/> 传 null 跳过校验。</summary>
        public static T FromFile<T>(string path, string expectedSchema)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("路径不能为空", nameof(path));
            return ParseCore<T>(File.ReadAllText(path), expectedSchema, path);
        }

        /// <summary>从 JSON 字符串解析并校验 schema；<paramref name="expectedSchema"/> 传 null 跳过校验。</summary>
        public static T Parse<T>(string json, string expectedSchema)
        {
            return ParseCore<T>(json, expectedSchema, "(inline)");
        }

        /// <summary>序列化为 JSON 字符串（默认缩进；落盘 UTF-8 无 BOM 由写盘方保证）。</summary>
        public static string ToJson(object value, bool indented = true)
        {
            return JsonConvert.SerializeObject(value, indented ? Formatting.Indented : Formatting.None, Settings);
        }

        /// <summary>JObject → POCO（使用统一序列化器；null 返回 default）。</summary>
        public static T ToObject<T>(JObject obj)
        {
            if (obj == null) return default;
            using (var reader = obj.CreateReader())
            {
                return Serializer.Deserialize<T>(reader);
            }
        }

        /// <summary>
        /// 深合并：overlay 覆盖 target——子对象递归合并，标量/数组直接覆盖（profile 扇出时 overlay=profile，profile 优先）。
        /// </summary>
        public static void DeepMerge(JObject target, JObject overlay)
        {
            if (target == null || overlay == null) return;
            foreach (var property in overlay.Properties())
            {
                var overlayValue = property.Value;
                if (overlayValue != null && overlayValue.Type == JTokenType.Object && target[property.Name] is JObject targetChild)
                {
                    DeepMerge(targetChild, (JObject)overlayValue);
                }
                else
                {
                    target[property.Name] = overlayValue != null ? (JToken)overlayValue.DeepClone() : null;
                }
            }
        }

        static T ParseCore<T>(string json, string expectedSchema, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new AxuSchemaException($"场景数据为空：path={sourcePath}，期望 \"{expectedSchema ?? "(any)"}\"，实际 \"(empty)\"");

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                throw new AxuSchemaException(
                    $"场景数据 JSON 解析失败：path={sourcePath}，期望 \"{expectedSchema ?? "(any)"}\"，{ex.Message}", ex);
            }

            if (expectedSchema != null)
            {
                var actual = root["schema"] != null && root["schema"].Type == JTokenType.String
                    ? root["schema"].ToString()
                    : null;
                if (actual != expectedSchema)
                    throw new AxuSchemaException(sourcePath, expectedSchema, actual ?? "(missing)");
            }

            using (var reader = root.CreateReader())
            {
                return Serializer.Deserialize<T>(reader);
            }
        }
    }
}
