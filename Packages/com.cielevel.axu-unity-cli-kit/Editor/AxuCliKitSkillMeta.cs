using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AxuCliKit.Editor
{
    /// <summary>
    /// Minimal reader for the skill file format: YAML frontmatter plus markdown body.
    /// Deliberately dependency-free — it only needs `name`, `description` and the title.
    /// </summary>
    internal sealed class AxuCliKitSkillMeta
    {
        public string Name;
        public string Description;
        public string Title;

        private const string FallbackRules =
            "## Hard rules\n\n" +
            "1. Run `unity status` (and `unity command`) before planning anything that touches Unity.\n" +
            "2. Never hand-edit `.unity` / `.prefab` / `.asset` YAML while a live Editor is reachable — drive it through `unity command`.\n" +
            "3. Run `unity command set_autotick --enable true` before a recompile or a test run.\n" +
            "4. Async commands are trigger-then-poll: `recompile` → `recompile_status`, `run_tests --async_tests` → `test_status`.\n" +
            "5. Read failures from stdout (`success`, `errors[0].code`) and the exit code — `unity test` exits 8 when tests ran and failed, 6 when the run never produced a verdict.\n";

        public static AxuCliKitSkillMeta Parse(string raw)
        {
            var meta = new AxuCliKitSkillMeta();
            if (string.IsNullOrEmpty(raw))
                return meta;

            var lines = raw.Replace("\r\n", "\n").Split('\n');
            var index = 0;

            if (lines.Length > 0 && lines[0].Trim() == "---")
            {
                index = 1;
                string currentKey = null;
                var value = new StringBuilder();

                for (; index < lines.Length; index++)
                {
                    var line = lines[index];
                    if (line.Trim() == "---")
                    {
                        index++;
                        break;
                    }

                    var match = Regex.Match(line, @"^([A-Za-z0-9_-]+):\s*(.*)$");
                    if (match.Success)
                    {
                        Flush(meta, currentKey, value);
                        currentKey = match.Groups[1].Value;
                        value.Length = 0;
                        value.Append(match.Groups[2].Value);
                    }
                    else if (currentKey != null && line.TrimStart().StartsWith("-", StringComparison.Ordinal))
                    {
                        // list entry (allowed-tools) — not needed
                    }
                    else if (currentKey != null && line.StartsWith(" ", StringComparison.Ordinal) && line.Trim().Length > 0)
                    {
                        value.Append(' ').Append(line.Trim());
                    }
                }

                Flush(meta, currentKey, value);
            }

            for (; index < lines.Length; index++)
            {
                var line = lines[index];
                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    meta.Title = line.Substring(2).Trim();
                    break;
                }
            }

            return meta;
        }

        private static void Flush(AxuCliKitSkillMeta meta, string key, StringBuilder value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var text = value.ToString().Trim().Trim('"');
            if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
                meta.Name = text;
            else if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase))
                meta.Description = text;
        }

        /// <summary>Pulls the "## Guardrails" section out of the skill body, or falls back to defaults.</summary>
        public static string HardRules(string skill)
        {
            string raw = null;
            try
            {
                var path = System.IO.Path.Combine(AxuCliKitPaths.SkillsRoot, skill, AxuCliKitPaths.SkillFileName);
                if (System.IO.File.Exists(path))
                    raw = System.IO.File.ReadAllText(path);
            }
            catch (Exception)
            {
                // fall through to defaults
            }

            var section = ExtractSection(raw, "Guardrails");
            return string.IsNullOrEmpty(section) ? FallbackRules : section;
        }

        private static string ExtractSection(string raw, string heading)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            var lines = raw.Replace("\r\n", "\n").Split('\n');
            var start = -1;
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("## " + heading, StringComparison.OrdinalIgnoreCase))
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
                return null;

            var sb = new StringBuilder();
            sb.Append("## ").Append(heading).Append("\n\n");
            var wroteContent = false;
            for (var i = start + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal))
                    break;

                if (!wroteContent && lines[i].Trim().Length == 0)
                    continue;

                wroteContent = true;
                sb.Append(lines[i]).Append('\n');
            }

            return sb.ToString().TrimEnd() + "\n";
        }
    }
}
