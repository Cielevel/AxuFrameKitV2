using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace AxuCliKit.Editor
{
    internal enum AxuCliKitTargetKind
    {
        /// <summary>Copy of the whole skill folder (SKILL.md + references/ + CHANGELOG.md).</summary>
        SkillDirectory,

        /// <summary>Single rule/instruction file that summarizes the skill and points at the source.</summary>
        PointerFile,

        /// <summary>A delimited block inside a shared file (AGENTS.md), rewritten in place.</summary>
        ManagedBlock
    }

    internal sealed class AxuCliKitTarget
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly AxuCliKitTargetKind Kind;
        public readonly string PathTemplate;
        public readonly string Note;

        /// <summary>
        /// Project-relative path whose existence means "this client is used here". Null for
        /// targets that cannot be inferred safely (they stay opt-in).
        /// </summary>
        public readonly string MarkerPath;

        public AxuCliKitTarget(string id, string displayName, AxuCliKitTargetKind kind, string pathTemplate,
            string note, string markerPath = null)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            PathTemplate = pathTemplate;
            Note = note;
            MarkerPath = markerPath;
        }

        /// <summary>Project-relative path for a given skill (no placeholder left).</summary>
        public string ResolvePath(string skillName)
        {
            return PathTemplate.Replace("{skill}", skillName).Replace('/', Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// Everything that knows where things live: the package itself, this project, the skill
    /// source folder, and the per-client install targets.
    /// </summary>
    internal static class AxuCliKitPaths
    {
        public const string PackageName = "com.cielevel.axu-unity-cli-kit";
        public const string SkillsFolderName = "Skills~";
        public const string StateFolderName = ".axu-cli-kit";
        public const string StateFileName = "state.json";
        public const string BlockBegin = "<!-- axu-cli-kit:begin -->";
        public const string BlockEnd = "<!-- axu-cli-kit:end -->";
        public const string SkillFileName = "SKILL.md";

        private static string s_packageRoot;

        /// <summary>Absolute path of this package, resolved from the assembly when possible.</summary>
        public static string PackageRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(s_packageRoot))
                    return s_packageRoot;

                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AxuCliKitPaths).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
                {
                    s_packageRoot = info.resolvedPath.Replace('\\', '/');
                    return s_packageRoot;
                }

                // PackageInfo is not always available during a domain reload: this code can run from
                // an [InitializeOnLoadMethod] before the Package Manager has re-resolved.
                var embedded = Path.Combine(ProjectRoot, "Packages", PackageName).Replace('\\', '/');
                if (Directory.Exists(embedded))
                {
                    s_packageRoot = embedded;
                    return s_packageRoot;
                }

                // Installed as a git/path dependency: Library/PackageCache/<name>@<version>.
                var cache = Path.Combine(ProjectRoot, "Library", "PackageCache");
                if (Directory.Exists(cache))
                {
                    foreach (var dir in Directory.GetDirectories(cache, PackageName + "@*"))
                    {
                        s_packageRoot = dir.Replace('\\', '/');
                        return s_packageRoot;
                    }
                }

                s_packageRoot = embedded;
                return s_packageRoot;
            }
        }

        public static string SkillsRoot
        {
            get { return Path.Combine(PackageRoot, SkillsFolderName).Replace('\\', '/'); }
        }

        /// <summary>Unity project root (parent of Assets).</summary>
        public static string ProjectRoot
        {
            get { return Path.GetDirectoryName(Application.dataPath).Replace('\\', '/'); }
        }

        public static string StateDirectory
        {
            get { return Path.Combine(ProjectRoot, StateFolderName); }
        }

        public static string StateFilePath
        {
            get { return Path.Combine(StateDirectory, StateFileName); }
        }

        /// <summary>
        /// A project-relative reference to a skill that stays valid when committed and shared,
        /// or null when the package lives somewhere machine-specific (PackageCache).
        /// </summary>
        public static string TryGetStableSkillReference(string skillName)
        {
            // Prefer a full copy that already lives in the project (any installed skill directory):
            // it is project-relative, committed with the repo, and unaffected by how the package
            // itself got resolved.
            foreach (var target in Targets)
            {
                if (target.Kind != AxuCliKitTargetKind.SkillDirectory)
                    continue;

                var relative = target.ResolvePath(skillName).Replace('\\', '/');
                if (File.Exists(Path.Combine(ProjectRoot, relative, SkillFileName)))
                    return relative + "/" + SkillFileName;
            }

            var skillsRoot = SkillsRoot;
            var projectRoot = ProjectRoot;
            if (skillsRoot.StartsWith(projectRoot + "/", StringComparison.Ordinal))
                return skillsRoot.Substring(projectRoot.Length + 1) + "/" + skillName + "/" + SkillFileName;

            return null;
        }

        public static readonly AxuCliKitTarget[] Targets =
        {
            new AxuCliKitTarget(
                "claude-code",
                "Claude Code — .claude/skills/",
                AxuCliKitTargetKind.SkillDirectory,
                ".claude/skills/{skill}",
                "Full skill copy (SKILL.md + references/).",
                ".claude"),

            new AxuCliKitTarget(
                "zcode",
                "ZCode — .zcode/skills/",
                AxuCliKitTargetKind.SkillDirectory,
                ".zcode/skills/{skill}",
                "Full skill copy. ZCode scans .zcode/skills before .agents/skills.",
                ".zcode"),

            new AxuCliKitTarget(
                "opencode",
                "opencode — .opencode/skills/",
                AxuCliKitTargetKind.SkillDirectory,
                ".opencode/skills/{skill}",
                "Full skill copy in opencode's own project config folder (read before the shared .agents/skills).",
                ".opencode"),

            new AxuCliKitTarget(
                "agents-dir",
                "Shared skills dir — .agents/skills/",
                AxuCliKitTargetKind.SkillDirectory,
                ".agents/skills/{skill}",
                "Full skill copy in the cross-tool location: Codex, Cursor, Copilot, Gemini CLI, Zed, OpenCode, Cline, Windsurf, Kilo and more read it.",
                ".agents"),

            new AxuCliKitTarget(
                "cursor",
                "Cursor rules — .cursor/rules/",
                AxuCliKitTargetKind.PointerFile,
                ".cursor/rules/{skill}.mdc",
                "Always-available rule file with frontmatter; use instead of (or beside) the shared skills dir.",
                ".cursor"),

            new AxuCliKitTarget(
                "windsurf",
                "Windsurf rules — .windsurf/rules/",
                AxuCliKitTargetKind.PointerFile,
                ".windsurf/rules/{skill}.md",
                "Rule file (trigger: model_decision).",
                ".windsurf"),

            new AxuCliKitTarget(
                "cline",
                "Cline rules — .clinerules/",
                AxuCliKitTargetKind.PointerFile,
                ".clinerules/{skill}.md",
                "Plain markdown rule file.",
                ".clinerules"),

            new AxuCliKitTarget(
                "copilot",
                "VS Code / Copilot — .github/instructions/",
                AxuCliKitTargetKind.PointerFile,
                ".github/instructions/{skill}.instructions.md",
                "Instruction file with applyTo: \"**\"."),

            new AxuCliKitTarget(
                "agents-md",
                "AGENTS.md — managed block",
                AxuCliKitTargetKind.ManagedBlock,
                "AGENTS.md",
                "Rewrites only the block between the axu-cli-kit markers. Read by ZCode, Codex, Cursor, Copilot and others.")
        };

        public static AxuCliKitTarget FindTarget(string id)
        {
            foreach (var target in Targets)
            {
                if (string.Equals(target.Id, id, StringComparison.OrdinalIgnoreCase))
                    return target;
            }

            return null;
        }

        /// <summary>Shortens a path for display: the home directory collapses to `~`.</summary>
        public static string Shorten(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home) && absolutePath.StartsWith(home, StringComparison.Ordinal))
                return "~" + absolutePath.Substring(home.Length);

            return absolutePath;
        }

        /// <summary>Every skill folder in the package that has a SKILL.md.</summary>
        public static List<string> DiscoverSkills()
        {
            var result = new List<string>();
            var root = SkillsRoot;
            if (!Directory.Exists(root))
                return result;

            foreach (var dir in Directory.GetDirectories(root))
            {
                if (File.Exists(Path.Combine(dir, SkillFileName)))
                    result.Add(Path.GetFileName(dir));
            }

            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }
}
