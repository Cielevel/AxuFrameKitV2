using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AxuCliKit.Editor
{
    internal static class AxuCliKitMenu
    {
        /// <summary>
        /// Sits under the project's AxuFrameKit menu, alongside the other toolkit categories
        /// (Modules, Hierarchy, Utility, …) so there is one entry point for everything.
        /// </summary>
        private const string Root = "AxuFrameKit/CLI Agent Kit/";

        [MenuItem(Root + "Open Kit Window", false, 10)]
        public static void OpenWindow()
        {
            AxuCliKitWindow.Open();
        }

        [MenuItem(Root + "Install Skills into This Project", false, 30)]
        public static void Install()
        {
            var report = AxuCliKitInstaller.Install(AxuCliKitSettings.GetEnabledTargets(), false, false);
            Debug.Log("[AxuCliKit] install\n" + report.ToText());
        }

        [MenuItem(Root + "Refresh Installed Skills", false, 31)]
        public static void Refresh()
        {
            var report = AxuCliKitInstaller.Install(AxuCliKitSettings.GetEnabledTargets(), false, false);
            Debug.Log("[AxuCliKit] refresh\n" + report.ToText());
        }

        [MenuItem(Root + "Uninstall Skills from This Project", false, 32)]
        public static void Uninstall()
        {
            if (!EditorUtility.DisplayDialog("Axu CLI Agent Kit",
                    "Remove the skills this kit installed into the project?\n\nOnly files recorded in " +
                    AxuCliKitPaths.StateFolderName + "/" + AxuCliKitPaths.StateFileName + " are removed.",
                    "Uninstall", "Cancel"))
                return;

            var report = AxuCliKitInstaller.Uninstall(AxuCliKitSettings.GetEnabledTargets(), false, false);
            Debug.Log("[AxuCliKit] uninstall\n" + report.ToText());
        }

        [MenuItem(Root + "Install Official Unity Skills (this project)", false, 50)]
        public static void InstallOfficialSkills()
        {
            var result = AxuCliKitCli.InstallOfficialSkills(AxuCliKitSettings.SkillClient);
            Debug.Log("[AxuCliKit] official skills (" + AxuCliKitSettings.SkillClient + ")\n" +
                      (string.IsNullOrEmpty(result.Error) ? result.StdOut : result.Error));
        }

        [MenuItem(Root + "Check Unity CLI", false, 51)]
        public static void CheckCli()
        {
            AxuCliKitCli.ResetCache();
            var version = AxuCliKitCli.GetVersion();
            if (!version.Found)
            {
                Debug.LogWarning("[AxuCliKit] " + version.Error);
                return;
            }

            Debug.Log("[AxuCliKit] unity " + version.StdOut.Trim() + "  (" + AxuCliKitCli.ExecutablePath + ")");
        }

        [MenuItem(Root + "Open Documentation", false, 70)]
        public static void OpenDocumentation()
        {
            var index = Path.Combine(AxuCliKitPaths.PackageRoot, "Documentation~", "index.md");
            if (File.Exists(index))
                EditorUtility.OpenWithDefaultApp(index);
            else
                Debug.LogWarning("[AxuCliKit] Documentation~/index.md is missing from the package.");
        }
    }

    /// <summary>
    /// Offers the install once per machine/project, so pulling the package is enough to get the
    /// skill into the project — without writing anything into a checkout the user did not ask for.
    /// </summary>
    [InitializeOnLoad]
    internal static class AxuCliKitBootstrap
    {
        static AxuCliKitBootstrap()
        {
            EditorApplication.update += WaitForUser;
        }

        /// <summary>
        /// Waits until the Editor is settled and in front of the user. A dialog raised while the
        /// Editor is unfocused or mid-import would either be missed or block work silently.
        /// </summary>
        private static void WaitForUser()
        {
            if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (!InternalEditorUtility.isApplicationActive)
                return;

            EditorApplication.update -= WaitForUser;
            OfferInstallOnce();
        }

        /// <summary>Session-scoped, per project: survives recompiles, cleared when the Editor restarts.</summary>
        private static string SessionKey()
        {
            var hash = 17;
            foreach (var c in AxuCliKitPaths.ProjectRoot)
                hash = hash * 31 + c;

            return "AxuCliKit.PromptDismissed." + (hash & 0x7fffffff).ToString("x8");
        }

        private static void OfferInstallOnce()
        {
            try
            {
                if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;

                if (AxuCliKitSettings.NeverAsk || AxuCliKitSettings.PromptAnswered)
                    return;

                var state = AxuCliKitInstaller.LoadState();
                if (state.entries.Count > 0)
                {
                    AxuCliKitSettings.PromptAnswered = true;
                    return;
                }

                // "Not now" means this session: a fresh Editor launch may ask once more. Repeated
                // recompiles inside one session must not re-open the dialog.
                if (SessionState.GetBool(SessionKey(), false))
                    return;

                var targets = AxuCliKitSettings.GetEnabledTargets();
                var targetList = string.Join(", ", targets.ToArray());
                var message =
                    "Install the Unity CLI agent skill into this project?\n\n" +
                    "Targets: " + targetList + "\n" +
                    "Location: " + AxuCliKitPaths.ProjectRoot + "\n\n" +
                    "The skill teaches an agent to drive a running Editor, run the " +
                    "edit → recompile → test loop, and run tests/builds from the unity CLI. " +
                    "Nothing is installed until you choose Install; you can change targets and " +
                    "refresh later from AxuFrameKit → CLI Agent Kit.";

                var choice = EditorUtility.DisplayDialogComplex("Axu CLI Agent Kit", message,
                    "Install", "Not now", "Don't ask again");

                if (choice == 1)
                {
                    SessionState.SetBool(SessionKey(), true);
                    return;
                }

                AxuCliKitSettings.PromptAnswered = true;
                if (choice == 2)
                {
                    AxuCliKitSettings.NeverAsk = true;
                    return;
                }

                var report = AxuCliKitInstaller.Install(targets, false, false);
                Debug.Log("[AxuCliKit] Installed agent skills:\n" + report.ToText());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AxuCliKit] Install prompt failed: " + e);
            }
        }
    }
}
