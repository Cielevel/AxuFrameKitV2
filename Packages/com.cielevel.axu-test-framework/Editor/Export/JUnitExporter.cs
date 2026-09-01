using System.IO;
using System.Text;
using AxuTestFramework.Core.Report;

namespace AxuTestFramework.Editor.Export
{
    /// <summary>
    /// ScenarioReport → JUnit XML 导出器（方案 §6.2：CI 门禁上报格式，C# 直出，无 xslt 依赖）。
    /// <para>映射规则：testsuite name = suiteId，每条 <see cref="ScenarioReportResult"/> 一个 testcase；
    /// status=pass 仅带 time（=DurationMs 秒）；warn/fail 产出 <c>&lt;failure&gt;</c>、error 产出
    /// <c>&lt;error&gt;</c>，message 取首条 error/warn 级 finding 摘要（无 finding 时退回 status 原文）；
    /// 其余未知状态按 <c>&lt;skipped&gt;</c> 处理。文本经 XML 转义，time 保留毫秒精度。</para>
    /// </summary>
    public static class JUnitExporter
    {
        /// <summary>导出为 JUnit XML 字符串（带 xml 声明，UTF-8 无 BOM 由写盘方保证）。</summary>
        public static string Export(ScenarioReport report, string suiteId)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            sb.Append($"<testsuite name=\"{Escape(string.IsNullOrEmpty(suiteId) ? report?.SuiteId : suiteId)}\"");
            sb.Append($" tests=\"{CountResults(report)}\"");
            sb.Append($" failures=\"{CountByStatus(report, "warn") + CountByStatus(report, "fail")}\"");
            sb.Append($" errors=\"{CountByStatus(report, "error")}\"");
            sb.Append($" skipped=\"{CountSkipped(report)}\"");
            sb.Append($" time=\"{FormatSeconds(TotalDurationMs(report))}\">\n");

            if (report?.Results != null)
            {
                foreach (var result in report.Results)
                {
                    if (result == null) continue;
                    sb.Append($"  <testcase classname=\"{Escape(string.IsNullOrEmpty(suiteId) ? report.SuiteId : suiteId)}\"");
                    sb.Append($" name=\"{Escape(result.ScenarioId)}\"");
                    sb.Append($" time=\"{FormatSeconds(result.DurationMs)}\"");

                    var message = FirstProblemMessage(result);
                    if (message == null)
                    {
                        sb.Append(" />\n");
                        continue;
                    }

                    sb.Append(">\n");
                    if (message.Value.isSkipped)
                        sb.Append($"    <skipped message=\"{Escape(message.Value.text)}\" />\n");
                    else if (message.Value.isError)
                        sb.Append($"    <error message=\"{Escape(message.Value.text)}\"><![CDATA[{Cdata(message.Value.text)}]]></error>\n");
                    else
                        sb.Append($"    <failure message=\"{Escape(message.Value.text)}\"><![CDATA[{Cdata(message.Value.text)}]]></failure>\n");
                    sb.Append("  </testcase>\n");
                }
            }

            sb.Append("</testsuite>\n");
            return sb.ToString();
        }

        /// <summary>导出并写盘（自动建目录，UTF-8 无 BOM），返回写入路径。</summary>
        public static string ExportToFile(ScenarioReport report, string suiteId, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, Export(report, suiteId), new UTF8Encoding(false));
            return path;
        }

        static string Cdata(string text)
        {
            return (text ?? string.Empty).Replace("]]>", "]]]]><![CDATA[>");
        }

        static (string text, bool isSkipped, bool isError)? FirstProblemMessage(ScenarioReportResult result)
        {
            var status = (result.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (status == "pass")
                return null;

            if (status != "warn" && status != "fail" && status != "error")
                return ($"[{result.Status}] skipped", true, false);

            // 首条 error 级 finding；无则首条 warn 级；再无退回 status 原文
            Finding firstError = null, firstWarn = null;
            if (result.Findings != null)
            {
                foreach (var finding in result.Findings)
                {
                    if (finding == null) continue;
                    if (finding.Severity == FindingSeverity.Error && firstError == null) firstError = finding;
                    if (finding.Severity == FindingSeverity.Warning && firstWarn == null) firstWarn = finding;
                }
            }
            var picked = firstError ?? firstWarn;
            var text = picked != null
                ? $"[{picked.SeverityText}] {picked.Code}: {picked.Message}"
                : $"[{result.Status}] {result.Capability} scenario 未通过（无结构化 finding）";
            return (text, false, status == "error");
        }

        static int CountResults(ScenarioReport report)
        {
            var count = 0;
            if (report?.Results != null)
                foreach (var result in report.Results)
                    if (result != null) count++;
            return count;
        }

        static int CountByStatus(ScenarioReport report, string status)
        {
            var count = 0;
            if (report?.Results != null)
                foreach (var result in report.Results)
                    if (result != null && (result.Status ?? "").Trim().ToLowerInvariant() == status)
                        count++;
            return count;
        }

        static int CountSkipped(ScenarioReport report)
        {
            var count = 0;
            if (report?.Results != null)
                foreach (var result in report.Results)
                {
                    if (result == null) continue;
                    var status = (result.Status ?? string.Empty).Trim().ToLowerInvariant();
                    if (status != "pass" && status != "warn" && status != "fail" && status != "error")
                        count++;
                }
            return count;
        }

        static double TotalDurationMs(ScenarioReport report)
        {
            var total = 0d;
            if (report?.Results != null)
                foreach (var result in report.Results)
                    if (result != null) total += result.DurationMs;
            return total;
        }

        static string FormatSeconds(double durationMs)
        {
            return (durationMs / 1000d).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                switch (c)
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
