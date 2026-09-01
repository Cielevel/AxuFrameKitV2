using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using AxuTestFramework.Core.Baseline;
using AxuTestFramework.Core.Json;
using AxuTestFramework.Core.Report;

namespace AxuTestFramework.Core.Trend
{
    /// <summary>
    /// 趋势 CSV 存储（纯 C#，schema axu-trend/1）：首行为 schema header，后续每行一个
    /// <see cref="TrendPoint"/>（字段顺序固定，RFC 4180 转义：含逗号/引号/换行的字段加引号并双写引号）。
    /// </summary>
    public static class TrendStore
    {
        /// <summary>趋势 CSV 的 schema header（首行，即 <see cref="SchemaNames.Trend"/>）。</summary>
        public const string SchemaHeader = SchemaNames.Trend;

        /// <summary>追加单行（文件不存在则先写 header）。</summary>
        public static void Append(string path, TrendPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            Append(path, new[] { point });
        }

        /// <summary>批量追加（文件不存在则先写 header；空集合不产生文件）。</summary>
        public static void Append(string path, IEnumerable<TrendPoint> points)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("路径不能为空", nameof(path));
            if (points == null) throw new ArgumentNullException(nameof(points));

            var sb = new StringBuilder();
            foreach (var point in points)
            {
                if (point == null) continue;
                sb.Append(Escape(point.TimestampUtc)).Append(',')
                  .Append(Escape(point.Suite)).Append(',')
                  .Append(Escape(point.Scenario)).Append(',')
                  .Append(Escape(point.Capability)).Append(',')
                  .Append(Escape(point.Result)).Append(',')
                  .Append(point.ExitCode.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(point.DurationMs.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                  .Append(Escape(point.Metric)).Append(',')
                  .Append(point.Value.HasValue
                      ? point.Value.Value.ToString("R", CultureInfo.InvariantCulture)
                      : string.Empty)
                  .Append('\n');
            }
            if (sb.Length == 0) return;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, SchemaHeader + "\n" + sb, new UTF8Encoding(false));
            }
            else
            {
                File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
            }
        }

        /// <summary>
        /// 读取全部行：文件缺失返回空表；首行 header 与 <see cref="SchemaHeader"/> 不符抛
        /// <see cref="AxuSchemaException"/>。健壮解析（支持带引号字段；列数不足/值非法的行跳过）。
        /// </summary>
        public static List<TrendPoint> Read(string path)
        {
            var points = new List<TrendPoint>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return points;

            var lines = SplitLogicalLines(File.ReadAllText(path));
            var header = lines.Count > 0 ? lines[0].Trim() : "(missing)";
            if (header != SchemaHeader)
            {
                throw new AxuSchemaException(path, SchemaHeader, header);
            }

            for (var i = 1; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var fields = SplitLine(lines[i]);
                if (fields.Count < 9) continue;

                double durationMs;
                double value;
                points.Add(new TrendPoint
                {
                    TimestampUtc = fields[0],
                    Suite = fields[1],
                    Scenario = fields[2],
                    Capability = fields[3],
                    Result = fields[4],
                    ExitCode = int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var exitCode) ? exitCode : 0,
                    DurationMs = double.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out durationMs) ? durationMs : 0d,
                    Metric = fields[7],
                    // 空串或非法值（run 级行 Value 列）按 null；run 级行用 Metric=="" 区分，Value 字段本身不承载判别
                    Value = double.TryParse(fields[8], NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : (double?)null,
                });
            }
            return points;
        }

        /// <summary>
        /// 从报告扁平化产出趋势行：每条 metric（<see cref="BaselineDiff.Flatten"/> 同款扁平键）一行 +
        /// 每条 scenario 一行 Metric = ""、Value = null 的 run 级行（携带 result/exitCode/durationMs）。
        /// </summary>
        public static IEnumerable<TrendPoint> FromReport(ScenarioReport report, string suiteId)
        {
            if (report == null) yield break;
            var timestamp = string.IsNullOrEmpty(report.StartedAt) ? DateTime.UtcNow.ToString("o") : report.StartedAt;
            var suite = string.IsNullOrEmpty(suiteId) ? report.SuiteId : suiteId;
            var exitCode = (int)report.ExitCode;

            if (report.Results == null) yield break;
            foreach (var result in report.Results)
            {
                if (result == null) continue;

                // 指标行：扁平键与 BaselineDiff.Flatten 完全一致（collector.stat，custom 通道 stat 自带点保留）
                if (result.Metrics != null)
                {
                    foreach (var flat in BaselineDiff.Flatten(result.Metrics))
                    {
                        yield return new TrendPoint
                        {
                            TimestampUtc = timestamp,
                            Suite = suite,
                            Scenario = result.ScenarioId,
                            Capability = result.Capability,
                            Result = result.Status,
                            ExitCode = exitCode,
                            DurationMs = result.DurationMs,
                            Metric = flat.Key,
                            Value = flat.Value,
                        };
                    }
                }

                // run 级行：整体结果
                yield return new TrendPoint
                {
                    TimestampUtc = timestamp,
                    Suite = suite,
                    Scenario = result.ScenarioId,
                    Capability = result.Capability,
                    Result = result.Status,
                    ExitCode = exitCode,
                    DurationMs = result.DurationMs,
                    Metric = string.Empty,
                    Value = null,
                };
            }
        }

        static string Escape(string field)
        {
            field = field ?? string.Empty;
            if (field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return field;
            return '"' + field.Replace("\"", "\"\"") + '"';
        }

        /// <summary>
        /// 把整段 CSV 文本切为逻辑行：引号字段内的换行（RFC 4180 合法）不换行，
        /// 行尾 \r\n / \n / \r 均按行分隔；结尾空行不产生额外记录。
        /// </summary>
        static List<string> SplitLogicalLines(string text)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    sb.Append(c);
                    if (c == '"') inQuotes = false;
                }
                else if (c == '"')
                {
                    inQuotes = true;
                    sb.Append(c);
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    lines.Add(sb.ToString());
                    sb.Length = 0;
                }
                else sb.Append(c);
            }
            if (sb.Length > 0) lines.Add(sb.ToString());
            return lines;
        }

        /// <summary>单行 CSV 解析（支持带引号/双写引号的字段）。</summary>
        static List<string> SplitLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Length = 0; }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
