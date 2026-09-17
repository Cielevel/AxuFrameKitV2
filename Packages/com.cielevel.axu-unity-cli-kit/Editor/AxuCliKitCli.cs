using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Debug = UnityEngine.Debug;

namespace AxuCliKit.Editor
{
    internal sealed class AxuCliKitCliResult
    {
        public bool Found;
        public int ExitCode;
        public string StdOut = string.Empty;
        public string StdErr = string.Empty;
        public string Error;

        public bool Success
        {
            get { return Found && ExitCode == 0; }
        }
    }

    /// <summary>
    /// Thin wrapper around the external `unity` executable.
    ///
    /// A GUI-launched Unity Editor does not inherit a login shell's PATH, so the CLI is looked up
    /// in PATH, then in the install locations the CLI's own installer uses, then via the shell.
    /// </summary>
    internal static class AxuCliKitCli
    {
        private static string s_executable;
        private static bool s_searched;

        /// <summary>AI clients `unity skill install` understands.</summary>
        public static readonly string[] SkillClients =
        {
            "claude-code", "cursor", "windsurf", "vscode", "cline", "codex", "grok", "claude-desktop"
        };

        public static string ExecutablePath
        {
            get
            {
                if (!s_searched)
                {
                    s_executable = Locate();
                    s_searched = true;
                }

                return s_executable;
            }
        }

        public static bool IsAvailable
        {
            get { return !string.IsNullOrEmpty(ExecutablePath); }
        }

        public static void ResetCache()
        {
            s_searched = false;
            s_executable = null;
        }

        private static string Locate()
        {
            var exeName = IsWindows ? "unity.exe" : "unity";

            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var dir in path.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrEmpty(dir))
                        continue;

                    try
                    {
                        var candidate = Path.Combine(dir.Trim(), exeName);
                        if (File.Exists(candidate))
                            return candidate;
                    }
                    catch (Exception)
                    {
                        // ignore malformed PATH entries
                    }
                }
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var candidates = new List<string>
            {
                Path.Combine(home, ".unity", "bin", exeName),
                Path.Combine(home, ".local", "bin", exeName),
                Path.Combine("/usr/local/bin", exeName),
                Path.Combine("/opt/homebrew/bin", exeName)
            };

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "Programs", "unity", exeName));
                candidates.Add(Path.Combine(localAppData, "Unity", "bin", exeName));
            }

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            var shell = LookupViaShell(exeName);
            return string.IsNullOrEmpty(shell) ? null : shell;
        }

        private static string LookupViaShell(string exeName)
        {
            if (IsWindows)
                return null;

            try
            {
                var shell = File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash";
                var result = Run(shell, "-lc \"command -v " + exeName + "\"", 15000);
                if (result.Success)
                {
                    var line = result.StdOut.Trim();
                    if (line.Length > 0 && File.Exists(line))
                        return line;
                }
            }
            catch (Exception)
            {
                // not fatal: the kit simply reports "CLI not found"
            }

            return null;
        }

        private static bool IsWindows
        {
            get { return Path.DirectorySeparatorChar == '\\'; }
        }

        public static AxuCliKitCliResult RunUnity(string arguments, int timeoutMs = 60000)
        {
            var exe = ExecutablePath;
            if (string.IsNullOrEmpty(exe))
            {
                return new AxuCliKitCliResult
                {
                    Found = false,
                    ExitCode = -1,
                    Error = "The `unity` CLI was not found on PATH or in the usual install locations " +
                            "(~/.unity/bin, /usr/local/bin, /opt/homebrew/bin). Install it, then re-check."
                };
            }

            return Run(exe, arguments, timeoutMs);
        }

        public static AxuCliKitCliResult Run(string fileName, string arguments, int timeoutMs)
        {
            var result = new AxuCliKitCliResult { Found = true };

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AxuCliKitPaths.ProjectRoot
                };

                info.EnvironmentVariables["UNITY_NO_BANNER"] = "1";
                info.EnvironmentVariables["UNITY_NON_INTERACTIVE"] = "1";
                info.EnvironmentVariables["NO_COLOR"] = "1";

                var stdout = new StringBuilder();
                var stderr = new StringBuilder();

                using (var process = new Process { StartInfo = info })
                {
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data != null)
                            stdout.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data != null)
                            stderr.AppendLine(e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception)
                        {
                            // the process may have exited between the check and the kill
                        }

                        result.ExitCode = -1;
                        result.StdOut = stdout.ToString();
                        result.StdErr = stderr.ToString();
                        result.Error = "Timed out after " + (timeoutMs / 1000) + "s.";
                        return result;
                    }

                    process.WaitForExit();
                    result.ExitCode = process.ExitCode;
                }

                result.StdOut = stdout.ToString();
                result.StdErr = stderr.ToString();
                return result;
            }
            catch (Exception e)
            {
                result.Found = false;
                result.ExitCode = -1;
                result.Error = e.Message;
                Debug.LogWarning("[AxuCliKit] Failed to run '" + fileName + " " + arguments + "': " + e.Message);
                return result;
            }
        }

        public static AxuCliKitCliResult GetVersion()
        {
            return RunUnity("--version --no-banner", 20000);
        }

        /// <summary>`unity status` — the human table, shown verbatim so nothing is lost in parsing.</summary>
        public static AxuCliKitCliResult GetStatus()
        {
            return RunUnity("status --no-banner --no-pager", 30000);
        }

        public static AxuCliKitCliResult InstallOfficialSkills(string client)
        {
            return RunUnity("skill install " + client + " --local --yes --no-banner", 120000);
        }

        public static AxuCliKitCliResult SkillInstallList()
        {
            return RunUnity("skill install --list --no-banner --no-pager", 30000);
        }
    }
}
