using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AxuTestFramework.Core.Json;
using Newtonsoft.Json;

namespace AxuTestFramework.Core.Baseline
{
    /// <summary>
    /// 基线文件（schema axu-baseline/1）：单 scenario 的指标基线，扁平两段键（&lt;collector&gt;.&lt;stat&gt;）→ 聚合值。
    /// 由 `-axuBaselineSave` 产出（&lt;目录&gt;/&lt;scenarioId&gt;.baseline.json），
    /// scenario 的 `baseline` 字段引用后每次运行自动 diff（见 DOC/ScenarioSchema.md §5）。
    /// </summary>
    public class BaselineFile
    {
        /// <summary>schema 常量。</summary>
        public const string SchemaName = "axu-baseline/1";

        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>自描述 schema，恒 axu-baseline/1。</summary>
        [JsonProperty("schema")] public string Schema = SchemaName;

        /// <summary>基线所属 scenario id。</summary>
        [JsonProperty("scenarioId")] public string ScenarioId;

        /// <summary>生成时间（UTC ISO 8601；Save 时空缺自动补当前时间）。</summary>
        [JsonProperty("createdAtUtc")] public string CreatedAtUtc;

        /// <summary>指标基线：扁平两段键（&lt;collector&gt;.&lt;stat&gt;，如 fps.avg）→ 聚合值。</summary>
        [JsonProperty("metrics")] public Dictionary<string, double> Metrics = new Dictionary<string, double>();

        /// <summary>
        /// 从文件加载（schema 校验：缺失或不符抛 <see cref="AxuSchemaException"/>；
        /// 文件不存在由内层 File.ReadAllText 抛 FileNotFoundException）。
        /// </summary>
        public static BaselineFile Load(string path)
        {
            return AxuJson.FromFile<BaselineFile>(path, SchemaName);
        }

        /// <summary>把基线落盘（自动建目录，UTF-8 无 BOM，缩进 JSON；CreatedAtUtc 空缺时补当前 UTC 时间）。</summary>
        public static void Save(string path, BaselineFile baseline)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("路径不能为空", nameof(path));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));

            if (string.IsNullOrEmpty(baseline.CreatedAtUtc)) baseline.CreatedAtUtc = UtcNowText();

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, AxuJson.ToJson(baseline), Utf8NoBom);
        }

        static string UtcNowText()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }
    }
}
