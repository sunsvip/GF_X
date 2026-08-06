using System;
using System.Collections.Generic;
using System.IO;

namespace UGF.EditorTools
{
    internal static class AiCliCommandResolver
    {
        internal readonly struct LaunchSpec
        {
            internal readonly string FileName;
            internal readonly string Arguments;
            internal readonly bool UseStandardInput;

            internal LaunchSpec(string fileName, string arguments, bool useStandardInput)
            {
                FileName = fileName;
                Arguments = arguments ?? string.Empty;
                UseStandardInput = useStandardInput;
            }
        }

        internal static bool TryResolve(string commandName, string commandArguments, bool useStandardInput, out LaunchSpec launchSpec, out string error)
        {
            launchSpec = default;
            error = null;

            if (string.IsNullOrWhiteSpace(commandName))
            {
                error = "CLI command name is empty.";
                return false;
            }

            string resolvedPath = ResolveCommandPath(commandName);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                error = $"未找到 CLI 命令: {commandName}";
                return false;
            }

            string extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
            if (IsWindows())
            {
                switch (extension)
                {
                    case ".cmd":
                    case ".bat":
                        launchSpec = new LaunchSpec(
                            Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                            $"/d /s /c \"\"{resolvedPath}\" {commandArguments}\"",
                            useStandardInput);
                        return true;

                    case ".ps1":
                        launchSpec = new LaunchSpec(
                            "powershell.exe",
                            $"-NoProfile -ExecutionPolicy Bypass -File \"{resolvedPath}\" {commandArguments}",
                            useStandardInput);
                        return true;
                }
            }

            launchSpec = new LaunchSpec(resolvedPath, commandArguments, useStandardInput);
            return true;
        }

        private static string ResolveCommandPath(string commandName)
        {
            if (Path.IsPathRooted(commandName) && File.Exists(commandName))
            {
                return commandName;
            }

            string[] candidateFileNames = BuildCandidateFileNames(commandName);
            foreach (string directory in EnumerateSearchDirectories())
            {
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    continue;
                }

                for (int i = 0; i < candidateFileNames.Length; i++)
                {
                    string candidatePath = Path.Combine(directory, candidateFileNames[i]);
                    if (File.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                }
            }

            return null;
        }

        private static string[] BuildCandidateFileNames(string commandName)
        {
            string extension = Path.GetExtension(commandName);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return new[] { commandName };
            }

            if (!IsWindows())
            {
                return new[]
                {
                    commandName,
                    commandName + ".sh"
                };
            }

            return new[]
            {
                commandName + ".cmd",
                commandName + ".exe",
                commandName + ".bat",
                commandName + ".ps1",
                commandName
            };
        }

        private static IEnumerable<string> EnumerateSearchDirectories()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pathDir in SplitPath(Environment.GetEnvironmentVariable("PATH")))
            {
                if (seen.Add(pathDir))
                {
                    yield return pathDir;
                }
            }

            if (IsWindows())
            {
                foreach (string pathDir in SplitPath(TryGetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)))
                {
                    if (seen.Add(pathDir))
                    {
                        yield return pathDir;
                    }
                }

                foreach (string pathDir in SplitPath(TryGetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)))
                {
                    if (seen.Add(pathDir))
                    {
                        yield return pathDir;
                    }
                }
            }
        }

        private static IEnumerable<string> SplitPath(string pathValue)
        {
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                yield break;
            }

            string[] parts = pathValue.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(part))
                {
                    yield return part;
                }
            }
        }
        private static string TryGetEnvironmentVariable(string name, EnvironmentVariableTarget target)
        {
            try
            {
                return Environment.GetEnvironmentVariable(name, target);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
    }
}
