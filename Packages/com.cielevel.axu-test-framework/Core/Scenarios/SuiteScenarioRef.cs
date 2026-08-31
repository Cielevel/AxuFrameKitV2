using System;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxuTestFramework.Core.Scenarios
{
    /// <summary>
    /// suite.scenarios[] 条目：字符串形态（引用 id）或对象形态（ref+profile 引用，或完整内联 scenario 对象）。
    /// 字符串形态读取时映射到 <see cref="Ref"/>。
    /// </summary>
    [JsonConverter(typeof(SuiteScenarioRef.Converter))]
    public class SuiteScenarioRef
    {
        /// <summary>引用的 scenario id（ref+profile 扇出时与 profile 搭配）。</summary>
        [JsonProperty("ref")] public string Ref;

        /// <summary>环境档名（可选），对应 suite.profiles 键；扇出后 id 追加 "@profile" 后缀。</summary>
        [JsonProperty("profile")] public string Profile;

        /// <summary>完整内联 scenario 对象（与 Ref 互斥）。</summary>
        [JsonIgnore] public ScenarioDef Inline;

        /// <summary>本条目的基准 id：引用形态取 Ref，内联形态取 Inline.Id。</summary>
        public string EffectiveId => !string.IsNullOrEmpty(Ref) ? Ref : Inline != null ? Inline.Id : null;

        /// <summary>是否内联完整 scenario 定义。</summary>
        public bool IsInline => Inline != null;

        /// <summary>Newtonsoft 自定义读取：JSON 字符串 → Ref；对象带 "ref" → Ref+Profile；否则按完整 scenario 内联解析。</summary>
        class Converter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(SuiteScenarioRef);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;

                if (reader.TokenType == JsonToken.String)
                    return new SuiteScenarioRef { Ref = reader.Value as string };

                if (reader.TokenType == JsonToken.StartObject)
                {
                    var obj = JObject.Load(reader);
                    var refToken = obj["ref"];
                    if (refToken != null && refToken.Type != JTokenType.Null)
                        return new SuiteScenarioRef { Ref = refToken.ToString(), Profile = obj["profile"] != null ? obj["profile"].ToString() : null };
                    return new SuiteScenarioRef { Inline = AxuJson.ToObject<ScenarioDef>(obj) };
                }

                throw new JsonSerializationException($"scenarios 元素类型不支持：{reader.TokenType}（支持字符串或对象）");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entry = (SuiteScenarioRef)value;
                if (entry != null && entry.Inline != null)
                {
                    serializer.Serialize(writer, entry.Inline);
                    return;
                }

                writer.WriteStartObject();
                writer.WritePropertyName("ref");
                writer.WriteValue(entry != null ? entry.Ref : null);
                if (!string.IsNullOrEmpty(entry != null ? entry.Profile : null))
                {
                    writer.WritePropertyName("profile");
                    writer.WriteValue(entry.Profile);
                }
                writer.WriteEndObject();
            }
        }
    }
}
