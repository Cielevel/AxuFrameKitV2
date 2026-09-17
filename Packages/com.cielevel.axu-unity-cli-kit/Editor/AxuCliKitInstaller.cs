using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxuCliKit.Editor
{
    internal enum AxuCliKitFileAction
    {
        Created,
        Updated,
        Unchanged,
        SkippedModified,
        Deleted,
        Missing
    }

    internal sealed class AxuCliKitFileResult
    {
        public string TargetId;
        public string Skill;
        public string DisplayPath;
        public AxuCliKitFileAction Action;
        public string Message;

        public AxuCliKitFileResult(string targetId, string skill, string displayPath, AxuCliKitFileAction action, string message = null)
        {
            TargetId = targetId;
            Skill = skill;
            DisplayPath = displayPath;
            Action = action;
            Message = message;
        }
    }

    internal sealed class AxuCliKitReport
    {
        public readonly List<AxuCliKitFileResult> Results = new List<AxuCliKitFileResult>();
        public bool DryRun;

        public int CountOf(AxuCliKitFileAction action)
        {
            var count = 0;
            foreach (var r in Results)
            {
                if (r.Action == action)
                    count++;
            }

            return count;
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            if (DryRun)
                sb.AppendLine("(dry run — nothing was written)");

            if (Results.Count == 0)
            {
                sb.AppendLine("Nothing to do.");
                return sb.ToString();
            }

            foreach (var r in Results)
                sb.AppendLine(string.Format("{0,-16} {1,-10} {2}{3}", r.Action, r.TargetId, r.DisplayPath,
                    string.IsNullOrEmpty(r.Message) ? string.Empty : "  — " + r.Message));

            sb.AppendLine();
            sb.AppendLine(string.Format("created {0}, updated {1}, unchanged {2}, skipped (hand-edited) {3}, deleted {4}, missing {5}",
                CountOf(AxuCliKitFileAction.Created), CountOf(AxuCliKitFileAction.Updated),
                CountOf(AxuCliKitFileAction.Unchanged), CountOf(AxuCliKitFileAction.SkippedModified),
                CountOf(AxuCliKitFileAction.Deleted), CountOf(AxuCliKitFileAction.Missing)));
            return sb.ToString();
        }
    }

    [Serializable]
    internal sealed class AxuCliKitStateEntry
    {
        public string target;
        public string skill;
        public string path;
        public string hash;
    }

    [Serializable]
    internal sealed class AxuCliKitState
    {
        public string packageVersion;
        public string updatedUtc;
        public List<AxuCliKitStateEntry> entries = new List<AxuCliKitStateEntry>();

        public AxuCliKitStateEntry Find(string target, string path)
        {
            foreach (var entry in entries)
            {
                if (entry.target == target && entry.path == path)
                    return entry;
            }

            return null;
        }
    }

    /// <summary>
    /// Writes the package's skills into the project's AI-client folders, idempotently.
    ///
    /// Rules that matter:
    ///  - identical content is never rewritten (safe to run on every import);
    ///  - a file whose bytes match neither the shipped content nor the hash recorded last time
    ///    is treated as hand-edited: reported as skipped, not overwritten, unless forced;
    ///  - <c>.axu-cli-kit/state.json</c> records exactly what was written, so uninstall removes
    ///    only our own output.
    /// </summary>
    internal static class AxuCliKitInstaller
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        public static AxuCliKitReport Install(IEnumerable<string> targetIds, bool dryRun, bool force)
        {
            var report = new AxuCliKitReport { DryRun = dryRun };
            var state = LoadState();
            var skills = AxuCliKitPaths.DiscoverSkills();

            if (skills.Count == 0)
            {
                Debug.LogWarning("[AxuCliKit] No skills found under " + AxuCliKitPaths.SkillsRoot +
                                 ". Is the package folder intact?");
                return report;
            }

            foreach (var targetId in targetIds)
            {
                var target = AxuCliKitPaths.FindTarget(targetId);
                if (target == null)
                    continue;

                foreach (var skill in skills)
                    InstallSkill(target, skill, state, report, dryRun, force);
            }

            if (!dryRun)
            {
                state.packageVersion = AxuCliKitPackageVersion();
                state.updatedUtc = DateTime.UtcNow.ToString("o");
                SaveState(state);
                AssetDatabase.Refresh();
            }

            return report;
        }

        public static AxuCliKitReport Uninstall(IEnumerable<string> targetIds, bool dryRun, bool force)
        {
            var report = new AxuCliKitReport { DryRun = dryRun };
            var state = LoadState();
            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in targetIds)
                wanted.Add(id);

            for (var i = state.entries.Count - 1; i >= 0; i--)
            {
                var entry = state.entries[i];
                if (!wanted.Contains(entry.target))
                    continue;

                // A managed target owns only a block inside a shared file, never the file itself.
                var owner = AxuCliKitPaths.FindTarget(entry.target);
                if (owner != null && owner.Kind == AxuCliKitTargetKind.ManagedBlock)
                {
                    state.entries.RemoveAt(i);
                    continue;
                }

                var absolute = AbsolutePath(entry.path);
                state.entries.RemoveAt(i);

                if (!File.Exists(absolute))
                {
                    report.Results.Add(new AxuCliKitFileResult(entry.target, entry.skill, entry.path, AxuCliKitFileAction.Missing));
                    continue;
                }

                if (!force && !string.Equals(Hash(File.ReadAllText(absolute)), entry.hash, StringComparison.Ordinal))
                {
                    report.Results.Add(new AxuCliKitFileResult(entry.target, entry.skill, entry.path,
                        AxuCliKitFileAction.SkippedModified, "hand-edited since install — use Force to remove"));
                    continue;
                }

                if (!dryRun)
                {
                    try
                    {
                        File.Delete(absolute);
                        DeleteEmptyParents(Path.GetDirectoryName(absolute));
                    }
                    catch (Exception e)
                    {
                        report.Results.Add(new AxuCliKitFileResult(entry.target, entry.skill, entry.path,
                            AxuCliKitFileAction.SkippedModified, e.Message));
                        continue;
                    }
                }

                report.Results.Add(new AxuCliKitFileResult(entry.target, entry.skill, entry.path, AxuCliKitFileAction.Deleted));
            }

            // A managed block lives inside a shared file: strip it when the owning target is uninstalled.
            foreach (var targetId in wanted)
            {
                var target = AxuCliKitPaths.FindTarget(targetId);
                if (target == null || target.Kind != AxuCliKitTargetKind.ManagedBlock)
                    continue;

                foreach (var skill in AxuCliKitPaths.DiscoverSkills())
                    RemoveBlock(target, skill, report, dryRun);
            }

            if (!dryRun)
            {
                if (state.entries.Count == 0)
                    DeleteStateFile();
                else
                    SaveState(state);

                AssetDatabase.Refresh();
            }

            return report;
        }

        private static void DeleteStateFile()
        {
            try
            {
                if (File.Exists(AxuCliKitPaths.StateFilePath))
                    File.Delete(AxuCliKitPaths.StateFilePath);

                if (Directory.Exists(AxuCliKitPaths.StateDirectory) &&
                    Directory.GetFileSystemEntries(AxuCliKitPaths.StateDirectory).Length == 0)
                    Directory.Delete(AxuCliKitPaths.StateDirectory, false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AxuCliKit] Could not remove the state file: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- install

        private static void InstallSkill(AxuCliKitTarget target, string skill, AxuCliKitState state,
            AxuCliKitReport report, bool dryRun, bool force)
        {
            switch (target.Kind)
            {
                case AxuCliKitTargetKind.SkillDirectory:
                    InstallSkillDirectory(target, skill, state, report, dryRun, force);
                    break;

                case AxuCliKitTargetKind.PointerFile:
                    InstallPointerFile(target, skill, state, report, dryRun, force);
                    break;

                case AxuCliKitTargetKind.ManagedBlock:
                    InstallManagedBlock(target, skill, state, report, dryRun, force);
                    break;
            }
        }

        private static void InstallSkillDirectory(AxuCliKitTarget target, string skill, AxuCliKitState state,
            AxuCliKitReport report, bool dryRun, bool force)
        {
            var sourceRoot = Path.Combine(AxuCliKitPaths.SkillsRoot, skill);
            if (!Directory.Exists(sourceRoot))
                return;

            var targetRelative = target.ResolvePath(skill).Replace('\\', '/');
            var files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // .DS_Store and friends are not part of the skill.
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(".", StringComparison.Ordinal) || fileName.EndsWith("~", StringComparison.Ordinal))
                    continue;

                var relativeInsideSkill = file.Substring(sourceRoot.Length).TrimStart('/', '\\').Replace('\\', '/');
                WriteFile(target, skill, targetRelative + "/" + relativeInsideSkill,
                    File.ReadAllText(file), state, report, dryRun, force);
            }
        }

        private static void InstallPointerFile(AxuCliKitTarget target, string skill, AxuCliKitState state,
            AxuCliKitReport report, bool dryRun, bool force)
        {
            var content = BuildPointerContent(target, skill);
            WriteFile(target, skill, target.ResolvePath(skill).Replace('\\', '/'), content, state, report, dryRun, force);
        }

        private static void InstallManagedBlock(AxuCliKitTarget target, string skill, AxuCliKitState state,
            AxuCliKitReport report, bool dryRun, bool force)
        {
            var relative = target.ResolvePath(skill).Replace('\\', '/');
            var absolute = AbsolutePath(relative);
            var existing = File.Exists(absolute) ? File.ReadAllText(absolute) : string.Empty;
            var block = BuildManagedBlock(skill);
            var updated = UpsertBlock(existing, block);

            if (existing == updated)
            {
                report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relative, AxuCliKitFileAction.Unchanged));
                return;
            }

            var mode = existing.Contains(AxuCliKitPaths.BlockBegin) ? AxuCliKitFileAction.Updated : AxuCliKitFileAction.Created;
            if (!dryRun)
            {
                // Deliberately NOT recorded in the state file: the state's file list is what uninstall
                // deletes, and this file is not ours — only the delimited block inside it is. Uninstall
                // strips the block instead.
                WriteAllText(absolute, updated);
            }

            report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relative, mode,
                existing.Length == 0 ? "created AGENTS.md" : "managed block rewritten, rest of the file untouched"));
        }

        private static void WriteFile(AxuCliKitTarget target, string skill, string relativePath, string content,
            AxuCliKitState state, AxuCliKitReport report, bool dryRun, bool force)
        {
            var absolute = AbsolutePath(relativePath);
            var desiredHash = Hash(content);
            var existing = File.Exists(absolute) ? File.ReadAllText(absolute) : null;

            if (existing != null && string.Equals(Hash(existing), desiredHash, StringComparison.Ordinal))
            {
                if (!dryRun)
                    UpsertEntry(state, target.Id, skill, relativePath, desiredHash);
                report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relativePath, AxuCliKitFileAction.Unchanged));
                return;
            }

            if (existing != null && !force)
            {
                var recorded = state.Find(target.Id, relativePath);
                var onDiskHash = Hash(existing);
                if (recorded == null || !string.Equals(recorded.hash, onDiskHash, StringComparison.Ordinal))
                {
                    report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relativePath,
                        AxuCliKitFileAction.SkippedModified, "not written by this kit, or edited by hand — use Force to overwrite"));
                    return;
                }
            }

            if (!dryRun)
            {
                WriteAllText(absolute, content);
                UpsertEntry(state, target.Id, skill, relativePath, desiredHash);
            }

            report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relativePath,
                existing == null ? AxuCliKitFileAction.Created : AxuCliKitFileAction.Updated));
        }

        private static void UpsertEntry(AxuCliKitState state, string target, string skill, string path, string hash)
        {
            var entry = state.Find(target, path);
            if (entry == null)
            {
                entry = new AxuCliKitStateEntry { target = target, path = path };
                state.entries.Add(entry);
            }

            entry.skill = skill;
            entry.hash = hash;
        }

        // ---------------------------------------------------------------- content

        private static void RemoveBlock(AxuCliKitTarget target, string skill, AxuCliKitReport report, bool dryRun)
        {
            var relative = target.ResolvePath(skill).Replace('\\', '/');
            var absolute = AbsolutePath(relative);
            if (!File.Exists(absolute))
                return;

            var existing = File.ReadAllText(absolute);
            if (!existing.Contains(AxuCliKitPaths.BlockBegin))
                return;

            var stripped = StripBlock(existing);
            if (!dryRun)
                WriteAllText(absolute, stripped);

            report.Results.Add(new AxuCliKitFileResult(target.Id, skill, relative, AxuCliKitFileAction.Deleted,
                "managed block removed"));
        }

        private static string UpsertBlock(string existing, string block)
        {
            var stripped = StripBlock(existing).TrimEnd();
            var sb = new StringBuilder();
            if (stripped.Length > 0)
            {
                sb.Append(stripped);
                sb.Append("\n\n");
            }

            sb.Append(block);
            sb.Append('\n');
            return sb.ToString();
        }

        private static string StripBlock(string existing)
        {
            var begin = existing.IndexOf(AxuCliKitPaths.BlockBegin, StringComparison.Ordinal);
            if (begin < 0)
                return existing;

            var endMarker = existing.IndexOf(AxuCliKitPaths.BlockEnd, begin, StringComparison.Ordinal);
            if (endMarker < 0)
                return existing.Substring(0, begin);

            var end = endMarker + AxuCliKitPaths.BlockEnd.Length;
            while (end < existing.Length && (existing[end] == '\n' || existing[end] == '\r'))
                end++;

            var head = existing.Substring(0, begin).TrimEnd();
            var tail = existing.Substring(end).TrimStart('\n', '\r');
            if (head.Length == 0)
                return tail;
            if (tail.Length == 0)
                return head + "\n";

            return head + "\n\n" + tail;
        }

        private static string BuildPointerContent(AxuCliKitTarget target, string skill)
        {
            var skillFile = Path.Combine(AxuCliKitPaths.SkillsRoot, skill, AxuCliKitPaths.SkillFileName);
            var raw = File.ReadAllText(skillFile);
            var front = AxuCliKitSkillMeta.Parse(raw);
            var title = string.IsNullOrEmpty(front.Title) ? skill : front.Title;
            var reference = AxuCliKitPaths.TryGetStableSkillReference(skill);

            var sb = new StringBuilder();
            sb.Append("---\n");
            sb.Append("description: \"").Append(EscapeYaml(front.Description)).Append("\"\n");
            switch (target.Id)
            {
                case "cursor":
                    sb.Append("alwaysApply: false\n");
                    break;
                case "windsurf":
                    sb.Append("trigger: model_decision\n");
                    break;
                case "copilot":
                    sb.Append("applyTo: \"**\"\n");
                    break;
            }

            sb.Append("---\n\n");
            sb.Append("# ").Append(title).Append("\n\n");
            sb.Append("<!-- Installed by ").Append(AxuCliKitPaths.PackageName).Append(" ").Append(AxuCliKitPackageVersion())
                .Append(". Edit the package, then re-run AxuFrameKit > CLI Agent Kit > Refresh Installed Skills. -->\n\n");
            sb.Append("> Installed project skill (source of truth: `").Append(reference ?? "the Axu CLI Agent Kit package, Skills~/")
                .Append(reference == null ? skill + "/SKILL.md`" : "`").Append(").\n")
                .Append("> ").Append(reference == null
                    ? "Resolve it from AxuFrameKit > CLI Agent Kit > Open Kit Window, or from any installed client copy."
                    : "Read that file and its `references/` before acting.")
                .Append("\n\n");
            sb.Append(AxuCliKitSkillMeta.HardRules(skill));
            return sb.ToString();
        }

        private static string BuildManagedBlock(string skill)
        {
            var skillFile = Path.Combine(AxuCliKitPaths.SkillsRoot, skill, AxuCliKitPaths.SkillFileName);
            var front = AxuCliKitSkillMeta.Parse(File.ReadAllText(skillFile));
            var reference = AxuCliKitPaths.TryGetStableSkillReference(skill);

            var sb = new StringBuilder();
            sb.Append(AxuCliKitPaths.BlockBegin).Append('\n');
            sb.Append("## Unity command-line work (installed by ").Append(AxuCliKitPaths.PackageName).Append(")\n\n");
            sb.Append(front.Description).Append("\n\n");
            sb.Append("Full skill: ");
            sb.Append(reference != null
                ? "`" + reference + "`"
                : "run `AxuFrameKit > CLI Agent Kit > Install Skills into This Project`, or `unity skill install <client> --local` for Unity's own skills");
            sb.Append("\n\n");
            sb.Append(AxuCliKitSkillMeta.HardRules(skill));
            sb.Append(AxuCliKitPaths.BlockEnd).Append('\n');
            return sb.ToString();
        }

        // ---------------------------------------------------------------- io

        public static AxuCliKitState LoadState()
        {
            var path = AxuCliKitPaths.StateFilePath;
            if (!File.Exists(path))
                return new AxuCliKitState();

            try
            {
                var state = JsonUtility.FromJson<AxuCliKitState>(File.ReadAllText(path));
                return state ?? new AxuCliKitState();
            }
            catch (Exception)
            {
                return new AxuCliKitState();
            }
        }

        private static void SaveState(AxuCliKitState state)
        {
            try
            {
                Directory.CreateDirectory(AxuCliKitPaths.StateDirectory);
                WriteAllText(AxuCliKitPaths.StateFilePath, JsonUtility.ToJson(state, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AxuCliKit] Could not write state file: " + e.Message);
            }
        }

        private static void WriteAllText(string absolutePath, string content)
        {
            var dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(absolutePath, NormalizeLineEndings(content), Utf8NoBom);
        }

        private static string NormalizeLineEndings(string content)
        {
            return content.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string AbsolutePath(string projectRelative)
        {
            return Path.Combine(AxuCliKitPaths.ProjectRoot, projectRelative.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void DeleteEmptyParents(string directory)
        {
            var root = AxuCliKitPaths.ProjectRoot;
            while (!string.IsNullOrEmpty(directory) && directory.Length > root.Length)
            {
                try
                {
                    if (!Directory.Exists(directory) || Directory.GetFileSystemEntries(directory).Length > 0)
                        return;

                    Directory.Delete(directory, false);
                }
                catch (Exception)
                {
                    return;
                }

                directory = Path.GetDirectoryName(directory);
            }
        }

        private static string Hash(string content)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Utf8NoBom.GetBytes(NormalizeLineEndings(content)));
                var sb = new StringBuilder(16);
                for (var i = 0; i < 8; i++)
                    sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static string EscapeYaml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        /// <summary>
        /// The package version, resolved without depending on the Package Manager being ready —
        /// the value is embedded in generated files, so an unstable answer would make every run
        /// look like a change.
        /// </summary>
        private static string AxuCliKitPackageVersion()
        {
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AxuCliKitInstaller).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.version))
                    return info.version;
            }
            catch (Exception)
            {
                // fall through to package.json
            }

            try
            {
                var manifest = Path.Combine(AxuCliKitPaths.PackageRoot, "package.json");
                if (File.Exists(manifest))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        File.ReadAllText(manifest), "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }
            catch (Exception)
            {
                // fall through
            }

            return "unknown";
        }
    }
}
