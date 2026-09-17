using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxuCliKit.Editor
{
    /// <summary>Status panel and control surface for the kit's skill installation.</summary>
    internal sealed class AxuCliKitWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _cliVersion;
        private string _cliStatus;
        private string _report = "No action yet. Pick the targets you want and press Install / Refresh.";
        private string _officialResult;
        private bool _showCliStatus;

        public static void Open()
        {
            var window = GetWindow<AxuCliKitWindow>(false, "Axu CLI Agent Kit", true);
            window.minSize = new Vector2(460, 420);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshCliInfo();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space();
            DrawEnvironment();
            EditorGUILayout.Space();
            DrawTargets();
            EditorGUILayout.Space();
            DrawOfficialSkills();
            EditorGUILayout.Space();
            DrawReport();
            EditorGUILayout.Space();

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ sections

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Axu CLI Agent Kit", EditorStyles.boldLabel);

            var skills = AxuCliKitPaths.DiscoverSkills();
            EditorGUILayout.LabelField("Package", AxuCliKitPaths.PackageRoot, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Skills", skills.Count == 0 ? "(none found!)" : string.Join(", ", skills), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Installed by", "com.cielevel.axu-unity-cli-kit", EditorStyles.miniLabel);

            var state = AxuCliKitInstaller.LoadState();
            EditorGUILayout.LabelField("State", state.entries.Count == 0
                ? "nothing installed from this kit yet"
                : state.entries.Count + " file(s) recorded, last sync " + state.updatedUtc, EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Documentation", GUILayout.Width(180)))
                    OpenDocumentation();

                if (GUILayout.Button("Reveal package folder", GUILayout.Width(180)))
                    EditorUtility.RevealInFinder(AxuCliKitPaths.PackageRoot);
            }
        }

        private void DrawEnvironment()
        {
            EditorGUILayout.LabelField("Unity CLI", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(AxuCliKitCli.IsAvailable
                    ? AxuCliKitPaths.Shorten(AxuCliKitCli.ExecutablePath)
                    : "not found on PATH or in the usual install locations");

                if (GUILayout.Button("Check", GUILayout.Width(70)))
                    RefreshCliInfo();
            }

            EditorGUILayout.LabelField("Version", string.IsNullOrEmpty(_cliVersion) ? "(not checked)" : _cliVersion, EditorStyles.miniLabel);

            if (!AxuCliKitCli.IsAvailable)
            {
                EditorGUILayout.HelpBox(
                    "Install the unity CLI:\n" +
                    "macOS / Linux:\ncurl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash\n" +
                    "Windows: see Documentation~/01-install-and-update.md",
                    MessageType.Info);
            }

            _showCliStatus = EditorGUILayout.Foldout(_showCliStatus, "Editor instances (unity status)");
            if (_showCliStatus)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run unity status", GUILayout.Width(180)))
                    {
                        var result = AxuCliKitCli.GetStatus();
                        _cliStatus = Describe(result);
                    }

                    if (GUILayout.Button("Install Pipeline package", GUILayout.Width(200)))
                    {
                        var result = AxuCliKitCli.RunUnity("pipeline install", 180000);
                        _cliStatus = Describe(result);
                    }
                }

                if (!string.IsNullOrEmpty(_cliStatus))
                    EditorGUILayout.TextArea(_cliStatus, GUILayout.MinHeight(70));
            }
        }

        private void DrawTargets()
        {
            EditorGUILayout.LabelField("Install targets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Where the skills are written in this project.", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Detect clients used in this project", GUILayout.Width(280)))
                    AxuCliKitSettings.EnabledTargetsCsv = AxuCliKitSettings.DetectTargetsCsv();

                EditorGUILayout.LabelField("A client whose project folder already exists is assumed to be in use.",
                    EditorStyles.miniLabel);
            }

            foreach (var target in AxuCliKitPaths.Targets)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var enabled = AxuCliKitSettings.IsTargetEnabled(target.Id);
                    var updated = EditorGUILayout.ToggleLeft(target.DisplayName, enabled, GUILayout.Width(250));
                    if (updated != enabled)
                        AxuCliKitSettings.SetTargetEnabled(target.Id, updated);

                    EditorGUILayout.LabelField(target.Note, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                var dryRun = AxuCliKitSettings.DryRun;
                AxuCliKitSettings.DryRun = EditorGUILayout.ToggleLeft("Dry run", dryRun, GUILayout.Width(90));

                var force = AxuCliKitSettings.Force;
                AxuCliKitSettings.Force = EditorGUILayout.ToggleLeft("Force (overwrite hand-edited files)", force, GUILayout.Width(260));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Install / Refresh", GUILayout.Height(24)))
                    RunInstall();

                if (GUILayout.Button("Uninstall selected", GUILayout.Height(24), GUILayout.Width(160)))
                    RunUninstall();
            }
        }

        private void DrawOfficialSkills()
        {
            EditorGUILayout.LabelField("Unity's own skills", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Writes the official unity-cli and unity-pipeline skills into this project (unity skill install --local).",
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var client = AxuCliKitSettings.SkillClient;
                var index = Mathf.Max(0, Array.IndexOf(AxuCliKitCli.SkillClients, client));
                index = EditorGUILayout.Popup(index, AxuCliKitCli.SkillClients, GUILayout.Width(160));
                AxuCliKitSettings.SkillClient = AxuCliKitCli.SkillClients[index];

                if (GUILayout.Button("Install official skills", GUILayout.Width(180)))
                {
                    var result = AxuCliKitCli.InstallOfficialSkills(AxuCliKitSettings.SkillClient);
                    _officialResult = Describe(result);
                }

                if (GUILayout.Button("Show install list", GUILayout.Width(140)))
                {
                    var result = AxuCliKitCli.SkillInstallList();
                    _officialResult = Describe(result);
                }
            }

            if (!string.IsNullOrEmpty(_officialResult))
                EditorGUILayout.TextArea(_officialResult, GUILayout.MinHeight(50));
        }

        private void DrawReport()
        {
            EditorGUILayout.LabelField("Last result", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_report, GUILayout.MinHeight(120));
        }

        // ------------------------------------------------------------------ actions

        private void RunInstall()
        {
            var targets = AxuCliKitSettings.GetEnabledTargets();
            if (targets.Count == 0)
            {
                _report = "No targets selected — tick at least one above.";
                return;
            }

            var report = AxuCliKitInstaller.Install(targets, AxuCliKitSettings.DryRun, AxuCliKitSettings.Force);
            _report = report.ToText();

            if (!AxuCliKitSettings.DryRun)
                Debug.Log("[AxuCliKit] " + report.ToText());
        }

        private void RunUninstall()
        {
            var targets = AxuCliKitSettings.GetEnabledTargets();
            if (targets.Count == 0)
            {
                _report = "No targets selected — tick at least one above.";
                return;
            }

            var report = AxuCliKitInstaller.Uninstall(targets, AxuCliKitSettings.DryRun, AxuCliKitSettings.Force);
            _report = report.ToText();

            if (!AxuCliKitSettings.DryRun)
                Debug.Log("[AxuCliKit] " + report.ToText());
        }

        private void RefreshCliInfo()
        {
            if (!AxuCliKitCli.IsAvailable)
            {
                _cliVersion = null;
                return;
            }

            var result = AxuCliKitCli.GetVersion();
            _cliVersion = result.Success
                ? result.StdOut.Trim()
                : "error: " + (string.IsNullOrEmpty(result.Error) ? result.StdErr.Trim() : result.Error);
        }

        private static string Describe(AxuCliKitCliResult result)
        {
            var sb = new StringBuilder();
            if (!result.Found)
            {
                sb.AppendLine(result.Error);
                return sb.ToString();
            }

            sb.AppendLine("exit " + result.ExitCode);
            if (!string.IsNullOrEmpty(result.StdOut))
                sb.AppendLine(result.StdOut.TrimEnd());
            if (!string.IsNullOrEmpty(result.StdErr))
                sb.AppendLine("stderr: " + result.StdErr.TrimEnd());
            if (!string.IsNullOrEmpty(result.Error))
                sb.AppendLine(result.Error);
            return sb.ToString();
        }

        private static void OpenDocumentation()
        {
            var index = Path.Combine(AxuCliKitPaths.PackageRoot, "Documentation~", "index.md");
            if (File.Exists(index))
                EditorUtility.OpenWithDefaultApp(index);
            else
                Debug.LogWarning("[AxuCliKit] Documentation~/index.md is missing from the package.");
        }
    }
}
