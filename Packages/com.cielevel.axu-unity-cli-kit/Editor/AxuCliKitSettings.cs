using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace AxuCliKit.Editor
{
    /// <summary>Per-machine, per-project preferences (EditorPrefs — nothing is written into the project).</summary>
    internal static class AxuCliKitSettings
    {
        private const string Prefix = "AxuCliKit.";

        /// <summary>Default target set: the one client directory that holds a full skill copy.</summary>
        public const string DefaultTargets = "claude-code";

        private static string Key(string name)
        {
            return Prefix + name + "." + ProjectKey();
        }

        private static string ProjectKey()
        {
            var path = AxuCliKitPaths.ProjectRoot;
            var hash = 17;
            foreach (var c in path)
                hash = hash * 31 + c;

            return (hash & 0x7fffffff).ToString("x8");
        }

        /// <summary>True once the user answered the first-import prompt for this project.</summary>
        public static bool PromptAnswered
        {
            get { return EditorPrefs.GetBool(Key("PromptAnswered"), false); }
            set { EditorPrefs.SetBool(Key("PromptAnswered"), value); }
        }

        /// <summary>Set when the user picked "Don't ask again".</summary>
        public static bool NeverAsk
        {
            get { return EditorPrefs.GetBool(Key("NeverAsk"), false); }
            set { EditorPrefs.SetBool(Key("NeverAsk"), value); }
        }

        public static string EnabledTargetsCsv
        {
            get
            {
                var key = Key("Targets");

                // Once the user has ticked anything, their choice wins — including "nothing".
                if (EditorPrefs.HasKey(key))
                    return EditorPrefs.GetString(key);

                return DetectTargetsCsv();
            }
            set { EditorPrefs.SetString(Key("Targets"), value); }
        }

        /// <summary>
        /// First-run suggestion: a client whose directory already exists in this project is
        /// assumed to be in use. When nothing is detected, fall back to the single most common
        /// client so the package still does something useful out of the box.
        /// </summary>
        public static string DetectTargetsCsv()
        {
            var found = new List<string>();

            foreach (var target in AxuCliKitPaths.Targets)
            {
                if (string.IsNullOrEmpty(target.MarkerPath))
                    continue;

                var marker = System.IO.Path.Combine(AxuCliKitPaths.ProjectRoot,
                    target.MarkerPath.Replace('/', System.IO.Path.DirectorySeparatorChar));

                if (System.IO.Directory.Exists(marker) || System.IO.File.Exists(marker))
                    found.Add(target.Id);
            }

            if (found.Count == 0)
                return DefaultTargets;

            return string.Join(",", found.ToArray());
        }

        public static string SkillClient
        {
            get { return EditorPrefs.GetString(Key("SkillClient"), "claude-code"); }
            set { EditorPrefs.SetString(Key("SkillClient"), value); }
        }

        public static bool DryRun
        {
            get { return EditorPrefs.GetBool(Key("DryRun"), false); }
            set { EditorPrefs.SetBool(Key("DryRun"), value); }
        }

        public static bool Force
        {
            get { return EditorPrefs.GetBool(Key("Force"), false); }
            set { EditorPrefs.SetBool(Key("Force"), value); }
        }

        public static List<string> GetEnabledTargets()
        {
            var result = new List<string>();
            foreach (var id in EnabledTargetsCsv.Split(','))
            {
                var trimmed = id.Trim();
                if (trimmed.Length > 0)
                    result.Add(trimmed);
            }

            return result;
        }

        public static bool IsTargetEnabled(string id)
        {
            foreach (var candidate in GetEnabledTargets())
            {
                if (string.Equals(candidate, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void SetTargetEnabled(string id, bool enabled)
        {
            var ids = GetEnabledTargets();
            var index = ids.FindIndex(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));

            if (enabled && index < 0)
                ids.Add(id);
            else if (!enabled && index >= 0)
                ids.RemoveAt(index);

            var sb = new StringBuilder();
            foreach (var candidate in ids)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(candidate);
            }

            EnabledTargetsCsv = sb.ToString();
        }
    }
}
